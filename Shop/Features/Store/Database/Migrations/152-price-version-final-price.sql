SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51520, 'Store.SchemaMigration is required before migration 152.', 1;

    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'152-price-version-final-price')
    BEGIN
        IF OBJECT_ID(N'Store.PriceVersion', N'U') IS NULL
            THROW 51521, 'Store.PriceVersion is required before migration 152.', 1;

        IF COL_LENGTH(N'Store.PriceVersion', N'FinalPriceAmount') IS NULL
        BEGIN
            ALTER TABLE Store.PriceVersion ADD FinalPriceAmount decimal(19,0) NULL;
            EXEC(N'UPDATE Store.PriceVersion
                SET FinalPriceAmount = CONVERT(decimal(19,0), ROUND(PriceAmount * (100 - DiscountPercent) / 100, 0));
                ALTER TABLE Store.PriceVersion ALTER COLUMN FinalPriceAmount decimal(19,0) NOT NULL;');
        END
        ELSE IF EXISTS (SELECT 1 FROM sys.computed_columns WHERE object_id=OBJECT_ID(N'Store.PriceVersion') AND name=N'FinalPriceAmount')
            THROW 51522, 'FinalPriceAmount exists as a computed column; review the schema before migration 152.', 1;

        IF OBJECT_ID(N'Store.OrderItemAmount', N'V') IS NOT NULL
            EXEC(N'ALTER VIEW Store.OrderItemAmount AS
                SELECT i.Id AS IdOrderItem,
                    CONVERT(decimal(19,0), v.FinalPriceAmount + COALESCE(a.AccessoryAmount, 0)) AS UnitAmount
                FROM Store.OrderItem i
                JOIN Store.PriceVersion v ON v.Id=i.IdPriceVersion
                OUTER APPLY (SELECT SUM(x.UnitAmountSnapshot) AS AccessoryAmount
                    FROM Store.OrderAccessory x
                    WHERE x.IdOrder=i.IdOrder AND x.IdProduct=i.IdProduct AND x.IdSalesPlan=i.IdSalesPlan) a;');

        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'152-price-version-final-price');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
