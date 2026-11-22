SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'150-tree-path-columns')
    BEGIN
        IF OBJECT_ID(N'Store.Category', N'U') IS NULL OR OBJECT_ID(N'Warehouse.Unit', N'U') IS NULL OR OBJECT_ID(N'Warehouse.VariantName', N'U') IS NULL
            THROW 51500, 'Expected tree tables are missing.', 1;

        IF COL_LENGTH(N'Store.Category', N'TreePathIds') IS NULL ALTER TABLE Store.Category ADD TreePathIds nvarchar(4000) NOT NULL CONSTRAINT DF_Store_Category_TreePathIds DEFAULT N'';
        IF COL_LENGTH(N'Store.Category', N'TreePathNames') IS NULL ALTER TABLE Store.Category ADD TreePathNames nvarchar(4000) NOT NULL CONSTRAINT DF_Store_Category_TreePathNames DEFAULT N'';
        IF COL_LENGTH(N'Warehouse.Unit', N'TreePathIds') IS NULL ALTER TABLE Warehouse.Unit ADD TreePathIds nvarchar(4000) NOT NULL CONSTRAINT DF_Warehouse_Unit_TreePathIds DEFAULT N'';
        IF COL_LENGTH(N'Warehouse.Unit', N'TreePathNames') IS NULL ALTER TABLE Warehouse.Unit ADD TreePathNames nvarchar(4000) NOT NULL CONSTRAINT DF_Warehouse_Unit_TreePathNames DEFAULT N'';
        IF COL_LENGTH(N'Warehouse.VariantName', N'TreePathIds') IS NULL ALTER TABLE Warehouse.VariantName ADD TreePathIds nvarchar(4000) NOT NULL CONSTRAINT DF_Warehouse_VariantName_TreePathIds DEFAULT N'';
        IF COL_LENGTH(N'Warehouse.VariantName', N'TreePathNames') IS NULL ALTER TABLE Warehouse.VariantName ADD TreePathNames nvarchar(4000) NOT NULL CONSTRAINT DF_Warehouse_VariantName_TreePathNames DEFAULT N'';

        EXEC(N';WITH T AS (
            SELECT Id, IdParentCategory, CONVERT(nvarchar(4000), RIGHT(''00''+CONVERT(varchar(20),Id),2)) TreePathIds, CONVERT(nvarchar(4000),NameFa) TreePathNames FROM Store.Category WHERE IdParentCategory IS NULL
            UNION ALL SELECT c.Id,c.IdParentCategory,CONVERT(nvarchar(4000),p.TreePathIds+N'' / ''+RIGHT(''00''+CONVERT(varchar(20),c.Id),2)),CONVERT(nvarchar(4000),p.TreePathNames+N'' / ''+c.NameFa) FROM Store.Category c JOIN T p ON c.IdParentCategory=p.Id
        ) UPDATE c SET TreePathIds=t.TreePathIds,TreePathNames=t.TreePathNames FROM Store.Category c JOIN T t ON t.Id=c.Id OPTION (MAXRECURSION 32767);');
        EXEC(N';WITH T AS (
            SELECT Id, IdParent, CONVERT(nvarchar(4000), RIGHT(''00''+CONVERT(varchar(20),Id),2)) TreePathIds, CONVERT(nvarchar(4000),NameFa) TreePathNames FROM Warehouse.Unit WHERE IdParent IS NULL
            UNION ALL SELECT c.Id,c.IdParent,CONVERT(nvarchar(4000),p.TreePathIds+N'' / ''+RIGHT(''00''+CONVERT(varchar(20),c.Id),2)),CONVERT(nvarchar(4000),p.TreePathNames+N'' / ''+c.NameFa) FROM Warehouse.Unit c JOIN T p ON c.IdParent=p.Id
        ) UPDATE c SET TreePathIds=t.TreePathIds,TreePathNames=t.TreePathNames FROM Warehouse.Unit c JOIN T t ON t.Id=c.Id OPTION (MAXRECURSION 32767);');
        EXEC(N';WITH T AS (
            SELECT Id, IdParent, CONVERT(nvarchar(4000), RIGHT(''00''+CONVERT(varchar(20),Id),2)) TreePathIds, CONVERT(nvarchar(4000),Name) TreePathNames FROM Warehouse.VariantName WHERE IdParent IS NULL
            UNION ALL SELECT c.Id,c.IdParent,CONVERT(nvarchar(4000),p.TreePathIds+N'' / ''+RIGHT(''00''+CONVERT(varchar(20),c.Id),2)),CONVERT(nvarchar(4000),p.TreePathNames+N'' / ''+c.Name) FROM Warehouse.VariantName c JOIN T p ON c.IdParent=p.Id
        ) UPDATE c SET TreePathIds=t.TreePathIds,TreePathNames=t.TreePathNames FROM Warehouse.VariantName c JOIN T t ON t.Id=c.Id OPTION (MAXRECURSION 32767);');
        EXEC(N'IF EXISTS(SELECT 1 FROM Store.Category WHERE TreePathIds=N'''' OR TreePathNames=N'''') OR EXISTS(SELECT 1 FROM Warehouse.Unit WHERE TreePathIds=N'''' OR TreePathNames=N'''') OR EXISTS(SELECT 1 FROM Warehouse.VariantName WHERE TreePathIds=N'''' OR TreePathNames=N'''') THROW 51501, ''A tree row is unreachable from a root; paths were not fully backfilled.'', 1;');

        EXEC(N'CREATE OR ALTER TRIGGER Store.TR_Category_TreePath ON Store.Category AFTER INSERT, UPDATE AS
        BEGIN SET NOCOUNT ON; IF TRIGGER_NESTLEVEL()>1 OR (NOT UPDATE(IdParentCategory) AND NOT UPDATE(NameFa) AND NOT EXISTS(SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id WHERE d.Id IS NULL)) RETURN;
        ;WITH T AS (SELECT Id,IdParentCategory,CONVERT(nvarchar(4000),RIGHT(''00''+CONVERT(varchar(20),Id),2)) TreePathIds,CONVERT(nvarchar(4000),NameFa) TreePathNames FROM Store.Category WHERE IdParentCategory IS NULL UNION ALL SELECT c.Id,c.IdParentCategory,CONVERT(nvarchar(4000),p.TreePathIds+N'' / ''+RIGHT(''00''+CONVERT(varchar(20),c.Id),2)),CONVERT(nvarchar(4000),p.TreePathNames+N'' / ''+c.NameFa) FROM Store.Category c JOIN T p ON c.IdParentCategory=p.Id) UPDATE c SET TreePathIds=t.TreePathIds,TreePathNames=t.TreePathNames FROM Store.Category c JOIN T t ON t.Id=c.Id OPTION(MAXRECURSION 32767); END');
        EXEC(N'CREATE OR ALTER TRIGGER Warehouse.TR_Unit_TreePath ON Warehouse.Unit AFTER INSERT, UPDATE AS
        BEGIN SET NOCOUNT ON; IF TRIGGER_NESTLEVEL()>1 OR (NOT UPDATE(IdParent) AND NOT UPDATE(NameFa) AND NOT EXISTS(SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id WHERE d.Id IS NULL)) RETURN;
        ;WITH T AS (SELECT Id,IdParent,CONVERT(nvarchar(4000),RIGHT(''00''+CONVERT(varchar(20),Id),2)) TreePathIds,CONVERT(nvarchar(4000),NameFa) TreePathNames FROM Warehouse.Unit WHERE IdParent IS NULL UNION ALL SELECT c.Id,c.IdParent,CONVERT(nvarchar(4000),p.TreePathIds+N'' / ''+RIGHT(''00''+CONVERT(varchar(20),c.Id),2)),CONVERT(nvarchar(4000),p.TreePathNames+N'' / ''+c.NameFa) FROM Warehouse.Unit c JOIN T p ON c.IdParent=p.Id) UPDATE c SET TreePathIds=t.TreePathIds,TreePathNames=t.TreePathNames FROM Warehouse.Unit c JOIN T t ON t.Id=c.Id OPTION(MAXRECURSION 32767); END');
        EXEC(N'CREATE OR ALTER TRIGGER Warehouse.TR_VariantName_TreePath ON Warehouse.VariantName AFTER INSERT, UPDATE AS
        BEGIN SET NOCOUNT ON; IF TRIGGER_NESTLEVEL()>1 OR (NOT UPDATE(IdParent) AND NOT UPDATE(Name) AND NOT EXISTS(SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id WHERE d.Id IS NULL)) RETURN;
        ;WITH T AS (SELECT Id,IdParent,CONVERT(nvarchar(4000),RIGHT(''00''+CONVERT(varchar(20),Id),2)) TreePathIds,CONVERT(nvarchar(4000),Name) TreePathNames FROM Warehouse.VariantName WHERE IdParent IS NULL UNION ALL SELECT c.Id,c.IdParent,CONVERT(nvarchar(4000),p.TreePathIds+N'' / ''+RIGHT(''00''+CONVERT(varchar(20),c.Id),2)),CONVERT(nvarchar(4000),p.TreePathNames+N'' / ''+c.Name) FROM Warehouse.VariantName c JOIN T p ON c.IdParent=p.Id) UPDATE c SET TreePathIds=t.TreePathIds,TreePathNames=t.TreePathNames FROM Warehouse.VariantName c JOIN T t ON t.Id=c.Id OPTION(MAXRECURSION 32767); END');
        INSERT Store.SchemaMigration(MigrationKey) VALUES(N'150-tree-path-columns');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
