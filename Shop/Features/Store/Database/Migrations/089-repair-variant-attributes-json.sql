SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'089-repair-variant-attributes-json')
 BEGIN
  UPDATE [Warehouse].[ProductVariant] SET [AttributesJson]=N'[{"key":"sampleStatus","label":"وضعیت نمونه","value":"active","display":"فعال"}]' WHERE [Code]=N'SAMPLE-STATUS-ACTIVE';
  UPDATE [Warehouse].[ProductVariant] SET [AttributesJson]=N'[{"key":"sampleStatus","label":"وضعیت نمونه","value":"inactive","display":"غیرفعال"}]' WHERE [Code]=N'SAMPLE-STATUS-INACTIVE';
  INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'089-repair-variant-attributes-json');
 END;
 COMMIT;
END TRY
BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;

