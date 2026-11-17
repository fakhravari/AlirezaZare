SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'118-price-version-changed-by-user')
    BEGIN
        IF COL_LENGTH(N'Store.PriceVersion', N'ChangedBy') IS NULL OR COL_LENGTH(N'Store.PriceVersion', N'IdChangedBy') IS NOT NULL
            THROW 51260, 'Unexpected PriceVersion actor columns; review the schema.', 1;
        DECLARE @adminId bigint = (SELECT Id FROM Admin.SystemUser WHERE NormalizedUserName = N'ADMIN');
        IF @adminId IS NULL THROW 51261, 'The admin user is required for legacy price versions.', 1;
        SELECT Id AS IdPriceVersion, ChangedBy INTO Store.PriceVersionChangedByArchive118
        FROM Store.PriceVersion WHERE ChangedBy IS NOT NULL;
        ALTER TABLE Store.PriceVersion ADD IdChangedBy bigint NULL;
        EXEC sys.sp_executesql N'UPDATE Store.PriceVersion SET IdChangedBy = @adminId;', N'@adminId bigint', @adminId;
        ALTER TABLE Store.PriceVersion ALTER COLUMN IdChangedBy bigint NOT NULL;
        ALTER TABLE Store.PriceVersion WITH CHECK ADD CONSTRAINT FK_PriceVersion_ChangedByUser
            FOREIGN KEY (IdChangedBy) REFERENCES Admin.SystemUser(Id);
        ALTER TABLE Store.PriceVersion DROP COLUMN ChangedBy;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'118-price-version-changed-by-user');
    END
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
