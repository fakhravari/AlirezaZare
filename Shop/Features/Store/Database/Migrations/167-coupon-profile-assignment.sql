SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51670, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'167-coupon-profile-assignment')
    BEGIN
        IF OBJECT_ID(N'Store.Coupon', N'U') IS NULL OR OBJECT_ID(N'Account.Profile', N'U') IS NULL
            THROW 51671, 'Coupon and profile tables are required.', 1;
        IF COL_LENGTH(N'Store.Coupon', N'IdProfile') IS NOT NULL
            THROW 51672, 'Coupon profile column already exists; inspect schema.', 1;
        ALTER TABLE Store.Coupon ADD IdProfile bigint NULL;
        EXEC sys.sp_executesql N'ALTER TABLE Store.Coupon WITH CHECK ADD CONSTRAINT FK_Coupon_Profile FOREIGN KEY (IdProfile) REFERENCES Account.Profile(Id);';
        EXEC sys.sp_executesql N'CREATE INDEX IX_Coupon_IdProfile ON Store.Coupon(IdProfile) WHERE IdProfile IS NOT NULL;';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'167-coupon-profile-assignment');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
