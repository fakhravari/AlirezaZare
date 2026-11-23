SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51570, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'157-order-number-pattern')
    BEGIN
        IF OBJECT_ID(N'Store.OrderPreviousNumber', N'U') IS NOT NULL
            THROW 51571, 'Store.OrderPreviousNumber already exists; inspect before migration.', 1;

        -- A previous number must not refer to a different order's new number.
        IF EXISTS (
            SELECT 1 FROM Store.[Order] AS oldOrder
            JOIN Store.[Order] AS target ON oldOrder.Id <> target.Id
                AND oldOrder.OrderNumber = N'AZ-' + REPLICATE(N'0', CASE WHEN LEN(CONVERT(varchar(20), target.Id)) < 10 THEN 10 - LEN(CONVERT(varchar(20), target.Id)) ELSE 0 END) + CONVERT(varchar(20), target.Id)
        )
            THROW 51572, 'An existing order number conflicts with another order''s new number.', 1;

        CREATE TABLE Store.OrderPreviousNumber (
            IdOrder bigint NOT NULL CONSTRAINT PK_OrderPreviousNumber PRIMARY KEY,
            PreviousNumber nvarchar(32) NOT NULL,
            CONSTRAINT FK_OrderPreviousNumber_Order FOREIGN KEY (IdOrder) REFERENCES Store.[Order](Id) ON DELETE CASCADE
        );
        CREATE INDEX IX_OrderPreviousNumber_PreviousNumber ON Store.OrderPreviousNumber(PreviousNumber);

        INSERT Store.OrderPreviousNumber (IdOrder, PreviousNumber)
        SELECT Id, OrderNumber FROM Store.[Order]
        WHERE OrderNumber <> N'AZ-' + REPLICATE(N'0', CASE WHEN LEN(CONVERT(varchar(20), Id)) < 10 THEN 10 - LEN(CONVERT(varchar(20), Id)) ELSE 0 END) + CONVERT(varchar(20), Id);

        UPDATE Store.[Order]
        SET OrderNumber = N'AZ-' + REPLICATE(N'0', CASE WHEN LEN(CONVERT(varchar(20), Id)) < 10 THEN 10 - LEN(CONVERT(varchar(20), Id)) ELSE 0 END) + CONVERT(varchar(20), Id)
        WHERE OrderNumber <> N'AZ-' + REPLICATE(N'0', CASE WHEN LEN(CONVERT(varchar(20), Id)) < 10 THEN 10 - LEN(CONVERT(varchar(20), Id)) ELSE 0 END) + CONVERT(varchar(20), Id);

        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'157-order-number-pattern');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
