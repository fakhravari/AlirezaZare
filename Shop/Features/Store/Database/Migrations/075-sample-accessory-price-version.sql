SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'075-sample-accessory-price-version')
    BEGIN
        DECLARE @IdProduct BIGINT;
        DECLARE @IdAccessory BIGINT;
        DECLARE @IdPlan BIGINT;
        DECLARE @PriceAmount DECIMAL(19,0);

        SELECT TOP (1)
            @IdProduct = pa.[IdProduct],
            @IdAccessory = pa.[IdAccessory],
            @PriceAmount = pa.[PriceAmount]
        FROM [Store].[ProductAccessory] pa
        INNER JOIN [Store].[Accessory] accessory ON accessory.[Id] = pa.[IdAccessory]
        WHERE accessory.[AccessoryKey] = N'sample-status-active';

        SELECT TOP (1) @IdPlan = [Id]
        FROM [Warehouse].[SalesPlanReference]
        WHERE [IdStockOwnerType] = 1 AND [IsActive] = 1
        ORDER BY [IsRegular] DESC, [Id];

        IF @IdProduct IS NOT NULL AND @IdPlan IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM [Store].[PriceVersion]
               WHERE [ScopeKey] = CONCAT(N'accessory/',@IdProduct,N'/',@IdAccessory,N'/plan/',@IdPlan)
                 AND [IsActive] = 1)
            INSERT [Store].[PriceVersion]
                ([ScopeKey],[IdProduct],[IdSalesPlan],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
            VALUES
                (CONCAT(N'accessory/',@IdProduct,N'/',@IdAccessory,N'/plan/',@IdPlan),@IdProduct,@IdPlan,N'accessory',@IdAccessory,@PriceAmount,0,1,N'Sample active accessory price');

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'075-sample-accessory-price-version');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
