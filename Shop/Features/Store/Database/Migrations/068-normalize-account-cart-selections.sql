SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'068-normalize-account-cart-selections')
    BEGIN
        IF OBJECT_ID(N'[Account].[CartItemPurchaseOptionValue]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Account].[CartItemPurchaseOptionValue]
            (
                [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Account_CartItemPurchaseOptionValue] PRIMARY KEY,
                [IdCartItem] BIGINT NOT NULL,
                [IdProductPurchaseOptionValue] BIGINT NOT NULL,
                CONSTRAINT [FK_Account_CartItemPurchaseOptionValue_CartItem] FOREIGN KEY ([IdCartItem]) REFERENCES [Account].[CartItem]([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_Account_CartItemPurchaseOptionValue_Value] FOREIGN KEY ([IdProductPurchaseOptionValue]) REFERENCES [Store].[ProductPurchaseOptionValue]([Id])
            );
            CREATE UNIQUE INDEX [UX_Account_CartItemPurchaseOptionValue] ON [Account].[CartItemPurchaseOptionValue] ([IdCartItem], [IdProductPurchaseOptionValue]);
        END;

        IF OBJECT_ID(N'[Account].[CartItemAccessory]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Account].[CartItemAccessory]
            (
                [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Account_CartItemAccessory] PRIMARY KEY,
                [IdCartItem] BIGINT NOT NULL,
                [IdProductAccessory] BIGINT NOT NULL,
                CONSTRAINT [FK_Account_CartItemAccessory_CartItem] FOREIGN KEY ([IdCartItem]) REFERENCES [Account].[CartItem]([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_Account_CartItemAccessory_ProductAccessory] FOREIGN KEY ([IdProductAccessory]) REFERENCES [Store].[ProductAccessory]([Id])
            );
            CREATE UNIQUE INDEX [UX_Account_CartItemAccessory] ON [Account].[CartItemAccessory] ([IdCartItem], [IdProductAccessory]);
        END;

        IF COL_LENGTH(N'Account.CartItem', N'OptionsJson') IS NOT NULL
        BEGIN
            INSERT INTO [Account].[CartItemPurchaseOptionValue] ([IdCartItem], [IdProductPurchaseOptionValue])
            SELECT item.[Id], value.[Id]
            FROM [Account].[CartItem] item
            CROSS APPLY OPENJSON(item.[OptionsJson], '$.priceOptions')
                WITH ([OptionKey] NVARCHAR(50) '$.key', [ValueKey] NVARCHAR(80) '$.productValue') selected
            JOIN [Store].[ProductPurchaseOption] option ON option.[IdProduct] = item.[IdProduct] AND option.[OptionKey] = selected.[OptionKey]
            JOIN [Store].[ProductPurchaseOptionValue] value ON value.[IdProductPurchaseOption] = option.[Id] AND value.[ValueKey] = selected.[ValueKey]
            WHERE ISJSON(item.[OptionsJson]) = 1
              AND NOT EXISTS (SELECT 1 FROM [Account].[CartItemPurchaseOptionValue] saved WHERE saved.[IdCartItem] = item.[Id] AND saved.[IdProductPurchaseOptionValue] = value.[Id]);

            INSERT INTO [Account].[CartItemAccessory] ([IdCartItem], [IdProductAccessory])
            SELECT item.[Id], link.[Id]
            FROM [Account].[CartItem] item
            CROSS APPLY OPENJSON(item.[OptionsJson], '$.accessories')
                WITH ([AccessoryKey] NVARCHAR(150) '$.id', [Selected] BIT '$.selected') selected
            JOIN [Store].[Accessory] accessory ON accessory.[AccessoryKey] = selected.[AccessoryKey]
            JOIN [Store].[ProductAccessory] link ON link.[IdProduct] = item.[IdProduct] AND link.[IdAccessory] = accessory.[Id]
            WHERE ISJSON(item.[OptionsJson]) = 1 AND ISNULL(selected.[Selected], 1) = 1
              AND NOT EXISTS (SELECT 1 FROM [Account].[CartItemAccessory] saved WHERE saved.[IdCartItem] = item.[Id] AND saved.[IdProductAccessory] = link.[Id]);

            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Account_CartItem_Options')
                ALTER TABLE [Account].[CartItem] DROP CONSTRAINT [CK_Account_CartItem_Options];
            ALTER TABLE [Account].[CartItem] DROP COLUMN [OptionsJson];
        END;

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'068-normalize-account-cart-selections');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
