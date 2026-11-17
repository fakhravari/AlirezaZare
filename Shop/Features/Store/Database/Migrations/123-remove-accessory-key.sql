SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'123-remove-accessory-key')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'122-accessory-display-discount')
            THROW 51300, 'Migration 122 must be applied first.', 1;
        IF COL_LENGTH(N'Store.Accessory', N'AccessoryKey') IS NULL
            THROW 51301, 'AccessoryKey is missing without its migration marker.', 1;
        EXEC sp_executesql N'
            IF EXISTS (SELECT 1 FROM Store.OrderItem WHERE OptionsJson IS NOT NULL AND ISJSON(OptionsJson) <> 1)
                THROW 51302, ''Invalid order options JSON; migration stopped.'', 1;
            IF EXISTS (
                SELECT 1 FROM Store.OrderItem AS item
                CROSS APPLY OPENJSON(CASE WHEN ISJSON(item.OptionsJson) = 1 THEN item.OptionsJson ELSE N''{}'' END, N''$.accessories'') AS entry
                OUTER APPLY (SELECT JSON_VALUE(CASE WHEN entry.type = 5 THEN entry.value ELSE N''{}'' END, N''$.id'') AS LegacyId) AS choice
                LEFT JOIN Store.Accessory AS accessory ON accessory.AccessoryKey = choice.LegacyId
                WHERE entry.type <> 5 OR choice.LegacyId IS NULL OR accessory.Id IS NULL
            )
                THROW 51303, ''An order accessory cannot be mapped; migration stopped.'', 1;
            DECLARE @itemId bigint, @arrayIndex int, @accessoryId bigint, @path nvarchar(200);
            DECLARE order_accessories CURSOR LOCAL FAST_FORWARD FOR
                SELECT item.Id, CONVERT(int, entry.[key]), accessory.Id
                FROM Store.OrderItem AS item
                CROSS APPLY OPENJSON(CASE WHEN ISJSON(item.OptionsJson) = 1 THEN item.OptionsJson ELSE N''{}'' END, N''$.accessories'') AS entry
                JOIN Store.Accessory AS accessory ON accessory.AccessoryKey = JSON_VALUE(entry.value, N''$.id'');
            OPEN order_accessories;
            FETCH NEXT FROM order_accessories INTO @itemId, @arrayIndex, @accessoryId;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @path = N''$.accessories['' + CONVERT(nvarchar(20), @arrayIndex) + N''].id'';
                UPDATE Store.OrderItem SET OptionsJson = JSON_MODIFY(OptionsJson, @path, CONVERT(nvarchar(20), @accessoryId)) WHERE Id = @itemId;
                FETCH NEXT FROM order_accessories INTO @itemId, @arrayIndex, @accessoryId;
            END;
            CLOSE order_accessories;
            DEALLOCATE order_accessories;
            IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N''Store.Accessory'') AND name = N''UQ_Store_Accessory_AccessoryKey'')
                THROW 51304, ''Expected accessory key constraint is missing.'', 1;
            ALTER TABLE Store.Accessory DROP CONSTRAINT UQ_Store_Accessory_AccessoryKey;
            ALTER TABLE Store.Accessory DROP COLUMN AccessoryKey;
        ';
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'123-remove-accessory-key');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
