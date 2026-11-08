SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Admin].[SchemaMigration] WHERE [MigrationKey] = N'002-store-feedback-permissions')
    BEGIN
        INSERT INTO [Admin].[Permission]
            ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [SortOrder])
        VALUES
            (N'store.feedback.read.reviews', N'مشاهدهٔ دیدگاه‌های محصول', N'store.reviews', N'Store', N'StoreFeedbackApi', N'Reviews', 10),
            (N'store.feedback.moderate.reviews', N'مدیریت دیدگاه‌های محصول', N'store.reviews', N'Store', N'StoreFeedbackApi', N'ModerateReview', 20),
            (N'store.feedback.read.questions', N'مشاهدهٔ پرسش‌های محصول', N'store.questions', N'Store', N'StoreFeedbackApi', N'Questions', 10),
            (N'store.feedback.moderate.questions', N'مدیریت پرسش‌های محصول', N'store.questions', N'Store', N'StoreFeedbackApi', N'ModerateQuestion', 20);

        INSERT INTO [Admin].[RolePermission] ([IdRole], [IdPermission])
        SELECT role.[Id], permission.[Id]
        FROM [Admin].[Role] AS role
        CROSS JOIN [Admin].[Permission] AS permission
        WHERE role.[RoleKey] = N'system.full-access'
          AND permission.[PermissionKey] IN
              (N'store.feedback.read.reviews', N'store.feedback.moderate.reviews',
               N'store.feedback.read.questions', N'store.feedback.moderate.questions');

        INSERT INTO [Admin].[SchemaMigration] ([MigrationKey]) VALUES (N'002-store-feedback-permissions');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
