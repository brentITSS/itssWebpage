/*
  Add color storage for tag types used in Property Hub lookups.
  Run on ITSS SQL Server.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.tblTagType', N'U') IS NULL
BEGIN
    PRINT 'Skipped: dbo.tblTagType does not exist.';
    RETURN;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblTagType')
      AND name = N'tagColor'
)
BEGIN
    ALTER TABLE dbo.tblTagType
        ADD tagColor NVARCHAR(32) NULL;

    PRINT 'Added dbo.tblTagType.tagColor';
END
ELSE
BEGIN
    PRINT 'Skipped: dbo.tblTagType.tagColor already exists.';
END
