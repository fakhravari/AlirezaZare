SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51610, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'161-payment-attempt-numeric-identity')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'159-status-reference-tables')
            THROW 51611, 'Migration 159 is required first.', 1;
        IF OBJECT_ID(N'Store.PaymentAttempt', N'U') IS NULL
            OR COL_LENGTH(N'Store.PaymentAttempt', N'Id') IS NULL
            THROW 51612, 'Store.PaymentAttempt.Id is required.', 1;
        IF NOT EXISTS
        (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'Store.PaymentAttempt')
              AND name = N'Id' AND TYPE_NAME(system_type_id) = N'uniqueidentifier'
        )
            THROW 51613, 'Store.PaymentAttempt.Id is not the expected uniqueidentifier column.', 1;
        IF EXISTS
        (
            SELECT 1 FROM sys.foreign_keys
            WHERE referenced_object_id = OBJECT_ID(N'Store.PaymentAttempt')
        )
            THROW 51614, 'A foreign key references Store.PaymentAttempt; inspect before migration.', 1;

        DECLARE @PrimaryKey sysname;
        SELECT @PrimaryKey = name
        FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'Store.PaymentAttempt') AND type = N'PK';
        IF @PrimaryKey IS NULL
            THROW 51615, 'Store.PaymentAttempt primary key is missing.', 1;

        ALTER TABLE Store.PaymentAttempt ADD PaymentKey numeric(30,0) IDENTITY(1,1) NOT NULL;
        DECLARE @DropPrimaryKey nvarchar(500) = N'ALTER TABLE Store.PaymentAttempt DROP CONSTRAINT ' + QUOTENAME(@PrimaryKey) + N';';
        EXEC sys.sp_executesql @DropPrimaryKey;
        ALTER TABLE Store.PaymentAttempt DROP COLUMN Id;
        EXEC sys.sp_rename N'Store.PaymentAttempt.PaymentKey', N'Id', N'COLUMN';
        ALTER TABLE Store.PaymentAttempt ADD CONSTRAINT PK_PaymentAttempt PRIMARY KEY (Id);

        INSERT Store.SchemaMigration (MigrationKey)
        VALUES (N'161-payment-attempt-numeric-identity');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
