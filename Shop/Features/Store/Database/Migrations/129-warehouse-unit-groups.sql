SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'129-warehouse-unit-groups')
    BEGIN
        IF OBJECT_ID(N'[Warehouse].[Unit]', N'U') IS NULL
            THROW 51290, 'Warehouse.Unit is missing.', 1;

        IF OBJECT_ID(N'[Warehouse].[UnitGroup]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Warehouse].[UnitGroup]
            (
                [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Warehouse_UnitGroup] PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_UnitGroup_IsActive] DEFAULT (1),
                [SortOrder] INT NOT NULL CONSTRAINT [DF_Warehouse_UnitGroup_SortOrder] DEFAULT (0)
            );
            CREATE UNIQUE INDEX [UX_Warehouse_UnitGroup_Name] ON [Warehouse].[UnitGroup]([Name]);
            CREATE INDEX [IX_Warehouse_UnitGroup_SortOrder_Name] ON [Warehouse].[UnitGroup]([SortOrder], [Name]);
        END;

        IF COL_LENGTH(N'Warehouse.Unit', N'IdUnitGroup') IS NULL
            ALTER TABLE [Warehouse].[Unit] ADD [IdUnitGroup] BIGINT NULL;

        IF NOT EXISTS (SELECT 1 FROM [Warehouse].[UnitGroup])
            INSERT [Warehouse].[UnitGroup] ([Name], [IsActive], [SortOrder]) VALUES (N'سایر واحدها', 1, 0);

        DECLARE @DefaultGroupId BIGINT = (SELECT TOP (1) [Id] FROM [Warehouse].[UnitGroup] ORDER BY [SortOrder], [Id]);
        EXEC sys.sp_executesql
            N'UPDATE [Warehouse].[Unit] SET [IdUnitGroup] = @GroupId WHERE [IdUnitGroup] IS NULL',
            N'@GroupId BIGINT', @GroupId = @DefaultGroupId;

        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] ALTER COLUMN [IdUnitGroup] BIGINT NOT NULL';
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Warehouse_Unit_UnitGroup' AND [parent_object_id] = OBJECT_ID(N'[Warehouse].[Unit]'))
            EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] ADD CONSTRAINT [FK_Warehouse_Unit_UnitGroup] FOREIGN KEY ([IdUnitGroup]) REFERENCES [Warehouse].[UnitGroup]([Id])';
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Warehouse_Unit_IdUnitGroup' AND [object_id] = OBJECT_ID(N'[Warehouse].[Unit]'))
            EXEC sys.sp_executesql N'CREATE INDEX [IX_Warehouse_Unit_IdUnitGroup] ON [Warehouse].[Unit]([IdUnitGroup])';

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'129-warehouse-unit-groups');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
