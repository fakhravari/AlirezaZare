SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51800, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                   WHERE [MigrationKey] = N'180-system-user-warehouse-id-and-unique-pair')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                       WHERE [MigrationKey] = N'179-rename-system-user-warehouse-table')
            THROW 51801, 'Migration 179 must be applied first.', 1;
        IF OBJECT_ID(N'[Admin].[SystemUserWarehouse]', N'U') IS NULL
            THROW 51802, 'Admin.SystemUserWarehouse is required.', 1;

        IF COL_LENGTH(N'Admin.SystemUserWarehouse', N'Id') IS NULL
            ALTER TABLE [Admin].[SystemUserWarehouse] ADD [Id] bigint IDENTITY(1,1) NOT NULL;
        ELSE IF COLUMNPROPERTY(OBJECT_ID(N'[Admin].[SystemUserWarehouse]'), N'Id', 'IsIdentity') <> 1 OR
                EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'[Admin].[SystemUserWarehouse]')
                        AND name=N'Id' AND is_nullable=1)
            THROW 51803, 'Admin.SystemUserWarehouse.Id must be a non-null identity column.', 1;

        IF EXISTS
        (
            SELECT 1 FROM [Admin].[SystemUserWarehouse]
            GROUP BY [IdUser], [IdWarehouse]
            HAVING COUNT_BIG(*) > 1
        )
            THROW 51804, 'Duplicate user and warehouse pairs exist.', 1;

        DECLARE @primaryKey sysname;
        SELECT @primaryKey=[name]
        FROM sys.key_constraints
        WHERE parent_object_id=OBJECT_ID(N'[Admin].[SystemUserWarehouse]') AND [type]=N'PK';

        IF @primaryKey IS NOT NULL AND NOT EXISTS
        (
            SELECT 1
            FROM sys.key_constraints kc
            JOIN sys.index_columns ic ON ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE kc.parent_object_id=OBJECT_ID(N'[Admin].[SystemUserWarehouse]') AND kc.[name]=@primaryKey
            GROUP BY kc.[name]
            HAVING COUNT(*)=1 AND MAX(c.[name])=N'Id'
        )
        BEGIN
            DECLARE @dropPk nvarchar(max)=N'ALTER TABLE [Admin].[SystemUserWarehouse] DROP CONSTRAINT '+QUOTENAME(@primaryKey)+N';';
            EXEC sys.sp_executesql @dropPk;
            SET @primaryKey=NULL;
        END;

        IF @primaryKey IS NULL
            ALTER TABLE [Admin].[SystemUserWarehouse]
                ADD CONSTRAINT [PK_Admin_SystemUserWarehouse] PRIMARY KEY CLUSTERED ([Id]);

        IF NOT EXISTS
        (
            SELECT 1 FROM sys.key_constraints
            WHERE parent_object_id=OBJECT_ID(N'[Admin].[SystemUserWarehouse]')
              AND [name]=N'UQ_Admin_SystemUserWarehouse_User_Warehouse'
        )
            ALTER TABLE [Admin].[SystemUserWarehouse]
                ADD CONSTRAINT [UQ_Admin_SystemUserWarehouse_User_Warehouse]
                UNIQUE NONCLUSTERED ([IdUser], [IdWarehouse]);

        INSERT [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'180-system-user-warehouse-id-and-unique-pair');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
