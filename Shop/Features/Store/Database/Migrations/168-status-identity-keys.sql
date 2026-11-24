SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51680, 'Migration ledger is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'168-status-identity-keys')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'167-coupon-profile-assignment')
            THROW 51681, 'Migration 167 is required.', 1;

        DECLARE @tables TABLE (EntityName nvarchar(256), ReferenceName nvarchar(256), ForeignKeyName sysname);
        INSERT @tables VALUES
            (N'Account.ArticleFeedback', N'Account.ArticleFeedbackStatus', N'FK_ArticleFeedback_Status'),
            (N'Store.ContactFormMessage', N'Store.ContactMessageStatus', N'FK_ContactFormMessage_Status'),
            (N'Store.PaymentAttempt', N'Store.PaymentAttemptStatus', N'FK_PaymentAttempt_Status'),
            (N'Warehouse.WarehouseDocument', N'Warehouse.DocumentStatus', N'FK_WarehouseDocument_Status');

        DECLARE @entity nvarchar(256), @reference nvarchar(256), @foreignKey sysname;
        DECLARE @sql nvarchar(max), @primaryKey sysname, @default sysname;
        DECLARE @entityQuoted nvarchar(520), @referenceQuoted nvarchar(520);
        DECLARE status_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT EntityName, ReferenceName, ForeignKeyName FROM @tables;
        OPEN status_cursor;
        FETCH NEXT FROM status_cursor INTO @entity, @reference, @foreignKey;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF OBJECT_ID(@entity, N'U') IS NULL OR OBJECT_ID(@reference, N'U') IS NULL
                OR COL_LENGTH(@entity, N'IdStatus') IS NOT NULL OR COL_LENGTH(@reference, N'Id') IS NOT NULL
                THROW 51682, 'Unexpected status schema.', 1;

            SET @entityQuoted = QUOTENAME(PARSENAME(@entity, 2)) + N'.' + QUOTENAME(PARSENAME(@entity, 1));
            SET @referenceQuoted = QUOTENAME(PARSENAME(@reference, 2)) + N'.' + QUOTENAME(PARSENAME(@reference, 1));
            SELECT @primaryKey = name FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(@reference) AND type = N'PK';
            IF @primaryKey IS NULL OR NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(@entity) AND name = @foreignKey)
                THROW 51683, 'Expected status keys are missing.', 1;

            IF @entity = N'Store.ContactFormMessage'
                DROP INDEX IX_Store_ContactFormMessage_Status_CreatedAtUtc ON Store.ContactFormMessage;
            IF @entity = N'Store.PaymentAttempt'
            BEGIN
                DROP INDEX UX_Store_PaymentAttempt_VerifiedOrder ON Store.PaymentAttempt;
                ALTER TABLE Store.PaymentAttempt DROP CONSTRAINT CK_Store_PaymentAttempt_Status;
                ALTER TABLE Store.PaymentAttempt DROP CONSTRAINT CK_Store_PaymentAttempt_Verification;
            END;
            IF @entity = N'Warehouse.WarehouseDocument'
                ALTER TABLE Warehouse.WarehouseDocument DROP CONSTRAINT CK_Warehouse_WarehouseDocument_Status;

            SET @sql = N'ALTER TABLE ' + @entityQuoted + N' DROP CONSTRAINT ' + QUOTENAME(@foreignKey) + N';
                ALTER TABLE ' + @referenceQuoted + N' ADD Id int IDENTITY(1,1) NOT NULL;
                ALTER TABLE ' + @referenceQuoted + N' DROP CONSTRAINT ' + QUOTENAME(@primaryKey) + N';
                ALTER TABLE ' + @referenceQuoted + N' ADD CONSTRAINT ' + QUOTENAME(@primaryKey) + N' PRIMARY KEY (Id);
                ALTER TABLE ' + @referenceQuoted + N' ADD CONSTRAINT ' + QUOTENAME(N'UQ_' + PARSENAME(@reference, 1) + N'_Code') + N' UNIQUE (Code);
                ALTER TABLE ' + @entityQuoted + N' ADD IdStatus int NULL;';
            EXEC sys.sp_executesql @sql;

            SET @sql = N'UPDATE e SET IdStatus = s.Id FROM ' + @entityQuoted + N' e JOIN ' + @referenceQuoted + N' s ON s.Code = e.Status;
                IF EXISTS (SELECT 1 FROM ' + @entityQuoted + N' WHERE IdStatus IS NULL)
                    THROW 51684, ''A stored status has no reference.'', 1;
                ALTER TABLE ' + @entityQuoted + N' ALTER COLUMN IdStatus int NOT NULL;';
            EXEC sys.sp_executesql @sql;

            SET @default = NULL;
            SELECT @default = d.name FROM sys.default_constraints d JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
            WHERE d.parent_object_id = OBJECT_ID(@entity) AND c.name = N'Status';
            IF @default IS NOT NULL
            BEGIN
                SET @sql = N'ALTER TABLE ' + @entityQuoted + N' DROP CONSTRAINT ' + QUOTENAME(@default) + N';';
                EXEC sys.sp_executesql @sql;
            END;
            SET @sql = N'ALTER TABLE ' + @entityQuoted + N' DROP COLUMN Status;
                ALTER TABLE ' + @entityQuoted + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@foreignKey)
                + N' FOREIGN KEY (IdStatus) REFERENCES ' + @referenceQuoted + N'(Id);';
            EXEC sys.sp_executesql @sql;
            FETCH NEXT FROM status_cursor INTO @entity, @reference, @foreignKey;
        END;
        CLOSE status_cursor;
        DEALLOCATE status_cursor;

        DECLARE @verified int;
        EXEC sys.sp_executesql N'SELECT @result = Id FROM Store.PaymentAttemptStatus WHERE Code = N''verified'';', N'@result int OUTPUT', @verified OUTPUT;
        IF @verified IS NULL THROW 51685, 'Verified status is missing.', 1;
        SET @sql = N'ALTER TABLE Store.PaymentAttempt WITH CHECK ADD CONSTRAINT CK_Store_PaymentAttempt_Verification CHECK
            (IdStatus <> ' + CONVERT(nvarchar(11), @verified) + N' OR (GatewayReference IS NOT NULL AND ReceiptReference IS NOT NULL AND PaidAtUtc IS NOT NULL));
            CREATE UNIQUE INDEX UX_Store_PaymentAttempt_VerifiedOrder ON Store.PaymentAttempt(IdOrder) WHERE IdStatus = ' + CONVERT(nvarchar(11), @verified) + N';';
        EXEC sys.sp_executesql @sql;
        EXEC sys.sp_executesql N'CREATE INDEX IX_Store_ContactFormMessage_IdStatus_CreatedAtUtc ON Store.ContactFormMessage(IdStatus, CreatedAtUtc);';

        DECLARE @procedure nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'Warehouse.RebuildDocumentStock', N'P'));
        IF @procedure IS NULL OR CHARINDEX(N'@Status = d.[Status]', @procedure) = 0
            THROW 51686, 'Stock procedure has an unexpected definition.', 1;
        SET @procedure = REPLACE(@procedure, N'CREATE PROCEDURE', N'ALTER PROCEDURE');
        SET @procedure = REPLACE(@procedure, N'CREATE   PROCEDURE', N'ALTER PROCEDURE');
        SET @procedure = REPLACE(@procedure, N'@Status = d.[Status]', N'@Status = (SELECT s.Code FROM Warehouse.DocumentStatus AS s WHERE s.Id = d.IdStatus)');
        IF CHARINDEX(N'ALTER PROCEDURE', UPPER(@procedure)) = 0
            THROW 51687, 'Cannot update stock procedure safely.', 1;
        EXEC sys.sp_executesql @procedure;

        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'168-status-identity-keys');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
