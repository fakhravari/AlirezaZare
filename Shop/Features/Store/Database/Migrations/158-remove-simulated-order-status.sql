SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51580, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'158-remove-simulated-order-status')
    BEGIN
        IF OBJECT_ID(N'Store.OrderStatusArchive158', N'U') IS NOT NULL
            THROW 51581, 'Store.OrderStatusArchive158 already exists; inspect before migration.', 1;
        IF OBJECT_ID(N'Store.PaymentAttempt', N'U') IS NULL
            THROW 51582, 'Store.PaymentAttempt is required.', 1;

        -- An already dispatched order without a verified payment needs individual review.
        IF EXISTS (
            SELECT 1 FROM Store.[Order] AS o
            WHERE o.Status = N'simulated-payment-confirmed' AND o.IdDocument IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM Store.PaymentAttempt AS p
                              WHERE p.IdOrder = o.Id AND p.Status = N'verified'
                                AND p.Amount = o.TotalAmount AND p.PaidAtUtc IS NOT NULL)
        )
            THROW 51583, 'A simulated order has a warehouse document without verified payment; review it first.', 1;

        CREATE TABLE Store.OrderStatusArchive158 (
            IdOrder bigint NOT NULL CONSTRAINT PK_OrderStatusArchive158 PRIMARY KEY,
            PreviousStatus nvarchar(30) NOT NULL,
            NewStatus nvarchar(30) NOT NULL,
            ChangedAtUtc datetime2(3) NOT NULL
        );

        INSERT Store.OrderStatusArchive158 (IdOrder, PreviousStatus, NewStatus, ChangedAtUtc)
        SELECT o.Id, o.Status,
               CASE WHEN EXISTS (SELECT 1 FROM Store.PaymentAttempt AS p
                                 WHERE p.IdOrder = o.Id AND p.Status = N'verified'
                                   AND p.Amount = o.TotalAmount AND p.PaidAtUtc IS NOT NULL)
                    THEN N'payment-confirmed' ELSE N'pending-payment' END,
               SYSUTCDATETIME()
        FROM Store.[Order] AS o
        WHERE o.Status = N'simulated-payment-confirmed';

        UPDATE o SET Status = archive.NewStatus
        FROM Store.[Order] AS o
        JOIN Store.OrderStatusArchive158 AS archive ON archive.IdOrder = o.Id;

        ALTER TABLE Store.[Order] WITH CHECK ADD CONSTRAINT CK_Order_NoSimulatedPaymentStatus
            CHECK (Status <> N'simulated-payment-confirmed');
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'158-remove-simulated-order-status');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
