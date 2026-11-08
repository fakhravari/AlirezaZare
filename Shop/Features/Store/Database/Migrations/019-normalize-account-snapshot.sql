SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'019-normalize-account-snapshot')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Account].[CustomerOrder]') AND name = N'ExternalOrderKey')
        ALTER TABLE [Account].[CustomerOrder] ADD [ExternalOrderKey] NVARCHAR(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Account_CustomerOrder_ExternalOrderKey' AND object_id = OBJECT_ID(N'[Account].[CustomerOrder]'))
        EXEC(N'CREATE UNIQUE INDEX [UX_Account_CustomerOrder_ExternalOrderKey] ON [Account].[CustomerOrder] ([ExternalOrderKey]) WHERE [ExternalOrderKey] IS NOT NULL;');

    IF OBJECT_ID(N'[Account].[Coupon]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Account].[Coupon]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL,
            [CouponKey] NVARCHAR(100) NOT NULL,
            [Code] NVARCHAR(32) NOT NULL,
            [DiscountPercent] INT NOT NULL,
            [UsageLimit] INT NOT NULL,
            [Enabled] BIT NOT NULL,
            [ExpiresOn] DATE NOT NULL,
            CONSTRAINT [PK_Account_Coupon] PRIMARY KEY ([Id]),
            CONSTRAINT [UQ_Account_Coupon_CouponKey] UNIQUE ([CouponKey]),
            CONSTRAINT [UQ_Account_Coupon_Code] UNIQUE ([Code]),
            CONSTRAINT [CK_Account_Coupon_Discount] CHECK ([DiscountPercent] BETWEEN 1 AND 99),
            CONSTRAINT [CK_Account_Coupon_UsageLimit] CHECK ([UsageLimit] > 0)
        );
    END;

    IF OBJECT_ID(N'[Account].[CouponUsage]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Account].[CouponUsage]
        (
            [IdCoupon] BIGINT NOT NULL,
            [IdCustomerOrder] BIGINT NOT NULL,
            CONSTRAINT [PK_Account_CouponUsage] PRIMARY KEY ([IdCoupon], [IdCustomerOrder]),
            CONSTRAINT [FK_Account_CouponUsage_Coupon] FOREIGN KEY ([IdCoupon]) REFERENCES [Account].[Coupon] ([Id]),
            CONSTRAINT [FK_Account_CouponUsage_Order] FOREIGN KEY ([IdCustomerOrder]) REFERENCES [Account].[CustomerOrder] ([Id]) ON DELETE CASCADE
        );
    END;

    IF OBJECT_ID(N'[Account].[ProfileSnapshot]', N'U') IS NOT NULL
    BEGIN
        DECLARE @ProfileId BIGINT;
        SELECT @ProfileId = [Id] FROM [Account].[Profile] WHERE [Email] = N'demo@example.test';
        IF @ProfileId IS NULL
        BEGIN
            INSERT INTO [Account].[Profile] ([Email], [FirstName], [LastName])
            VALUES (N'demo@example.test', N'کاربر', N'نمونه');
            SET @ProfileId = CONVERT(BIGINT, SCOPE_IDENTITY());
        END;

        EXEC sys.sp_executesql N'
            INSERT INTO [Account].[CustomerOrder]
                ([IdProfile], [OrderNumber], [ExternalOrderKey], [Status], [PaymentMethod], [ItemCount], [TotalAmount], [OrderedAtUtc], [AddressSnapshotJson])
            SELECT @ProfileId, source.[number], source.[id], source.[status], N''demo'', source.[itemCount], source.[totalAmount],
                   CONVERT(DATETIME2(3), source.[orderedOn]), N''{}''
            FROM [Account].[ProfileSnapshot] snapshot
            CROSS APPLY OPENJSON(snapshot.[PayloadJson], ''$.orders'') WITH
            (
                [id] NVARCHAR(100) ''$.id'',
                [number] NVARCHAR(32) ''$.number'',
                [status] NVARCHAR(30) ''$.status'',
                [orderedOn] DATE ''$.orderedOn'',
                [itemCount] INT ''$.itemCount'',
                [totalAmount] DECIMAL(19,0) ''$.totalAmount''
            ) source
            WHERE NOT EXISTS (SELECT 1 FROM [Account].[CustomerOrder] existing WHERE existing.[ExternalOrderKey] = source.[id]);',
            N'@ProfileId BIGINT', @ProfileId;

        INSERT INTO [Account].[Coupon] ([CouponKey], [Code], [DiscountPercent], [UsageLimit], [Enabled], [ExpiresOn])
        SELECT source.[id], source.[code], source.[discountPercent], source.[usageLimit], source.[enabled], source.[expiresOn]
        FROM [Account].[ProfileSnapshot] snapshot
        CROSS APPLY OPENJSON(snapshot.[PayloadJson], '$.coupons') WITH
        (
            [id] NVARCHAR(100) '$.id',
            [code] NVARCHAR(32) '$.code',
            [discountPercent] INT '$.discountPercent',
            [usageLimit] INT '$.usageLimit',
            [enabled] BIT '$.enabled',
            [expiresOn] DATE '$.expiresOn'
        ) source
        WHERE NOT EXISTS (SELECT 1 FROM [Account].[Coupon] existing WHERE existing.[CouponKey] = source.[id]);

        EXEC(N'
            INSERT INTO [Account].[CouponUsage] ([IdCoupon], [IdCustomerOrder])
            SELECT coupon.[Id], customerOrder.[Id]
            FROM [Account].[ProfileSnapshot] snapshot
            CROSS APPLY OPENJSON(snapshot.[PayloadJson], ''$.coupons'') WITH
            (
                [id] NVARCHAR(100) ''$.id'',
                [orderIds] NVARCHAR(MAX) ''$.orderIds'' AS JSON
            ) source
            INNER JOIN [Account].[Coupon] coupon ON coupon.[CouponKey] = source.[id]
            CROSS APPLY OPENJSON(source.[orderIds]) orderKey
            INNER JOIN [Account].[CustomerOrder] customerOrder ON customerOrder.[ExternalOrderKey] = CONVERT(NVARCHAR(100), orderKey.[value])
            WHERE NOT EXISTS
            (
                SELECT 1 FROM [Account].[CouponUsage] existing
                WHERE existing.[IdCoupon] = coupon.[Id] AND existing.[IdCustomerOrder] = customerOrder.[Id]
            );');

        DROP TABLE [Account].[ProfileSnapshot];
    END;

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'019-normalize-account-snapshot');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
