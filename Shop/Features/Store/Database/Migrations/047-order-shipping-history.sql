SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'047-order-shipping-history')
    BEGIN
        IF OBJECT_ID(N'[Store].[PaymentAttempt]', N'U') IS NULL
            THROW 51047, 'Payment migration 045 is required before order shipping history.', 1;
        CREATE TABLE [Store].[OrderShippingHistory]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_OrderShippingHistory] PRIMARY KEY,
            [IdOrder] BIGINT NOT NULL,
            [PreviousStatus] NVARCHAR(30) NOT NULL,
            [NewStatus] NVARCHAR(30) NOT NULL,
            [ActorUserKey] NVARCHAR(256) NOT NULL,
            [ChangedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_OrderShippingHistory_ChangedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [FK_Store_OrderShippingHistory_Order] FOREIGN KEY ([IdOrder]) REFERENCES [Store].[Order] ([Id])
        );
        CREATE INDEX [IX_Store_OrderShippingHistory_Order] ON [Store].[OrderShippingHistory] ([IdOrder], [Id]);
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'047-order-shipping-history');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
