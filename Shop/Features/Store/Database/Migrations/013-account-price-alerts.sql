SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF EXISTS(SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'013-account-price-alerts') BEGIN COMMIT; RETURN; END;
 CREATE TABLE [Account].[PriceAlert]([Id] BIGINT IDENTITY(1,1) NOT NULL,[IdProfile] BIGINT NOT NULL,[IdProduct] BIGINT NOT NULL,[Scope] NVARCHAR(20) NOT NULL,[VariantSignature] NVARCHAR(500) NOT NULL,[VariantsJson] NVARCHAR(MAX) NULL,[PriceAtRegistration] DECIMAL(19,0) NOT NULL,[StockAtRegistration] NVARCHAR(100) NULL,[CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Account_PriceAlert_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),CONSTRAINT [PK_Account_PriceAlert] PRIMARY KEY([Id]),CONSTRAINT [FK_Account_PriceAlert_Profile] FOREIGN KEY([IdProfile]) REFERENCES [Account].[Profile]([Id]) ON DELETE CASCADE,CONSTRAINT [FK_Account_PriceAlert_Product] FOREIGN KEY([IdProduct]) REFERENCES [Store].[Product]([Id]),CONSTRAINT [UQ_Account_PriceAlert_Configuration] UNIQUE([IdProfile],[IdProduct],[Scope],[VariantSignature]),CONSTRAINT [CK_Account_PriceAlert_Scope] CHECK([Scope] IN(N'all',N'variant')),CONSTRAINT [CK_Account_PriceAlert_Price] CHECK([PriceAtRegistration]>=0),CONSTRAINT [CK_Account_PriceAlert_Variants] CHECK([VariantsJson] IS NULL OR ISJSON([VariantsJson])=1));
 INSERT INTO [Store].[SchemaMigration]([MigrationKey]) VALUES(N'013-account-price-alerts');
 COMMIT;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
