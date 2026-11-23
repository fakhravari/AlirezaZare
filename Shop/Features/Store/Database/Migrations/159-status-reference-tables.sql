SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51590, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey=N'159-status-reference-tables')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey=N'156-product-feedback-status')
            OR NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey=N'158-remove-simulated-order-status')
            THROW 51591, 'Migrations 156 and 158 are required.', 1;
        IF OBJECT_ID(N'Store.OrderStatus', N'U') IS NOT NULL OR OBJECT_ID(N'Store.PaymentAttemptStatus', N'U') IS NOT NULL
            OR OBJECT_ID(N'Warehouse.DocumentStatus', N'U') IS NOT NULL OR COL_LENGTH(N'Store.ProductFeedbackStatus', N'NameEn') IS NOT NULL
            THROW 51592, 'Unexpected status reference schema.', 1;

        CREATE TABLE Store.OrderStatus (Code nvarchar(30) NOT NULL CONSTRAINT PK_OrderStatus PRIMARY KEY, NameFa nvarchar(80) NOT NULL, NameEn nvarchar(80) NOT NULL);
        INSERT Store.OrderStatus (Code, NameFa, NameEn) VALUES
            (N'pending-payment', N'در انتظار پرداخت', N'Pending payment'),
            (N'payment-confirmed', N'پرداخت تأییدشده', N'Payment confirmed'),
            (N'warehouse-confirmed', N'تأیید انبار', N'Warehouse confirmed'),
            (N'in-transit', N'در حال ارسال', N'In transit'),
            (N'shipped-to-post', N'ارسال به پست', N'Shipped to post'),
            (N'delivered', N'تحویل‌شده', N'Delivered'),
            (N'cancelled', N'لغوشده', N'Cancelled');

        CREATE TABLE Store.PaymentAttemptStatus (Code nvarchar(20) NOT NULL CONSTRAINT PK_PaymentAttemptStatus PRIMARY KEY, NameFa nvarchar(80) NOT NULL, NameEn nvarchar(80) NOT NULL);
        INSERT Store.PaymentAttemptStatus (Code, NameFa, NameEn) VALUES
            (N'initiated', N'آغازشده', N'Initiated'), (N'awaiting', N'در انتظار پاسخ', N'Awaiting response'),
            (N'unavailable', N'درگاه در دسترس نیست', N'Gateway unavailable'), (N'failed', N'ناموفق', N'Failed'),
            (N'verified', N'تأییدشده', N'Verified');

        CREATE TABLE Warehouse.DocumentStatus (Code char(1) NOT NULL CONSTRAINT PK_DocumentStatus PRIMARY KEY, NameFa nvarchar(80) NOT NULL, NameEn nvarchar(80) NOT NULL);
        INSERT Warehouse.DocumentStatus (Code, NameFa, NameEn) VALUES ('D', N'موقت', N'Draft'), ('F', N'قطعی', N'Final');

        ALTER TABLE Store.ProductFeedbackStatus ADD NameEn nvarchar(80) NULL;
        EXEC sys.sp_executesql N'UPDATE Store.ProductFeedbackStatus SET NameEn=CASE Id WHEN 1 THEN N''Pending review'' WHEN 2 THEN N''Approved'' WHEN 3 THEN N''Rejected'' END;';
        EXEC sys.sp_executesql N'IF EXISTS (SELECT 1 FROM Store.ProductFeedbackStatus WHERE NameEn IS NULL) THROW 51593, ''Unknown product feedback status.'', 1;';
        EXEC sys.sp_executesql N'ALTER TABLE Store.ProductFeedbackStatus ALTER COLUMN NameEn nvarchar(80) NOT NULL;';

        -- WITH CHECK validates every existing value; unknown states roll back the entire migration.
        ALTER TABLE Store.[Order] WITH CHECK ADD CONSTRAINT FK_Order_OrderStatus FOREIGN KEY (Status) REFERENCES Store.OrderStatus(Code);
        ALTER TABLE Store.OrderShippingHistory WITH CHECK ADD CONSTRAINT FK_OrderShippingHistory_PreviousStatus FOREIGN KEY (PreviousStatus) REFERENCES Store.OrderStatus(Code);
        ALTER TABLE Store.OrderShippingHistory WITH CHECK ADD CONSTRAINT FK_OrderShippingHistory_NewStatus FOREIGN KEY (NewStatus) REFERENCES Store.OrderStatus(Code);
        ALTER TABLE Store.PaymentAttempt WITH CHECK ADD CONSTRAINT FK_PaymentAttempt_Status FOREIGN KEY (Status) REFERENCES Store.PaymentAttemptStatus(Code);
        ALTER TABLE Warehouse.WarehouseDocument WITH CHECK ADD CONSTRAINT FK_WarehouseDocument_Status FOREIGN KEY (Status) REFERENCES Warehouse.DocumentStatus(Code);

        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'159-status-reference-tables');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
