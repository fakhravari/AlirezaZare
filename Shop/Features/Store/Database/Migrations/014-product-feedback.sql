SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'014-product-feedback')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    CREATE TABLE [Store].[ProductFeedback]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [IdProfile] BIGINT NULL,
        [FeedbackType] NVARCHAR(20) NOT NULL,
        [DisplayName] NVARCHAR(80) NOT NULL,
        [FeedbackText] NVARCHAR(800) NOT NULL,
        [Rating] TINYINT NULL,
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Store_ProductFeedback_Status] DEFAULT(N'pending'),
        [AdminReply] NVARCHAR(1000) NULL,
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_ProductFeedback_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [RepliedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Store_ProductFeedback] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductFeedback_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Store].[Product]([Id]),
        CONSTRAINT [FK_Store_ProductFeedback_Profile] FOREIGN KEY ([IdProfile]) REFERENCES [Account].[Profile]([Id]) ON DELETE SET NULL,
        CONSTRAINT [CK_Store_ProductFeedback_Type] CHECK ([FeedbackType] IN(N'reviews',N'questions')),
        CONSTRAINT [CK_Store_ProductFeedback_Rating] CHECK (([FeedbackType]=N'reviews' AND [Rating] IS NOT NULL AND [Rating] BETWEEN 1 AND 5) OR ([FeedbackType]=N'questions' AND [Rating] IS NULL)),
        CONSTRAINT [CK_Store_ProductFeedback_Status] CHECK ([Status] IN(N'pending',N'approved',N'rejected'))
    );
    CREATE INDEX [IX_Store_ProductFeedback_Product_Type_Status_Created]
        ON [Store].[ProductFeedback]([IdProduct],[FeedbackType],[Status],[CreatedAtUtc] DESC);
    CREATE INDEX [IX_Store_ProductFeedback_Profile]
        ON [Store].[ProductFeedback]([IdProfile],[IdProduct],[FeedbackType]);

    INSERT INTO [Store].[SchemaMigration]([MigrationKey]) VALUES(N'014-product-feedback');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
