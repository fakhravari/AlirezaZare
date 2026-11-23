SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL OR OBJECT_ID(N'Store.ContactFormMessage', N'U') IS NULL
        THROW 51630, 'The migration ledger and contact message table are required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'163-contact-message-status')
    BEGIN
        IF EXISTS (SELECT 1 FROM Store.ContactFormMessage WHERE Status NOT IN (N'new', N'read', N'closed'))
            THROW 51631, 'Unknown contact message status; inspect before migration.', 1;

        IF OBJECT_ID(N'Store.ContactMessageStatus', N'U') IS NULL
            EXEC sys.sp_executesql N'CREATE TABLE Store.ContactMessageStatus (Code nvarchar(20) NOT NULL CONSTRAINT PK_ContactMessageStatus PRIMARY KEY, NameFa nvarchar(80) NOT NULL, NameEn nvarchar(80) NOT NULL);';

        EXEC sys.sp_executesql N'
            IF EXISTS (SELECT 1 FROM Store.ContactMessageStatus WHERE Code NOT IN (N''new'', N''read'', N''closed''))
                THROW 51632, ''Unknown contact status reference; inspect before migration.'', 1;
            IF NOT EXISTS (SELECT 1 FROM Store.ContactMessageStatus WHERE Code = N''new'')
                INSERT Store.ContactMessageStatus (Code, NameFa, NameEn) VALUES (N''new'', N''جدید'', N''New'');
            IF NOT EXISTS (SELECT 1 FROM Store.ContactMessageStatus WHERE Code = N''read'')
                INSERT Store.ContactMessageStatus (Code, NameFa, NameEn) VALUES (N''read'', N''در حال بررسی'', N''In progress'');
            IF NOT EXISTS (SELECT 1 FROM Store.ContactMessageStatus WHERE Code = N''closed'')
                INSERT Store.ContactMessageStatus (Code, NameFa, NameEn) VALUES (N''closed'', N''اتمام'', N''Completed'');
            UPDATE Store.ContactMessageStatus SET
                NameFa = CASE Code WHEN N''new'' THEN N''جدید'' WHEN N''read'' THEN N''در حال بررسی'' ELSE N''اتمام'' END,
                NameEn = CASE Code WHEN N''new'' THEN N''New'' WHEN N''read'' THEN N''In progress'' ELSE N''Completed'' END;';

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'Store.ContactFormMessage') AND name = N'FK_ContactFormMessage_Status')
            EXEC sys.sp_executesql N'ALTER TABLE Store.ContactFormMessage WITH CHECK ADD CONSTRAINT FK_ContactFormMessage_Status FOREIGN KEY (Status) REFERENCES Store.ContactMessageStatus(Code);';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'163-contact-message-status');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
