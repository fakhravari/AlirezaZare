SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'069-drop-cart-options-json')
    BEGIN
        IF COL_LENGTH(N'Account.CartItem', N'OptionsJson') IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM [Account].[CartItem] WHERE [OptionsJson] IS NOT NULL AND ISJSON([OptionsJson]) <> 1)
                THROW 51069, 'CartItem.OptionsJson contains invalid JSON; column was preserved.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM [Account].[CartItem] AS item
                CROSS APPLY OPENJSON(item.[OptionsJson], '$.priceOptions')
                    WITH ([OptionKey] NVARCHAR(50) '$.key', [ValueKey] NVARCHAR(80) '$.productValue') AS selected
                LEFT JOIN [Store].[ProductPurchaseOption] AS purchaseOption
                    ON purchaseOption.[IdProduct] = item.[IdProduct] AND purchaseOption.[OptionKey] = selected.[OptionKey]
                LEFT JOIN [Store].[ProductPurchaseOptionValue] AS purchaseValue
                    ON purchaseValue.[IdProductPurchaseOption] = purchaseOption.[Id] AND purchaseValue.[ValueKey] = selected.[ValueKey]
                LEFT JOIN [Account].[CartItemPurchaseOptionValue] AS saved
                    ON saved.[IdCartItem] = item.[Id] AND saved.[IdProductPurchaseOptionValue] = purchaseValue.[Id]
                WHERE ISJSON(item.[OptionsJson]) = 1 AND saved.[Id] IS NULL
            )
                THROW 51070, 'A cart price option is not represented by its relational row; column was preserved.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM [Account].[CartItem] AS item
                CROSS APPLY OPENJSON(item.[OptionsJson], '$.accessories')
                    WITH ([AccessoryKey] NVARCHAR(150) '$.id', [Selected] BIT '$.selected') AS selected
                LEFT JOIN [Store].[Accessory] AS accessory ON accessory.[AccessoryKey] = selected.[AccessoryKey]
                LEFT JOIN [Store].[ProductAccessory] AS link
                    ON link.[IdProduct] = item.[IdProduct] AND link.[IdAccessory] = accessory.[Id]
                LEFT JOIN [Account].[CartItemAccessory] AS saved
                    ON saved.[IdCartItem] = item.[Id] AND saved.[IdProductAccessory] = link.[Id]
                WHERE ISJSON(item.[OptionsJson]) = 1 AND ISNULL(selected.[Selected], 1) = 1 AND saved.[Id] IS NULL
            )
                THROW 51071, 'A selected cart accessory is not represented by its relational row; column was preserved.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM [Account].[CartItem] AS item
                CROSS APPLY OPENJSON(item.[OptionsJson])
                    WITH ([LegacyVariantId] BIGINT '$.inventoryVariant') AS parsed
                WHERE ISJSON(item.[OptionsJson]) = 1 AND parsed.[LegacyVariantId] IS NOT NULL
                  AND (item.[IdProductVariant] IS NULL OR item.[IdProductVariant] <> parsed.[LegacyVariantId])
            )
                THROW 51072, 'A legacy inventory variant does not match CartItem.IdProductVariant; column was preserved.', 1;

            DECLARE @DefaultConstraint sysname;
            SELECT @DefaultConstraint = [dc].[name]
            FROM sys.default_constraints AS dc
            JOIN sys.columns AS columnInfo ON columnInfo.[object_id] = dc.[parent_object_id] AND columnInfo.[column_id] = dc.[parent_column_id]
            WHERE dc.[parent_object_id] = OBJECT_ID(N'Account.CartItem') AND columnInfo.[name] = N'OptionsJson';

            IF @DefaultConstraint IS NOT NULL
            BEGIN
                DECLARE @DropConstraintSql NVARCHAR(4000) = N'ALTER TABLE [Account].[CartItem] DROP CONSTRAINT ' + QUOTENAME(@DefaultConstraint) + N';';
                EXEC sys.sp_executesql @DropConstraintSql;
            END;

            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'Account.CartItem') AND [name] = N'CK_Account_CartItem_Options')
                ALTER TABLE [Account].[CartItem] DROP CONSTRAINT [CK_Account_CartItem_Options];

            ALTER TABLE [Account].[CartItem] DROP COLUMN [OptionsJson];
        END;

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'069-drop-cart-options-json');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
