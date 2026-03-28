-- Password reset tokens for self-service flow (hashed, time-limited).
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tblPasswordResetToken')
BEGIN
    CREATE TABLE dbo.tblPasswordResetToken (
        passwordResetTokenID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        userID INT NOT NULL,
        tokenHash VARCHAR(64) NOT NULL,
        expiresAtUtc DATETIME2 NOT NULL,
        createdAtUtc DATETIME2 NOT NULL,
        CONSTRAINT FK_tblPasswordResetToken_tblUser
            FOREIGN KEY (userID) REFERENCES dbo.tblUser(userID) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_tblPasswordResetToken_tokenHash ON dbo.tblPasswordResetToken(tokenHash);
    CREATE INDEX IX_tblPasswordResetToken_userID ON dbo.tblPasswordResetToken(userID);
END
