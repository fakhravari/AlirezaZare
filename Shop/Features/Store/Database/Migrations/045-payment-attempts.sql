SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'045-payment-attempts')
    BEGIN
        CREATE TABLE [Store].[PaymentAttempt]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Store_PaymentAttempt] PRIMARY KEY,
            [IdOrder] BIGINT NOT NULL,
            [Amount] DECIMAL(19,0) NOT NULL,
            [Status] NVARCHAR(20) NOT NULL,
            [GatewayReference] NVARCHAR(200) NULL,
            [ReceiptReference] NVARCHAR(200) NULL,
            [PaidAtUtc] DATETIME2(3) NULL,
            [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_PaymentAttempt_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
            [UpdatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_PaymentAttempt_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [FK_Store_PaymentAttempt_Order] FOREIGN KEY ([IdOrder]) REFERENCES [Store].[Order] ([Id]),
            CONSTRAINT [CK_Store_PaymentAttempt_Amount] CHECK ([Amount] > 0),
            CONSTRAINT [CK_Store_PaymentAttempt_Status] CHECK ([Status] IN (N'initiated', N'awaiting', N'verified', N'failed', N'unavailable')),
            CONSTRAINT [CK_Store_PaymentAttempt_Verification] CHECK ([Status] <> N'verified' OR ([GatewayReference] IS NOT NULL AND [ReceiptReference] IS NOT NULL AND [PaidAtUtc] IS NOT NULL))
        );
        CREATE INDEX [IX_Store_PaymentAttempt_Order] ON [Store].[PaymentAttempt] ([IdOrder], [CreatedAtUtc]);
        CREATE UNIQUE INDEX [UX_Store_PaymentAttempt_GatewayReference] ON [Store].[PaymentAttempt] ([GatewayReference]) WHERE [GatewayReference] IS NOT NULL;
        CREATE UNIQUE INDEX [UX_Store_PaymentAttempt_ReceiptReference] ON [Store].[PaymentAttempt] ([ReceiptReference]) WHERE [ReceiptReference] IS NOT NULL;
        CREATE UNIQUE INDEX [UX_Store_PaymentAttempt_VerifiedOrder] ON [Store].[PaymentAttempt] ([IdOrder]) WHERE [Status] = N'verified';
        CREATE TABLE [Store].[PaidOrderOutbox]
        (
            [IdOrder] BIGINT NOT NULL CONSTRAINT [PK_Store_PaidOrderOutbox] PRIMARY KEY,
            [IdPaymentAttempt] UNIQUEIDENTIFIER NOT NULL,
            [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_PaidOrderOutbox_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
            [ProcessedAtUtc] DATETIME2(3) NULL,
            CONSTRAINT [FK_Store_PaidOrderOutbox_Order] FOREIGN KEY ([IdOrder]) REFERENCES [Store].[Order] ([Id]),
            CONSTRAINT [FK_Store_PaidOrderOutbox_Payment] FOREIGN KEY ([IdPaymentAttempt]) REFERENCES [Store].[PaymentAttempt] ([Id])
        );
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'045-payment-attempts');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
