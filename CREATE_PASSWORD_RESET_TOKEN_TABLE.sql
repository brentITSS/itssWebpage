/*
  Create password reset token storage used by /api/auth/forgot-password
  and /api/auth/complete-password-reset.

  Run on ITSS SQL Server.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.tblPasswordResetToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblPasswordResetToken (
        passwordResetTokenID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        userID               INT NOT NULL,
        tokenHash            NVARCHAR(64) NOT NULL,
        expiresAtUtc         DATETIME2 NOT NULL,
        createdAtUtc         DATETIME2 NOT NULL,
        CONSTRAINT FK_tblPasswordResetToken_User
            FOREIGN KEY (userID) REFERENCES dbo.tblUser(userID)
    );

    CREATE INDEX IX_tblPasswordResetToken_tokenHash
        ON dbo.tblPasswordResetToken(tokenHash);

    CREATE INDEX IX_tblPasswordResetToken_userID
        ON dbo.tblPasswordResetToken(userID);

    PRINT 'Created dbo.tblPasswordResetToken';
END
ELSE
BEGIN
    PRINT 'Skipped dbo.tblPasswordResetToken (already exists).';
END
