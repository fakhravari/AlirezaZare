SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'034-product-feedback-from-details')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM [Store].[ProductDetail] d
               WHERE (COALESCE(d.[ReviewsJson], N'[]') <> N'[]' OR COALESCE(d.[QuestionsJson], N'[]') <> N'[]')
                 AND NOT EXISTS (SELECT 1 FROM [Store].[Product] p WHERE p.[DetailKey] = d.[DetailKey]))
        THROW 51034, 'Product detail feedback has no product mapping.', 1;

    ALTER TABLE [Store].[ProductFeedback] ADD [IsSample] BIT NOT NULL CONSTRAINT [DF_Store_ProductFeedback_IsSample] DEFAULT (0) WITH VALUES;

    CREATE TABLE #LegacyFeedback
    (
        [IdProduct] BIGINT NOT NULL, [FeedbackType] NVARCHAR(20) NOT NULL,
        [DisplayName] NVARCHAR(MAX) NULL, [FeedbackText] NVARCHAR(MAX) NULL,
        [RatingText] NVARCHAR(MAX) NULL, [AdminReply] NVARCHAR(MAX) NULL,
        [SourceJson] NVARCHAR(MAX) NOT NULL
    );
    INSERT #LegacyFeedback ([IdProduct], [FeedbackType], [DisplayName], [FeedbackText], [RatingText], [AdminReply], [SourceJson])
    SELECT p.[Id], N'reviews', JSON_VALUE(j.[value], '$[0]'), JSON_VALUE(j.[value], '$[1]'),
           JSON_VALUE(j.[value], '$[2]'), JSON_VALUE(j.[value], '$[3]'), j.[value]
    FROM [Store].[Product] p JOIN [Store].[ProductDetail] d ON d.[DetailKey] = p.[DetailKey]
    CROSS APPLY OPENJSON(d.[ReviewsJson]) j;

    INSERT #LegacyFeedback ([IdProduct], [FeedbackType], [DisplayName], [FeedbackText], [RatingText], [AdminReply], [SourceJson])
    SELECT p.[Id], N'questions', N'کاربر مهمان', JSON_VALUE(j.[value], '$[0]'), NULL,
           CONCAT(JSON_VALUE(j.[value], '$[1]'),
               CASE WHEN NULLIF(JSON_VALUE(j.[value], '$[3]'), N'') IS NOT NULL
                    THEN CONCAT(NCHAR(10), NCHAR(10), JSON_VALUE(j.[value], '$[3]')) ELSE N'' END), j.[value]
    FROM [Store].[Product] p JOIN [Store].[ProductDetail] d ON d.[DetailKey] = p.[DetailKey]
    CROSS APPLY OPENJSON(d.[QuestionsJson]) j;

    IF EXISTS (SELECT 1 FROM #LegacyFeedback f
               WHERE ISJSON(f.[SourceJson]) <> 1 OR LEFT(LTRIM(f.[SourceJson]), 1) <> N'['
                  OR EXISTS (SELECT 1 FROM OPENJSON(f.[SourceJson]) x WHERE TRY_CONVERT(INT, x.[key]) > 3)
                  OR NULLIF(LTRIM(RTRIM(f.[DisplayName])), N'') IS NULL OR LEN(f.[DisplayName]) > 80
                  OR NULLIF(LTRIM(RTRIM(f.[FeedbackText])), N'') IS NULL OR LEN(f.[FeedbackText]) > 800
                  OR LEN(f.[AdminReply]) > 1000
                  OR (f.[FeedbackType] = N'reviews' AND (TRY_CONVERT(TINYINT, f.[RatingText]) IS NULL OR TRY_CONVERT(TINYINT, f.[RatingText]) NOT BETWEEN 1 AND 5))
                  OR (f.[FeedbackType] = N'questions' AND COALESCE(JSON_VALUE(f.[SourceJson], '$[2]'), N'0') <> N'0'))
        THROW 51034, 'Legacy feedback has an unsupported shape or exceeds column limits.', 1;

    INSERT [Store].[ProductFeedback] ([IdProduct], [FeedbackType], [DisplayName], [FeedbackText], [Rating], [Status], [AdminReply], [RepliedAtUtc], [IsSample])
    SELECT [IdProduct], [FeedbackType], [DisplayName], [FeedbackText], TRY_CONVERT(TINYINT, [RatingText]), N'pending',
           NULLIF([AdminReply], N''), CASE WHEN NULLIF([AdminReply], N'') IS NULL THEN NULL ELSE SYSUTCDATETIME() END, 1
    FROM #LegacyFeedback;

    ALTER TABLE [Store].[ProductDetail] DROP CONSTRAINT [CK_Store_ProductDetail_Json];
    ALTER TABLE [Store].[ProductDetail] DROP COLUMN [ReviewsJson], [QuestionsJson];
    ALTER TABLE [Store].[ProductDetail] ADD CONSTRAINT [CK_Store_ProductDetail_Json]
        CHECK (([HighlightsJson] IS NULL OR ISJSON([HighlightsJson]) = 1) AND ([SpecsJson] IS NULL OR ISJSON([SpecsJson]) = 1));
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'034-product-feedback-from-details');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
