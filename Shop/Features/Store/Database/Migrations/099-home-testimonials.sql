SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'099-home-testimonials')
    BEGIN
        IF OBJECT_ID(N'[Store].[HomeTestimonial]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Store].[HomeTestimonial]
            (
                [IdProductFeedback] BIGINT NOT NULL,
                [ImagePath] NVARCHAR(500) NULL,
                [SortOrder] INT NOT NULL CONSTRAINT [DF_Store_HomeTestimonial_SortOrder] DEFAULT (0),
                [IsActive] BIT NOT NULL CONSTRAINT [DF_Store_HomeTestimonial_IsActive] DEFAULT (1),
                [UpdatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_HomeTestimonial_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
                CONSTRAINT [PK_Store_HomeTestimonial] PRIMARY KEY ([IdProductFeedback]),
                CONSTRAINT [FK_Store_HomeTestimonial_ProductFeedback] FOREIGN KEY ([IdProductFeedback]) REFERENCES [Store].[ProductFeedback]([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_Store_HomeTestimonial_Active_Sort]
                ON [Store].[HomeTestimonial]([IsActive], [SortOrder], [IdProductFeedback]);
        END;

        ;WITH ApprovedReviews AS
        (
            SELECT TOP (6) [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc] DESC, [Id] DESC) AS [SortOrder]
            FROM [Store].[ProductFeedback]
            WHERE [FeedbackType] = N'reviews' AND [Status] = N'approved'
            ORDER BY [CreatedAtUtc] DESC, [Id] DESC
        )
        INSERT INTO [Store].[HomeTestimonial] ([IdProductFeedback], [SortOrder], [IsActive])
        SELECT review.[Id], CONVERT(INT, review.[SortOrder]), 1
        FROM ApprovedReviews review
        WHERE NOT EXISTS (SELECT 1 FROM [Store].[HomeTestimonial] existing WHERE existing.[IdProductFeedback] = review.[Id]);

        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.features.testimonials')
            INSERT INTO [Admin].[Permission] ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES (N'store.features.testimonials', N'دیدگاه‌های صفحهٔ نخست', N'store.customers.group', N'Store', N'Features', N'Index', N'testimonials', 29, 1);

        ;WITH RelatedApiPermissions AS
        (
            SELECT [PermissionKey], [Name], [Action]
            FROM (VALUES
                (N'store.api.testimonials.list', N'فهرست دیدگاه‌های صفحهٔ نخست', N'List'),
                (N'store.api.testimonials.save', N'مدیریت دیدگاه‌های صفحهٔ نخست', N'Save'),
                (N'store.api.testimonials.upload', N'بارگذاری تصویر دیدگاه', N'UploadImage'),
                (N'store.api.testimonials.delete-image', N'حذف تصویر دیدگاه', N'DeleteImage')
            ) permissions([PermissionKey], [Name], [Action])
        )
        INSERT INTO [Admin].[Permission] ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
        SELECT api.[PermissionKey], api.[Name], N'store.features.testimonials', N'Store', N'StoreHomeTestimonialsAdminApi', api.[Action], N'testimonials', 1, 1
        FROM RelatedApiPermissions api
        WHERE NOT EXISTS (SELECT 1 FROM [Admin].[Permission] existing WHERE existing.[PermissionKey] = api.[PermissionKey]);

        ;WITH PermissionTree AS
        (
            SELECT rolePermission.[IdRole], permission.[PermissionKey]
            FROM [Admin].[RolePermission] rolePermission
            INNER JOIN [Admin].[Permission] permission ON permission.[Id] = rolePermission.[IdPermission]
            UNION ALL
            SELECT tree.[IdRole], child.[PermissionKey]
            FROM PermissionTree tree
            INNER JOIN [Admin].[Permission] child ON child.[ParentPermissionKey] = tree.[PermissionKey] AND child.[IsActive] = 1
        )
        INSERT INTO [Admin].[RolePermission] ([IdRole], [IdPermission])
        SELECT DISTINCT tree.[IdRole], permission.[Id]
        FROM PermissionTree tree
        CROSS JOIN [Admin].[Permission] permission
        WHERE tree.[PermissionKey] = N'store.customers.group'
          AND permission.[IsActive] = 1
          AND permission.[ParentPermissionKey] = N'store.customers.group'
          AND NOT EXISTS (SELECT 1 FROM [Admin].[RolePermission] existing WHERE existing.[IdRole] = tree.[IdRole] AND existing.[IdPermission] = permission.[Id])
        OPTION (MAXRECURSION 32767);

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'099-home-testimonials');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
