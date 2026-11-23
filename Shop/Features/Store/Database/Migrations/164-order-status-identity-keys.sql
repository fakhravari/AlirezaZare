SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51620, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'164-order-status-identity-keys')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'159-status-reference-tables')
            OR NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'161-payment-attempt-numeric-identity')
            OR NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'162-remove-empty-order-status-archive')
            OR NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'163-contact-message-status')
            THROW 51621, 'Migrations 159, 161, 162 and 163 are required first.', 1;
        IF OBJECT_ID(N'Store.OrderStatus', N'U') IS NULL
            OR OBJECT_ID(N'Store.Order', N'U') IS NULL
            OR OBJECT_ID(N'Store.OrderShippingHistory', N'U') IS NULL
            THROW 51622, 'Order status tables are incomplete.', 1;
        IF COL_LENGTH(N'Store.OrderStatus', N'Id') IS NOT NULL
            OR COL_LENGTH(N'Store.Order', N'IdStatus') IS NOT NULL
            OR COL_LENGTH(N'Store.OrderShippingHistory', N'IdPreviousStatus') IS NOT NULL
            THROW 51623, 'Unexpected order status identity schema.', 1;
        IF EXISTS
        (
            SELECT 1
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id IN (OBJECT_ID(N'Store.Order'), OBJECT_ID(N'Store.OrderShippingHistory'))
              AND i.is_primary_key = 0
              AND ((i.object_id = OBJECT_ID(N'Store.Order') AND c.name = N'Status')
                OR (i.object_id = OBJECT_ID(N'Store.OrderShippingHistory') AND c.name IN (N'PreviousStatus', N'NewStatus')))
        )
            THROW 51624, 'An index uses legacy order status columns; inspect before migration.', 1;

        ALTER TABLE Store.[Order] DROP CONSTRAINT FK_Order_OrderStatus;
        ALTER TABLE Store.OrderShippingHistory DROP CONSTRAINT FK_OrderShippingHistory_PreviousStatus;
        ALTER TABLE Store.OrderShippingHistory DROP CONSTRAINT FK_OrderShippingHistory_NewStatus;

        DECLARE @StatusPrimaryKey sysname;
        SELECT @StatusPrimaryKey = name
        FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'Store.OrderStatus') AND type = N'PK';
        IF @StatusPrimaryKey IS NULL
            THROW 51625, 'Store.OrderStatus primary key is missing.', 1;

        ALTER TABLE Store.OrderStatus ADD Id int IDENTITY(1,1) NOT NULL;
        DECLARE @DropStatusPrimaryKey nvarchar(500) = N'ALTER TABLE Store.OrderStatus DROP CONSTRAINT ' + QUOTENAME(@StatusPrimaryKey) + N';';
        EXEC sys.sp_executesql @DropStatusPrimaryKey;
        EXEC sys.sp_executesql N'ALTER TABLE Store.OrderStatus ADD CONSTRAINT PK_OrderStatus PRIMARY KEY (Id);';
        ALTER TABLE Store.OrderStatus ADD CONSTRAINT UQ_OrderStatus_Code UNIQUE (Code);

        ALTER TABLE Store.[Order] ADD IdStatus int NULL;
        ALTER TABLE Store.OrderShippingHistory ADD IdPreviousStatus int NULL, IdNewStatus int NULL;

        EXEC sys.sp_executesql N'UPDATE o SET IdStatus = s.Id
        FROM Store.[Order] o JOIN Store.OrderStatus s ON s.Code = o.Status;
        UPDATE h SET IdPreviousStatus = previousStatus.Id, IdNewStatus = newStatus.Id
        FROM Store.OrderShippingHistory h
        JOIN Store.OrderStatus previousStatus ON previousStatus.Code = h.PreviousStatus
        JOIN Store.OrderStatus newStatus ON newStatus.Code = h.NewStatus;
        IF EXISTS (SELECT 1 FROM Store.[Order] WHERE IdStatus IS NULL)
            OR EXISTS (SELECT 1 FROM Store.OrderShippingHistory WHERE IdPreviousStatus IS NULL OR IdNewStatus IS NULL)
            THROW 51626, ''A stored order status has no matching reference row.'', 1;';

        EXEC sys.sp_executesql N'ALTER TABLE Store.[Order] ALTER COLUMN IdStatus int NOT NULL;
        ALTER TABLE Store.OrderShippingHistory ALTER COLUMN IdPreviousStatus int NOT NULL;
        ALTER TABLE Store.OrderShippingHistory ALTER COLUMN IdNewStatus int NOT NULL;';

        -- Migration 158 adds this constraint to the legacy text column. The checked
        -- foreign key and the status reference table enforce the same rule after conversion.
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'Store.[Order]') AND name = N'CK_Order_NoSimulatedPaymentStatus')
            ALTER TABLE Store.[Order] DROP CONSTRAINT CK_Order_NoSimulatedPaymentStatus;
        DECLARE @StatusDefault sysname;
        SELECT @StatusDefault = d.name
        FROM sys.default_constraints d JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID(N'Store.[Order]') AND c.name = N'Status';
        IF @StatusDefault IS NOT NULL
        BEGIN
            DECLARE @DropStatusDefault nvarchar(500) = N'ALTER TABLE Store.[Order] DROP CONSTRAINT ' + QUOTENAME(@StatusDefault) + N';';
            EXEC sys.sp_executesql @DropStatusDefault;
        END;
        ALTER TABLE Store.[Order] DROP COLUMN Status;
        ALTER TABLE Store.OrderShippingHistory DROP COLUMN PreviousStatus, NewStatus;

        EXEC sys.sp_executesql N'ALTER TABLE Store.[Order] WITH CHECK ADD CONSTRAINT FK_Order_OrderStatus FOREIGN KEY (IdStatus) REFERENCES Store.OrderStatus(Id);
        ALTER TABLE Store.OrderShippingHistory WITH CHECK ADD CONSTRAINT FK_OrderShippingHistory_PreviousStatus FOREIGN KEY (IdPreviousStatus) REFERENCES Store.OrderStatus(Id);
        ALTER TABLE Store.OrderShippingHistory WITH CHECK ADD CONSTRAINT FK_OrderShippingHistory_NewStatus FOREIGN KEY (IdNewStatus) REFERENCES Store.OrderStatus(Id);';

        INSERT Store.SchemaMigration (MigrationKey)
        VALUES (N'164-order-status-identity-keys');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
