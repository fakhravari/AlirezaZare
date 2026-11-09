SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'048-order-shipment-tracking')
    BEGIN
        IF OBJECT_ID(N'[Store].[OrderShippingHistory]', N'U') IS NULL
            THROW 51048, 'Order shipping history migration 047 is required.', 1;
        ALTER TABLE [Store].[Order] ADD [ShippingCarrier] NVARCHAR(100) NULL, [ShippingTrackingCode] NVARCHAR(100) NULL;
        CREATE TABLE [Store].[OrderShipmentTrackingHistory]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_OrderShipmentTrackingHistory] PRIMARY KEY,
            [IdOrder] BIGINT NOT NULL,
            [PreviousCarrier] NVARCHAR(100) NULL,
            [PreviousTrackingCode] NVARCHAR(100) NULL,
            [Carrier] NVARCHAR(100) NOT NULL,
            [TrackingCode] NVARCHAR(100) NOT NULL,
            [ActorUserKey] NVARCHAR(256) NOT NULL,
            [ChangedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_OrderShipmentTrackingHistory_ChangedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [FK_Store_OrderShipmentTrackingHistory_Order] FOREIGN KEY ([IdOrder]) REFERENCES [Store].[Order] ([Id])
        );
        CREATE INDEX [IX_Store_OrderShipmentTrackingHistory_Order] ON [Store].[OrderShipmentTrackingHistory] ([IdOrder], [Id]);
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'048-order-shipment-tracking');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
