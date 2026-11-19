SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
    THROW 51320, 'Store.SchemaMigration is required before migration 132.', 1;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'132-warehouse-unit-parent-tree')
BEGIN
    IF COL_LENGTH(N'Warehouse.Unit', N'IdParent') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] ADD [IdParent] BIGINT NULL;';
    IF COL_LENGTH(N'Warehouse.Unit', N'SortOrder') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] ADD [SortOrder] INT NOT NULL CONSTRAINT [DF_Warehouse_Unit_SortOrder] DEFAULT (0) WITH VALUES;';

    CREATE TABLE #Groups ([NameFa] NVARCHAR(100) NOT NULL, [NameEn] NVARCHAR(100) NOT NULL);
    INSERT #Groups ([NameFa], [NameEn]) VALUES
        (N'شمارشی', N'Countable'), (N'بسته‌بندی', N'Packaging'),
        (N'وزن', N'Mass'), (N'طول', N'Length'), (N'مساحت', N'Area'),
        (N'حجم', N'Volume'), (N'توان', N'Power'), (N'ظرفیت سرمایش', N'Refrigeration Capacity'),
        (N'زمان', N'Time'), (N'دما', N'Temperature'), (N'چگالی', N'Density'),
        (N'فشار', N'Pressure'), (N'گرانروی', N'Viscosity'),
        (N'غلظت جرمی', N'Mass Fraction'), (N'درصد', N'Percentage');

    CREATE TABLE #GroupIds ([NameFa] NVARCHAR(100) NOT NULL PRIMARY KEY, [Id] BIGINT NOT NULL);
    IF COL_LENGTH(N'Warehouse.Unit', N'IdUnitGroup') IS NOT NULL
    BEGIN
        DECLARE @FallbackUnitGroupId BIGINT = (SELECT TOP (1) [Id] FROM [Warehouse].[UnitGroup] ORDER BY [Id]);
        IF @FallbackUnitGroupId IS NULL
            THROW 51323, 'A temporary unit group is required while migrating legacy rows.', 1;
        EXEC sys.sp_executesql N'
            INSERT [Warehouse].[Unit] ([NameFa], [NameEn], [IsActive], [IdUnitGroup])
            OUTPUT inserted.[NameFa], inserted.[Id] INTO #GroupIds ([NameFa], [Id])
            SELECT g.[NameFa], g.[NameEn], 1, @GroupId FROM #Groups g;',
            N'@GroupId BIGINT', @GroupId = @FallbackUnitGroupId;
    END
    ELSE
    BEGIN
        INSERT [Warehouse].[Unit] ([NameFa], [NameEn], [IsActive])
        OUTPUT inserted.[NameFa], inserted.[Id] INTO #GroupIds ([NameFa], [Id])
        SELECT g.[NameFa], g.[NameEn], 1 FROM #Groups g;
    END;

    CREATE TABLE #Assignments ([UnitId] BIGINT NOT NULL, [GroupNameFa] NVARCHAR(100) NOT NULL);
    INSERT #Assignments ([UnitId], [GroupNameFa]) VALUES
        (1,N'شمارشی'),(2,N'شمارشی'),(3,N'شمارشی'),(4,N'شمارشی'),(5,N'شمارشی'),(6,N'شمارشی'),(7,N'شمارشی'),(8,N'شمارشی'),
        (9,N'شمارشی'),(10,N'شمارشی'),(11,N'شمارشی'),(12,N'شمارشی'),(13,N'شمارشی'),(14,N'شمارشی'),(15,N'شمارشی'),(16,N'شمارشی'),
        (17,N'بسته‌بندی'),(18,N'بسته‌بندی'),(19,N'بسته‌بندی'),(20,N'بسته‌بندی'),(21,N'بسته‌بندی'),(22,N'بسته‌بندی'),(23,N'بسته‌بندی'),
        (24,N'بسته‌بندی'),(25,N'بسته‌بندی'),(58,N'بسته‌بندی'),(60,N'بسته‌بندی'),(61,N'بسته‌بندی'),(62,N'بسته‌بندی'),(66,N'بسته‌بندی'),(68,N'بسته‌بندی'),
        (26,N'وزن'),(27,N'وزن'),(28,N'وزن'),(63,N'وزن'),
        (29,N'طول'),(30,N'طول'),(31,N'طول'),(32,N'طول'),(33,N'طول'),(34,N'طول'),
        (35,N'مساحت'),(59,N'مساحت'),(53,N'حجم'),(54,N'حجم'),(73,N'حجم'),
        (36,N'توان'),(37,N'توان'),(38,N'توان'),(39,N'توان'),(40,N'توان'),(41,N'توان'),(42,N'توان'),
        (43,N'ظرفیت سرمایش'),(44,N'زمان'),(45,N'زمان'),(46,N'زمان'),(47,N'زمان'),(48,N'زمان'),(49,N'زمان'),(50,N'زمان'),(51,N'زمان'),
        (52,N'چگالی'),(55,N'دما'),(56,N'دما'),(64,N'دما'),(57,N'فشار'),(65,N'گرانروی'),(67,N'غلظت جرمی'),(69,N'درصد'),
        (70,N'شمارشی'),(71,N'شمارشی'),(72,N'شمارشی');

    EXEC sys.sp_executesql N'
        IF (SELECT COUNT_BIG(*) FROM #GroupIds) <> 15
            THROW 51321, ''Unit category roots could not be created.'', 1;

        UPDATE u
        SET u.[IdParent] = parent.[Id]
        FROM [Warehouse].[Unit] u
        JOIN #Assignments a ON a.[UnitId] = u.[Id]
        JOIN #GroupIds parent ON parent.[NameFa] = a.[GroupNameFa]
        WHERE u.[IdParent] IS NULL AND u.[Id] NOT IN (SELECT [Id] FROM #GroupIds);

        IF EXISTS (SELECT 1 FROM #Assignments a LEFT JOIN [Warehouse].[Unit] u ON u.[Id] = a.[UnitId]
                   WHERE u.[Id] IS NULL OR u.[IdParent] IS NULL)
            THROW 51322, ''Expected seeded warehouse units are missing; parent groups were not assigned.'', 1;';

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Warehouse_Unit_UnitGroup' AND [parent_object_id] = OBJECT_ID(N'[Warehouse].[Unit]'))
        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] DROP CONSTRAINT [FK_Warehouse_Unit_UnitGroup];';
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Warehouse_Unit_IdUnitGroup' AND [object_id] = OBJECT_ID(N'[Warehouse].[Unit]'))
        EXEC sys.sp_executesql N'DROP INDEX [IX_Warehouse_Unit_IdUnitGroup] ON [Warehouse].[Unit];';
    IF COL_LENGTH(N'Warehouse.Unit', N'IdUnitGroup') IS NOT NULL
        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] DROP COLUMN [IdUnitGroup];';

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Warehouse_Unit_Parent' AND [parent_object_id] = OBJECT_ID(N'[Warehouse].[Unit]'))
        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] ADD CONSTRAINT [FK_Warehouse_Unit_Parent] FOREIGN KEY ([IdParent]) REFERENCES [Warehouse].[Unit]([Id]);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Warehouse_Unit_IdParent' AND [object_id] = OBJECT_ID(N'[Warehouse].[Unit]'))
        EXEC sys.sp_executesql N'CREATE INDEX [IX_Warehouse_Unit_IdParent] ON [Warehouse].[Unit]([IdParent]);';

    IF OBJECT_ID(N'[Warehouse].[UnitGroup]', N'U') IS NOT NULL
        EXEC sys.sp_executesql N'DROP TABLE [Warehouse].[UnitGroup];';

    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'132-warehouse-unit-parent-tree');
END;

COMMIT TRANSACTION;
