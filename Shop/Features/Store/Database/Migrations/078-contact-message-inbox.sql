SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'078-contact-message-inbox')
    BEGIN
        IF OBJECT_ID(N'[Account].[Message]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Account].[Message]
            (
                [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Account_Message] PRIMARY KEY,
                [IdProfile] BIGINT NOT NULL,
                [Title] NVARCHAR(200) NOT NULL,
                [Category] NVARCHAR(40) NOT NULL CONSTRAINT [DF_Account_Message_Category] DEFAULT N'notice',
                [Body] NVARCHAR(4000) NOT NULL,
                [IsRead] BIT NOT NULL CONSTRAINT [DF_Account_Message_IsRead] DEFAULT 0,
                [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Account_Message_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
                [ReadAtUtc] DATETIME2(3) NULL,
                CONSTRAINT [FK_Account_Message_Profile] FOREIGN KEY ([IdProfile]) REFERENCES [Account].[Profile]([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_Account_Message_IdProfile_CreatedAtUtc] ON [Account].[Message] ([IdProfile], [CreatedAtUtc] DESC);
        END;
        IF OBJECT_ID(N'[Store].[ContactFormMessage]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Store].[ContactFormMessage]
            (
                [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_ContactFormMessage] PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [Phone] NVARCHAR(30) NOT NULL,
                [SubjectKey] NVARCHAR(30) NOT NULL,
                [Message] NVARCHAR(3000) NOT NULL,
                [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Store_ContactFormMessage_Status] DEFAULT N'new',
                [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_ContactFormMessage_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
                [UpdatedAtUtc] DATETIME2(3) NULL
            );
            CREATE INDEX [IX_Store_ContactFormMessage_Status_CreatedAtUtc]
                ON [Store].[ContactFormMessage] ([Status], [CreatedAtUtc] DESC);
        END;

        IF OBJECT_ID(N'[Admin].[Permission]', N'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.contact-messages.page')
                INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[FeatureKey],[SortOrder],[IsActive])
                VALUES (N'store.contact-messages.page',N'پیام‌های فرم تماس',NULL,N'Store',N'Features',N'Index',N'contact-messages',35,1);
            IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.contact-messages.list')
                INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[FeatureKey],[SortOrder],[IsActive])
                VALUES (N'store.contact-messages.list',N'فهرست پیام‌های تماس',N'store.contact-messages.page',N'Store',N'StoreContactMessagesAdminApi',N'List',NULL,36,1);
            IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.contact-messages.update-status')
                INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[FeatureKey],[SortOrder],[IsActive])
                VALUES (N'store.contact-messages.update-status',N'تغییر وضعیت پیام تماس',N'store.contact-messages.page',N'Store',N'StoreContactMessagesAdminApi',N'UpdateStatus',NULL,37,1);
            IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.account-messages.page')
                INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[FeatureKey],[SortOrder],[IsActive])
                VALUES (N'store.account-messages.page',N'پیام‌های حساب مشتریان',NULL,N'Store',N'Features',N'Index',N'account-messages',38,1);
            IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.account-messages.list')
                INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[FeatureKey],[SortOrder],[IsActive])
                VALUES (N'store.account-messages.list',N'فهرست پیام‌های حساب',N'store.account-messages.page',N'Store',N'StoreAccountMessagesAdminApi',N'List',NULL,39,1);
            IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.account-messages.customers')
                INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[FeatureKey],[SortOrder],[IsActive])
                VALUES (N'store.account-messages.customers',N'جست‌وجوی مشتری برای پیام',N'store.account-messages.page',N'Store',N'StoreAccountMessagesAdminApi',N'Customers',NULL,40,1);
            IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.account-messages.send')
                INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[FeatureKey],[SortOrder],[IsActive])
                VALUES (N'store.account-messages.send',N'ارسال پیام به حساب مشتری',N'store.account-messages.page',N'Store',N'StoreAccountMessagesAdminApi',N'Send',NULL,41,1);
        END;
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'078-contact-message-inbox');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
