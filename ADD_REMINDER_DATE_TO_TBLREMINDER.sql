/*
  Adds reminderDate to dbo.tblReminder if missing.

  Run against ITSS SQL Server database.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.tblReminder', N'U') IS NULL
BEGIN
    PRINT 'WARNING: dbo.tblReminder missing.';
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblReminder')
      AND name = N'reminderDate'
)
BEGIN
    ALTER TABLE dbo.tblReminder
        ADD reminderDate DATETIME2 NULL;

    CREATE INDEX IX_tblReminder_reminderDate
        ON dbo.tblReminder(reminderDate);

    PRINT 'Added dbo.tblReminder.reminderDate + IX_tblReminder_reminderDate.';
END
ELSE
BEGIN
    PRINT 'dbo.tblReminder.reminderDate already exists.';
END;
