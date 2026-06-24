/*
  Add trackingDataOnly to tblJournalLog.
  When true, journal entries are excluded from financial reports (filtering applied later).

  Run on ITSS SQL Server.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.tblJournalLog', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.tblJournalLog') AND name = N'trackingDataOnly')
BEGIN
    ALTER TABLE dbo.tblJournalLog
        ADD trackingDataOnly BIT NOT NULL
            CONSTRAINT DF_tblJournalLog_trackingDataOnly DEFAULT (0);
    PRINT 'Added tblJournalLog.trackingDataOnly (default 0 / false).';
END
ELSE IF OBJECT_ID(N'dbo.tblJournalLog', N'U') IS NOT NULL
    PRINT 'tblJournalLog.trackingDataOnly already present.';

-- Ensure all existing rows are false (idempotent).
IF OBJECT_ID(N'dbo.tblJournalLog', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.tblJournalLog') AND name = N'trackingDataOnly')
BEGIN
    UPDATE dbo.tblJournalLog
    SET trackingDataOnly = 0
    WHERE trackingDataOnly <> 0;

    PRINT 'Updated existing tblJournalLog rows to trackingDataOnly = 0.';
END

PRINT 'Done.';
