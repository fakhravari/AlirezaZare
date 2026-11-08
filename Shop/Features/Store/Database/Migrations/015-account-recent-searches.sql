SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'015-account-recent-searches')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    CREATE TABLE [Account].[RecentSearch]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProfile] BIGINT NOT NULL,
        [SearchKey] NVARCHAR(80) NOT NULL,
        [SearchText] NVARCHAR(80) NOT NULL,
        [SearchedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Account_RecentSearch_SearchedAtUtc] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Account_RecentSearch] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Account_RecentSearch_Profile] FOREIGN KEY ([IdProfile]) REFERENCES [Account].[Profile]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_Account_RecentSearch_Profile_Key] UNIQUE ([IdProfile], [SearchKey])
    );
    CREATE INDEX [IX_Account_RecentSearch_Profile_Date]
        ON [Account].[RecentSearch]([IdProfile], [SearchedAtUtc] DESC);

    INSERT INTO [Store].[SchemaMigration]([MigrationKey]) VALUES(N'015-account-recent-searches');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
