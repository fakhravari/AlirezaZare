SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51560, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'156-product-feedback-status')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'155-independent-home-testimonials')
            THROW 51561, 'Migration 155 is required first.', 1;
        IF COL_LENGTH(N'Store.ProductFeedback', N'Status') IS NULL OR COL_LENGTH(N'Store.ProductFeedback', N'IdStatus') IS NOT NULL
            OR OBJECT_ID(N'Store.ProductFeedbackStatus', N'U') IS NOT NULL
            THROW 51562, 'Unexpected product feedback status schema.', 1;
        IF EXISTS (SELECT 1 FROM Store.ProductFeedback WHERE Status IS NULL OR Status COLLATE Latin1_General_100_BIN2 NOT IN (N'pending', N'approved', N'rejected'))
            THROW 51563, 'Unknown product feedback status; migration stopped.', 1;
        -- The original schema has this known status constraint; the new foreign key replaces it.
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'Store.ProductFeedback') AND name = N'CK_Store_ProductFeedback_Status')
            ALTER TABLE Store.ProductFeedback DROP CONSTRAINT CK_Store_ProductFeedback_Status;
        IF EXISTS (SELECT 1 FROM sys.sql_expression_dependencies d JOIN sys.columns c ON c.object_id = d.referenced_id AND c.column_id = d.referenced_minor_id
            WHERE d.referenced_id = OBJECT_ID(N'Store.ProductFeedback') AND c.name = N'Status')
            THROW 51564, 'A SQL module references ProductFeedback.Status; review it first.', 1;
        IF EXISTS (SELECT 1 FROM sys.check_constraints c WHERE c.parent_object_id = OBJECT_ID(N'Store.ProductFeedback') AND c.definition LIKE N'%Status%')
            THROW 51565, 'A check constraint references ProductFeedback.Status; review it first.', 1;
        IF EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                   JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                   WHERE i.object_id = OBJECT_ID(N'Store.ProductFeedback') AND c.name = N'Status'
                     AND (i.is_unique = 1 OR i.has_filter = 1 OR i.type <> 2 OR
                          (SELECT COUNT(*) FROM sys.index_columns allcols WHERE allcols.object_id = i.object_id AND allcols.index_id = i.index_id) <> 4 OR
                          NOT EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns col ON col.object_id = k.object_id AND col.column_id = k.column_id WHERE k.object_id = i.object_id AND k.index_id = i.index_id AND k.key_ordinal = 1 AND col.name = N'IdProduct') OR
                          NOT EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns col ON col.object_id = k.object_id AND col.column_id = k.column_id WHERE k.object_id = i.object_id AND k.index_id = i.index_id AND k.key_ordinal = 2 AND col.name = N'FeedbackType') OR
                          ic.key_ordinal <> 3 OR
                          NOT EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns col ON col.object_id = k.object_id AND col.column_id = k.column_id WHERE k.object_id = i.object_id AND k.index_id = i.index_id AND k.key_ordinal = 4 AND col.name = N'CreatedAtUtc')))
            THROW 51566, 'An unexpected index references ProductFeedback.Status; review it first.', 1;
        CREATE TABLE Store.ProductFeedbackStatus (Id int NOT NULL CONSTRAINT PK_ProductFeedbackStatus PRIMARY KEY, Name nvarchar(80) NOT NULL);
        INSERT Store.ProductFeedbackStatus (Id, Name) VALUES (1, N'در انتظار بررسی'), (2, N'تأییدشده'), (3, N'ردشده');
        ALTER TABLE Store.ProductFeedback ADD IdStatus int NULL;
        EXEC sys.sp_executesql N'UPDATE Store.ProductFeedback SET IdStatus = CASE Status WHEN N''pending'' THEN 1 WHEN N''approved'' THEN 2 WHEN N''rejected'' THEN 3 END;';
        EXEC sys.sp_executesql N'ALTER TABLE Store.ProductFeedback ALTER COLUMN IdStatus int NOT NULL;';
        EXEC sys.sp_executesql N'ALTER TABLE Store.ProductFeedback WITH CHECK ADD CONSTRAINT FK_ProductFeedback_Status FOREIGN KEY (IdStatus) REFERENCES Store.ProductFeedbackStatus(Id);';
        DECLARE @indexName sysname, @sql nvarchar(max);
        DECLARE statusIndexes CURSOR LOCAL FAST_FORWARD FOR
            SELECT DISTINCT i.name FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(N'Store.ProductFeedback') AND c.name = N'Status';
        OPEN statusIndexes;
        FETCH NEXT FROM statusIndexes INTO @indexName;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sql = N'DROP INDEX ' + QUOTENAME(@indexName) + N' ON Store.ProductFeedback;';
            EXEC sys.sp_executesql @sql;
            FETCH NEXT FROM statusIndexes INTO @indexName;
        END;
        CLOSE statusIndexes;
        DEALLOCATE statusIndexes;
        DECLARE @defaultName sysname;
        SELECT @defaultName = d.name FROM sys.default_constraints d JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID(N'Store.ProductFeedback') AND c.name = N'Status';
        IF @defaultName IS NOT NULL
        BEGIN
            SET @sql = N'ALTER TABLE Store.ProductFeedback DROP CONSTRAINT ' + QUOTENAME(@defaultName);
            EXEC sys.sp_executesql @sql;
        END;
        ALTER TABLE Store.ProductFeedback DROP COLUMN Status;
        EXEC sys.sp_executesql N'CREATE INDEX IX_ProductFeedback_IdProduct_FeedbackType_IdStatus_CreatedAtUtc ON Store.ProductFeedback (IdProduct, FeedbackType, IdStatus, CreatedAtUtc);';
        EXEC sys.sp_executesql N'CREATE INDEX IX_ProductFeedback_IdStatus ON Store.ProductFeedback (IdStatus);';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'156-product-feedback-status');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
