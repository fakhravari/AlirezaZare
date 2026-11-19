SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'128-product-unit-single-base-source')
    BEGIN
        IF OBJECT_ID(N'[Warehouse].[Product]', N'U') IS NULL OR OBJECT_ID(N'[Warehouse].[ProductUnit]', N'U') IS NULL
            THROW 51280, 'Warehouse.Product or Warehouse.ProductUnit is missing.', 1;
        IF COL_LENGTH(N'Warehouse.ProductUnit', N'IdProduct') IS NULL OR COL_LENGTH(N'Warehouse.ProductUnit', N'IdUnit') IS NULL
           OR COL_LENGTH(N'Warehouse.ProductUnit', N'ConversionFactor') IS NULL OR COL_LENGTH(N'Warehouse.ProductUnit', N'IsBaseUnit') IS NULL
           OR COL_LENGTH(N'Warehouse.ProductUnit', N'IsActive') IS NULL
            THROW 51281, 'Warehouse.ProductUnit is missing a required unit-link column.', 1;

        IF COL_LENGTH(N'Warehouse.Product', N'IdBaseUnit') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'Warehouse.Product', N'Id') IS NULL
                THROW 51282, 'Warehouse.Product.Id is missing.', 1;
            IF EXISTS
            (
                SELECT 1 FROM [Warehouse].[Product] product
                LEFT JOIN [Warehouse].[Unit] unit ON unit.[Id] = product.[IdBaseUnit]
                WHERE unit.[Id] IS NULL
            )
                THROW 51283, 'A product references a missing base unit; repair it before migration.', 1;

            -- The existing product pointer is the migration source. Normalize the
            -- unit links to it, adding a base link when one did not exist.
            UPDATE link
            SET [IsBaseUnit] = CASE WHEN link.[IdUnit] = product.[IdBaseUnit] THEN 1 ELSE 0 END,
                [IsActive] = CASE WHEN link.[IdUnit] = product.[IdBaseUnit] THEN 1 ELSE link.[IsActive] END,
                [ConversionFactor] = CASE WHEN link.[IdUnit] = product.[IdBaseUnit] THEN 1 ELSE link.[ConversionFactor] END
            FROM [Warehouse].[ProductUnit] link
            JOIN [Warehouse].[Product] product ON product.[Id] = link.[IdProduct];

            INSERT [Warehouse].[ProductUnit] ([IdProduct], [IdUnit], [ConversionFactor], [IsBaseUnit], [IsActive])
            SELECT product.[Id], product.[IdBaseUnit], 1, 1, 1
            FROM [Warehouse].[Product] product
            WHERE NOT EXISTS
            (
                SELECT 1 FROM [Warehouse].[ProductUnit] link
                WHERE link.[IdProduct] = product.[Id] AND link.[IdUnit] = product.[IdBaseUnit]
            );

            DECLARE @ConstraintName sysname;
            DECLARE @Command nvarchar(max);
            DECLARE ForeignKeys CURSOR LOCAL FAST_FORWARD FOR
                SELECT DISTINCT foreignKey.[name]
                FROM sys.foreign_keys foreignKey
                JOIN sys.foreign_key_columns foreignKeyColumn ON foreignKeyColumn.[constraint_object_id] = foreignKey.[object_id]
                JOIN sys.columns columnInfo ON columnInfo.[object_id] = foreignKeyColumn.[parent_object_id]
                    AND columnInfo.[column_id] = foreignKeyColumn.[parent_column_id]
                WHERE foreignKey.[parent_object_id] = OBJECT_ID(N'[Warehouse].[Product]')
                    AND columnInfo.[name] = N'IdBaseUnit';
            OPEN ForeignKeys;
            FETCH NEXT FROM ForeignKeys INTO @ConstraintName;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @Command = N'ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                EXEC sys.sp_executesql @Command;
                FETCH NEXT FROM ForeignKeys INTO @ConstraintName;
            END;
            CLOSE ForeignKeys;
            DEALLOCATE ForeignKeys;

            DECLARE Defaults CURSOR LOCAL FAST_FORWARD FOR
                SELECT [name] FROM sys.default_constraints
                WHERE [parent_object_id] = OBJECT_ID(N'[Warehouse].[Product]') AND [parent_column_id] = COLUMNPROPERTY(OBJECT_ID(N'[Warehouse].[Product]'), N'IdBaseUnit', 'ColumnId');
            OPEN Defaults;
            FETCH NEXT FROM Defaults INTO @ConstraintName;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @Command = N'ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                EXEC sys.sp_executesql @Command;
                FETCH NEXT FROM Defaults INTO @ConstraintName;
            END;
            CLOSE Defaults;
            DEALLOCATE Defaults;

            DECLARE Checks CURSOR LOCAL FAST_FORWARD FOR
                SELECT DISTINCT checkConstraint.[name]
                FROM sys.check_constraints checkConstraint
                WHERE checkConstraint.[parent_object_id] = OBJECT_ID(N'[Warehouse].[Product]')
                    AND checkConstraint.[definition] LIKE N'%IdBaseUnit%';
            OPEN Checks;
            FETCH NEXT FROM Checks INTO @ConstraintName;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @Command = N'ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                EXEC sys.sp_executesql @Command;
                FETCH NEXT FROM Checks INTO @ConstraintName;
            END;
            CLOSE Checks;
            DEALLOCATE Checks;

            DECLARE @IndexName sysname, @IsUniqueConstraint bit, @IsPrimaryKey bit;
            DECLARE ProductIndexes CURSOR LOCAL FAST_FORWARD FOR
                SELECT DISTINCT indexInfo.[name], indexInfo.[is_unique_constraint], indexInfo.[is_primary_key]
                FROM sys.indexes indexInfo
                JOIN sys.index_columns indexColumn ON indexColumn.[object_id] = indexInfo.[object_id]
                    AND indexColumn.[index_id] = indexInfo.[index_id]
                JOIN sys.columns columnInfo ON columnInfo.[object_id] = indexColumn.[object_id]
                    AND columnInfo.[column_id] = indexColumn.[column_id]
                WHERE indexInfo.[object_id] = OBJECT_ID(N'[Warehouse].[Product]')
                    AND indexInfo.[name] IS NOT NULL AND columnInfo.[name] = N'IdBaseUnit';
            OPEN ProductIndexes;
            FETCH NEXT FROM ProductIndexes INTO @IndexName, @IsUniqueConstraint, @IsPrimaryKey;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                IF @IsPrimaryKey = 1 THROW 51284, 'Product.IdBaseUnit unexpectedly participates in the primary key.', 1;
                IF @IsUniqueConstraint = 1
                BEGIN
                    SET @Command = N'ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT ' + QUOTENAME(@IndexName);
                    EXEC sys.sp_executesql @Command;
                END
                ELSE
                BEGIN
                    SET @Command = N'DROP INDEX ' + QUOTENAME(@IndexName) + N' ON [Warehouse].[Product]';
                    EXEC sys.sp_executesql @Command;
                END
                FETCH NEXT FROM ProductIndexes INTO @IndexName, @IsUniqueConstraint, @IsPrimaryKey;
            END;
            CLOSE ProductIndexes;
            DEALLOCATE ProductIndexes;

            ALTER TABLE [Warehouse].[Product] DROP COLUMN [IdBaseUnit];
        END;

        IF EXISTS
        (
            SELECT [IdProduct] FROM [Warehouse].[ProductUnit]
            WHERE [IsBaseUnit] = 1 AND [IsActive] = 1
            GROUP BY [IdProduct] HAVING COUNT_BIG(*) > 1
        ) OR EXISTS
        (
            SELECT 1 FROM [Warehouse].[Product] product
            WHERE NOT EXISTS
            (
                SELECT 1 FROM [Warehouse].[ProductUnit] link
                WHERE link.[IdProduct] = product.[Id] AND link.[IsBaseUnit] = 1 AND link.[IsActive] = 1
            )
        )
            THROW 51285, 'Every product must have exactly one active base-unit link.', 1;

        IF NOT EXISTS
        (
            SELECT 1 FROM sys.indexes
            WHERE [object_id] = OBJECT_ID(N'[Warehouse].[ProductUnit]') AND [name] = N'UX_ProductUnit_OneActiveBasePerProduct'
        )
            CREATE UNIQUE INDEX [UX_ProductUnit_OneActiveBasePerProduct]
                ON [Warehouse].[ProductUnit] ([IdProduct]) WHERE [IsBaseUnit] = 1 AND [IsActive] = 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'128-product-unit-single-base-source');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
