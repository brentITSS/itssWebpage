-- Persist RunSummarisation / RunExtraction outputs on workflow audit runs (SQL Server)

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbldocumentworkflowauditrun')
BEGIN
    IF COL_LENGTH('dbo.tbldocumentworkflowauditrun', 'SummarisationText') IS NULL
        ALTER TABLE dbo.tbldocumentworkflowauditrun ADD SummarisationText NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.tbldocumentworkflowauditrun', 'ExtractionJson') IS NULL
        ALTER TABLE dbo.tbldocumentworkflowauditrun ADD ExtractionJson NVARCHAR(MAX) NULL;
END;
GO
