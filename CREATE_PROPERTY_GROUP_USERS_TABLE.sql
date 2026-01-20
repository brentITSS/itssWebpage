-- Create table for Property Group User access control
-- This allows users to be assigned to specific property groups
-- Similar to tblWorkstreamUsers but for property group-level access

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tblPropertyGroupUsers')
BEGIN
    CREATE TABLE [dbo].[tblPropertyGroupUsers] (
        [propertyGroupUserID] INT IDENTITY(1,1) PRIMARY KEY,
        [userID] INT NOT NULL,
        [propertyGrpID] INT NOT NULL,
        [active] BIT NOT NULL DEFAULT 1,
        
        CONSTRAINT [FK_PropertyGroupUsers_User] 
            FOREIGN KEY ([userID]) REFERENCES [dbo].[tblUser]([userID]),
        CONSTRAINT [FK_PropertyGroupUsers_PropertyGroup] 
            FOREIGN KEY ([propertyGrpID]) REFERENCES [dbo].[tblPropertyGroup]([propertyGrpID]),
        CONSTRAINT [UQ_PropertyGroupUsers_User_PropertyGroup] 
            UNIQUE ([userID], [propertyGrpID])
    );
    
    CREATE INDEX [IX_PropertyGroupUsers_UserID] ON [dbo].[tblPropertyGroupUsers]([userID]);
    CREATE INDEX [IX_PropertyGroupUsers_PropertyGrpID] ON [dbo].[tblPropertyGroupUsers]([propertyGrpID]);
    
    PRINT 'Table tblPropertyGroupUsers created successfully';
END
ELSE
BEGIN
    PRINT 'Table tblPropertyGroupUsers already exists';
END
