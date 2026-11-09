SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'040-remove-site-identity-compatibility-view')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'039-consolidate-site-identity')
        THROW 51040, 'Site identity migration 039 must run first.', 1;
    IF OBJECT_ID(N'[Store].[SiteIdentity]', N'U') IS NOT NULL
        THROW 51040, 'The old site identity table still exists.', 1;
    IF OBJECT_ID(N'[Store].[SiteIdentity]', N'V') IS NOT NULL
        DROP VIEW [Store].[SiteIdentity];
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'040-remove-site-identity-compatibility-view');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
