/*
  Add must-change-password flag for admin-issued temporary passwords.
  Run on ITSS SQL Server.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.tblUser', N'U') IS NULL
BEGIN
    PRINT 'Skipped: dbo.tblUser does not exist.';
    RETURN;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblUser')
      AND name = N'mustChangePassword'
)
BEGIN
    ALTER TABLE dbo.tblUser
        ADD mustChangePassword BIT NOT NULL
            CONSTRAINT DF_tblUser_mustChangePassword DEFAULT (0);
    PRINT 'Added dbo.tblUser.mustChangePassword';
END
ELSE
BEGIN
    PRINT 'Skipped: dbo.tblUser.mustChangePassword already exists.';
END
