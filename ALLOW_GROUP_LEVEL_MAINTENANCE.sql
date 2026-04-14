/*
  Allow group-level maintenance records without a specific property.
  Run on ITSS SQL Server.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.tblMaintenance', N'U') IS NULL
BEGIN
    PRINT 'Skipped: dbo.tblMaintenance does not exist.';
    RETURN;
END

IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME = 'tblMaintenance'
      AND COLUMN_NAME = 'propertyID'
      AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE dbo.tblMaintenance
        ALTER COLUMN propertyID INT NULL;
    PRINT 'Altered dbo.tblMaintenance.propertyID to NULL.';
END
ELSE
BEGIN
    PRINT 'Skipped: dbo.tblMaintenance.propertyID is already nullable.';
END
