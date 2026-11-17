SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'103-unified-product-assets')
 BEGIN
  IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'102-product-media-relations')
   THROW 51000, 'Migration 102 is required first.', 1;
  IF EXISTS (SELECT 1 FROM Store.ProductMedia m JOIN Warehouse.Product p ON p.Id=m.IdProduct WHERE m.MediaKey <> 'product-' + p.ProductKey)
   THROW 51001, 'Unrecognized product media keys.', 1;
  IF EXISTS (SELECT 1 FROM Store.ProductMedia m WHERE NULLIF(m.MainPath,'') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Store.ProductImage i WHERE i.IdProductMedia=m.Id AND i.Path=m.MainPath))
   THROW 51002, 'Default image missing from gallery.', 1;
  IF EXISTS (SELECT 1 FROM Store.ProductMedia GROUP BY IdProduct HAVING COUNT(*) > 1)
   THROW 51004, 'Multiple media groups for one product require manual review.', 1;
  IF EXISTS (SELECT 1 FROM Store.ProductMedia m WHERE m.MainPath IS NULL AND (m.CardPath IS NOT NULL OR m.ThumbnailPath IS NOT NULL))
   THROW 51005, 'Media group has orphaned default image paths.', 1;
  IF EXISTS (SELECT 1 FROM Store.ProductMedia m JOIN Store.ProductImage i ON i.IdProductMedia=m.Id AND i.Path=m.MainPath WHERE ISNULL(i.ThumbnailPath,'')<>ISNULL(m.ThumbnailPath,''))
   THROW 51006, 'Default thumbnail differs from gallery thumbnail.', 1;
  CREATE TABLE Store.ProductMediaType (Id int NOT NULL CONSTRAINT PK_ProductMediaType PRIMARY KEY, [Key] nvarchar(50) NOT NULL CONSTRAINT UQ_ProductMediaType_Key UNIQUE);
  INSERT Store.ProductMediaType (Id,[Key]) VALUES (1,N'image'),(2,N'video'),(3,N'document');
  CREATE TABLE Store.ProductAsset (
   Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductAsset PRIMARY KEY,
   IdProduct bigint NOT NULL CONSTRAINT FK_ProductAsset_Product REFERENCES Warehouse.Product(Id),
   IdMediaType int NOT NULL CONSTRAINT FK_ProductAsset_Type REFERENCES Store.ProductMediaType(Id),
   [Path] nvarchar(1000) NOT NULL, ThumbnailPath nvarchar(1000) NULL, CardPath nvarchar(1000) NULL,
   Title nvarchar(200) NULL, SortOrder int NOT NULL, IsDefault bit NOT NULL, CreatedAtUtc datetime2(3) NULL,
   CONSTRAINT CK_ProductAsset_DefaultImage CHECK (IsDefault=0 OR IdMediaType=1)
  );
  INSERT Store.ProductAsset (IdProduct,IdMediaType,[Path],ThumbnailPath,CardPath,SortOrder,IsDefault)
  SELECT m.IdProduct,1,i.[Path],i.ThumbnailPath,CASE WHEN i.[Path]=m.MainPath THEN m.CardPath ELSE REPLACE(i.[Path],'content/products/images/main/','content/products/images/card/') END,i.SortOrder,CASE WHEN i.[Path]=m.MainPath THEN 1 ELSE 0 END
  FROM Store.ProductImage i JOIN Store.ProductMedia m ON m.Id=i.IdProductMedia;
  INSERT Store.ProductAsset (IdProduct,IdMediaType,[Path],SortOrder,IsDefault)
  SELECT m.IdProduct,2,v.[Path],v.SortOrder,0 FROM Store.ProductVideo v JOIN Store.ProductMedia m ON m.Id=v.IdProductMedia;
  INSERT Store.ProductAsset (IdProduct,IdMediaType,[Path],Title,SortOrder,IsDefault,CreatedAtUtc)
  SELECT d.IdProduct,3,d.[Path],d.Title,ROW_NUMBER() OVER (PARTITION BY d.IdProduct ORDER BY d.Id),0,d.CreatedAtUtc FROM Store.ProductDocument d;
  IF (SELECT COUNT_BIG(*) FROM Store.ProductAsset WHERE IdMediaType=1) <> (SELECT COUNT_BIG(*) FROM Store.ProductImage) OR
     (SELECT COUNT_BIG(*) FROM Store.ProductAsset WHERE IdMediaType=2) <> (SELECT COUNT_BIG(*) FROM Store.ProductVideo) OR
     (SELECT COUNT_BIG(*) FROM Store.ProductAsset WHERE IdMediaType=3) <> (SELECT COUNT_BIG(*) FROM Store.ProductDocument)
   THROW 51003, 'Product asset transfer count mismatch.', 1;
  CREATE INDEX IX_ProductAsset_Product_Type_Order ON Store.ProductAsset(IdProduct,IdMediaType,SortOrder,Id);
  CREATE UNIQUE INDEX UX_ProductAsset_Default ON Store.ProductAsset(IdProduct) WHERE IsDefault=1;
  DROP TABLE Store.ProductImage;
  DROP TABLE Store.ProductVideo;
  DROP TABLE Store.ProductDocument;
  DROP TABLE Store.ProductMedia;
  INSERT Store.SchemaMigration (MigrationKey) VALUES (N'103-unified-product-assets');
 END;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
