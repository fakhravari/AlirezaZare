SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL OR OBJECT_ID(N'Account.ArticleFeedback', N'U') IS NULL
        THROW 51650, 'The migration ledger and article feedback table are required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'165-article-feedback-status')
    BEGIN
        IF EXISTS (SELECT 1 FROM Account.ArticleFeedback WHERE Status NOT IN (N'pending', N'approved', N'rejected'))
            THROW 51651, 'Unknown article feedback status; inspect before migration.', 1;

        IF OBJECT_ID(N'Account.ArticleFeedbackStatus', N'U') IS NULL
            EXEC sys.sp_executesql N'CREATE TABLE Account.ArticleFeedbackStatus (Code nvarchar(20) NOT NULL CONSTRAINT PK_ArticleFeedbackStatus PRIMARY KEY, NameFa nvarchar(80) NOT NULL, NameEn nvarchar(80) NOT NULL);';

        EXEC sys.sp_executesql N'
            IF EXISTS (SELECT 1 FROM Account.ArticleFeedbackStatus WHERE Code NOT IN (N''pending'', N''approved'', N''rejected''))
                THROW 51652, ''Unknown article feedback status reference; inspect before migration.'', 1;
            IF NOT EXISTS (SELECT 1 FROM Account.ArticleFeedbackStatus WHERE Code = N''pending'')
                INSERT Account.ArticleFeedbackStatus (Code, NameFa, NameEn) VALUES (N''pending'', N''در انتظار بررسی'', N''Pending review'');
            IF NOT EXISTS (SELECT 1 FROM Account.ArticleFeedbackStatus WHERE Code = N''approved'')
                INSERT Account.ArticleFeedbackStatus (Code, NameFa, NameEn) VALUES (N''approved'', N''تأییدشده'', N''Approved'');
            IF NOT EXISTS (SELECT 1 FROM Account.ArticleFeedbackStatus WHERE Code = N''rejected'')
                INSERT Account.ArticleFeedbackStatus (Code, NameFa, NameEn) VALUES (N''rejected'', N''ردشده'', N''Rejected'');
            UPDATE Account.ArticleFeedbackStatus SET
                NameFa = CASE Code WHEN N''pending'' THEN N''در انتظار بررسی'' WHEN N''approved'' THEN N''تأییدشده'' ELSE N''ردشده'' END,
                NameEn = CASE Code WHEN N''pending'' THEN N''Pending review'' WHEN N''approved'' THEN N''Approved'' ELSE N''Rejected'' END;';

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'Account.ArticleFeedback') AND name = N'FK_ArticleFeedback_Status')
            EXEC sys.sp_executesql N'ALTER TABLE Account.ArticleFeedback WITH CHECK ADD CONSTRAINT FK_ArticleFeedback_Status FOREIGN KEY (Status) REFERENCES Account.ArticleFeedbackStatus(Code);';
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'Account.ArticleFeedback') AND name = N'CK_Account_ArticleFeedback_Status')
            ALTER TABLE Account.ArticleFeedback DROP CONSTRAINT CK_Account_ArticleFeedback_Status;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'165-article-feedback-status');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
