SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51690, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'169-warehouse-voucher-names')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'168-status-identity-keys')
            THROW 51691, 'Migration 168 is required.', 1;

        IF OBJECT_ID(N'[Warehouse].[WarehouseDocument]', N'U') IS NULL
           OR OBJECT_ID(N'[Warehouse].[WarehouseDocumentLine]', N'U') IS NULL
           OR OBJECT_ID(N'[Warehouse].[DocumentType]', N'U') IS NULL
           OR OBJECT_ID(N'[Warehouse].[DocumentStatus]', N'U') IS NULL
           OR OBJECT_ID(N'[Warehouse].[DocumentAudit]', N'U') IS NULL
            THROW 51692, 'Expected warehouse document tables are missing.', 1;
        IF OBJECT_ID(N'[Warehouse].[Voucher]', N'U') IS NOT NULL
           OR OBJECT_ID(N'[Warehouse].[VoucherLine]', N'U') IS NOT NULL
           OR OBJECT_ID(N'[Warehouse].[VoucherType]', N'U') IS NOT NULL
           OR OBJECT_ID(N'[Warehouse].[VoucherStatus]', N'U') IS NOT NULL
           OR OBJECT_ID(N'[Warehouse].[VoucherAudit]', N'U') IS NOT NULL
           OR OBJECT_ID(N'[Warehouse].[DocumentNumberSequence]', N'SO') IS NULL
           OR OBJECT_ID(N'[Warehouse].[RebuildDocumentStock]', N'P') IS NULL
           OR OBJECT_ID(N'[Store].[Order]', N'U') IS NULL
            THROW 51696, 'Voucher destination names or required database objects are not in the expected state.', 1;
        IF COL_LENGTH(N'Warehouse.WarehouseDocument', N'IdDocumentType') IS NULL
           OR COL_LENGTH(N'Warehouse.WarehouseDocument', N'IdStatus') IS NULL
           OR COL_LENGTH(N'Warehouse.WarehouseDocument', N'DocumentNumber') IS NULL
           OR COL_LENGTH(N'Warehouse.WarehouseDocument', N'DocumentDate') IS NULL
           OR COL_LENGTH(N'Warehouse.WarehouseDocumentLine', N'IdDocument') IS NULL
           OR COL_LENGTH(N'Warehouse.DocumentAudit', N'DocumentId') IS NULL
           OR COL_LENGTH(N'Warehouse.DocumentAudit', N'DocumentNumber') IS NULL
           OR COL_LENGTH(N'Warehouse.StockMovement', N'IdDocument') IS NULL
           OR COL_LENGTH(N'Warehouse.StockMovement', N'IdDocumentLine') IS NULL
           OR COL_LENGTH(N'Store.Order', N'IdDocument') IS NULL
            THROW 51697, 'Expected document key columns are missing.', 1;

        EXEC sys.sp_rename N'[Warehouse].[WarehouseDocument]', N'Voucher';
        EXEC sys.sp_rename N'[Warehouse].[WarehouseDocumentLine]', N'VoucherLine';
        EXEC sys.sp_rename N'[Warehouse].[DocumentType]', N'VoucherType';
        EXEC sys.sp_rename N'[Warehouse].[DocumentStatus]', N'VoucherStatus';
        EXEC sys.sp_rename N'[Warehouse].[DocumentAudit]', N'VoucherAudit';

        EXEC sys.sp_rename N'[Warehouse].[Voucher].[IdDocumentType]', N'IdVoucherType', N'COLUMN';
        EXEC sys.sp_rename N'[Warehouse].[Voucher].[IdStatus]', N'IdVoucherStatus', N'COLUMN';
        EXEC sys.sp_rename N'[Warehouse].[Voucher].[DocumentNumber]', N'VoucherNumber', N'COLUMN';
        EXEC sys.sp_rename N'[Warehouse].[Voucher].[DocumentDate]', N'VoucherDate', N'COLUMN';
        EXEC sys.sp_rename N'[Warehouse].[VoucherLine].[IdDocument]', N'IdVoucher', N'COLUMN';
        EXEC sys.sp_rename N'[Warehouse].[VoucherAudit].[DocumentId]', N'VoucherId', N'COLUMN';
        EXEC sys.sp_rename N'[Warehouse].[VoucherAudit].[DocumentNumber]', N'VoucherNumber', N'COLUMN';
        EXEC sys.sp_rename N'[Warehouse].[StockMovement].[IdDocument]', N'IdVoucher', N'COLUMN';
        EXEC sys.sp_rename N'[Warehouse].[StockMovement].[IdDocumentLine]', N'IdVoucherLine', N'COLUMN';
        EXEC sys.sp_rename N'[Store].[Order].[IdDocument]', N'IdVoucher', N'COLUMN';
        DECLARE @defaultName sysname;
        DECLARE @dropDefaultSql nvarchar(max);
        SELECT @defaultName = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[Warehouse].[Voucher]') AND c.name = N'VoucherNumber';
        IF @defaultName IS NOT NULL
        BEGIN
            SET @dropDefaultSql = N'ALTER TABLE [Warehouse].[Voucher] DROP CONSTRAINT ' + QUOTENAME(@defaultName) + N';';
            EXEC sys.sp_executesql @dropDefaultSql;
        END;
        EXEC sys.sp_rename N'[Warehouse].[DocumentNumberSequence]', N'VoucherNumberSequence', N'OBJECT';
        ALTER TABLE [Warehouse].[Voucher]
            ADD CONSTRAINT [DF_Voucher_VoucherNumber] DEFAULT (NEXT VALUE FOR [Warehouse].[VoucherNumberSequence]) FOR [VoucherNumber];

        DECLARE @rebuildProcedure nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'[Warehouse].[RebuildDocumentStock]'));
        IF @rebuildProcedure IS NULL
            THROW 51695, 'Warehouse.RebuildDocumentStock procedure is missing.', 1;
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'CREATE PROCEDURE [Warehouse].[RebuildDocumentStock]', N'CREATE OR ALTER PROCEDURE [Warehouse].[RebuildVoucherStock]');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'@IdDocumentType', N'@IdVoucherType');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'@IdDocument', N'@IdVoucher');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'@DocumentExists', N'@VoucherExists');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'[Warehouse].[WarehouseDocumentLine]', N'[Warehouse].[VoucherLine]');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'[Warehouse].[WarehouseDocument]', N'[Warehouse].[Voucher]');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'[Warehouse].[DocumentType]', N'[Warehouse].[VoucherType]');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'Warehouse.DocumentStatus', N'Warehouse.VoucherStatus');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'[IdDocumentType]', N'[IdVoucherType]');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'[IdDocumentLine]', N'[IdVoucherLine]');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'[IdDocument]', N'[IdVoucher]');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'd.IdStatus', N'd.IdVoucherStatus');
        SET @rebuildProcedure = REPLACE(@rebuildProcedure, N'Warehouse document was not found.', N'Warehouse voucher was not found.');
        EXEC sys.sp_executesql @rebuildProcedure;
        EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE [Warehouse].[RebuildDocumentStock] @IdDocument BIGINT AS BEGIN SET NOCOUNT ON; EXEC [Warehouse].[RebuildVoucherStock] @IdVoucher = @IdDocument; END;';

        IF EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.documents')
           AND EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.vouchers')
            THROW 51693, 'Both old and new voucher root permissions exist.', 1;
        IF EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.definitions.document-types')
           AND EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.definitions.voucher-types')
            THROW 51694, 'Both old and new voucher type permissions exist.', 1;
        IF EXISTS (
            SELECT 1 FROM [Admin].[Permission] oldPermission
            JOIN [Admin].[Permission] newPermission
              ON newPermission.[PermissionKey] = REPLACE(oldPermission.[PermissionKey], N'warehouse.documents.', N'warehouse.vouchers.')
            WHERE oldPermission.[PermissionKey] LIKE N'warehouse.documents.%')
            THROW 51698, 'Both old and new voucher child permissions exist.', 1;

        ALTER TABLE [Admin].[Permission] NOCHECK CONSTRAINT [FK_Admin_Permission_Parent];
        UPDATE [Admin].[Permission]
        SET [ParentPermissionKey] = N'warehouse.vouchers'
        WHERE [ParentPermissionKey] = N'warehouse.documents';
        UPDATE [Admin].[Permission]
        SET [PermissionKey] = N'warehouse.vouchers'
        WHERE [PermissionKey] = N'warehouse.documents';

        UPDATE [Admin].[Permission]
        SET [PermissionKey] = REPLACE([PermissionKey], N'warehouse.documents.', N'warehouse.vouchers.')
        WHERE [PermissionKey] LIKE N'warehouse.documents.%';
        UPDATE [Admin].[Permission]
        SET [ParentPermissionKey] = REPLACE([ParentPermissionKey], N'warehouse.documents.', N'warehouse.vouchers.')
        WHERE [ParentPermissionKey] LIKE N'warehouse.documents.%';
        UPDATE [Admin].[Permission]
        SET [Controller] = N'VouchersApi',
            [PermissionKey] = REPLACE([PermissionKey], N'warehouse.documents.', N'warehouse.vouchers.')
        WHERE [Area] = N'Warehouse' AND [Controller] = N'DocumentsApi';

        UPDATE [Admin].[Permission]
        SET [PermissionKey] = N'warehouse.definitions.voucher-types', [FeatureKey] = N'voucher-types'
        WHERE [PermissionKey] = N'warehouse.definitions.document-types';
        ALTER TABLE [Admin].[Permission] WITH CHECK CHECK CONSTRAINT [FK_Admin_Permission_Parent];

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'169-warehouse-voucher-names');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
