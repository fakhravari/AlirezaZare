SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'046-iran-bank-catalog')
    BEGIN
        CREATE TABLE [Store].[Bank]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_Bank] PRIMARY KEY,
            [Code] VARCHAR(40) NOT NULL CONSTRAINT [UQ_Store_Bank_Code] UNIQUE,
            [NameFa] NVARCHAR(150) NOT NULL CONSTRAINT [UQ_Store_Bank_NameFa] UNIQUE,
            [InstitutionType] VARCHAR(20) NOT NULL,
            [IsActive] BIT NOT NULL CONSTRAINT [DF_Store_Bank_IsActive] DEFAULT (1),
            CONSTRAINT [CK_Store_Bank_Type] CHECK ([InstitutionType] IN ('bank', 'credit-institution')),
            CONSTRAINT [CK_Store_Bank_Name] CHECK (LEN(LTRIM(RTRIM([NameFa]))) > 0)
        );

        INSERT INTO [Store].[Bank] ([Code], [NameFa], [InstitutionType]) VALUES
        ('melli', N'بانک ملی ایران', 'bank'),
        ('sepah', N'بانک سپه', 'bank'),
        ('keshavarzi', N'بانک کشاورزی', 'bank'),
        ('maskan', N'بانک مسکن', 'bank'),
        ('export-development', N'بانک توسعه صادرات ایران', 'bank'),
        ('industry-mine', N'بانک صنعت و معدن', 'bank'),
        ('post-bank', N'پست بانک ایران', 'bank'),
        ('taavon', N'بانک توسعه تعاون', 'bank'),
        ('mellat', N'بانک ملت', 'bank'),
        ('saderat', N'بانک صادرات ایران', 'bank'),
        ('tejarat', N'بانک تجارت', 'bank'),
        ('refah', N'بانک رفاه کارگران', 'bank'),
        ('karafarin', N'بانک کارآفرین', 'bank'),
        ('saman', N'بانک سامان', 'bank'),
        ('eghtesad-novin', N'بانک اقتصاد نوین', 'bank'),
        ('pasargad', N'بانک پاسارگاد', 'bank'),
        ('parsian', N'بانک پارسیان', 'bank'),
        ('sarmayeh', N'بانک سرمایه', 'bank'),
        ('sina', N'بانک سینا', 'bank'),
        ('shahr', N'بانک شهر', 'bank'),
        ('iran-zamin', N'بانک ایران زمین', 'bank'),
        ('tourism', N'بانک گردشگری', 'bank'),
        ('day', N'بانک دی', 'bank'),
        ('mehr-iran', N'بانک قرض‌الحسنه مهر ایران', 'bank'),
        ('middle-east', N'بانک خاورمیانه', 'bank'),
        ('iran-venezuela', N'بانک مشترک ایران و ونزوئلا', 'bank'),
        ('resalat', N'بانک قرض‌الحسنه رسالت', 'bank'),
        ('melal', N'مؤسسه اعتباری ملل', 'credit-institution');

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'046-iran-bank-catalog');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
