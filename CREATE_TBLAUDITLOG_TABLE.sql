-- Create tblAuditLog table for tracking all changes in the system
-- This table stores audit trail information for Create, Update, and Delete operations

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tblAuditLog')
BEGIN
    CREATE TABLE [dbo].[tblAuditLog] (
        [auditLogID] INT IDENTITY(1,1) PRIMARY KEY,
        [userID] INT NOT NULL,
        [action] NVARCHAR(50) NOT NULL, -- "Create", "Update", "Delete"
        [entityType] NVARCHAR(100) NOT NULL, -- "User", "Property", "Tenant", etc.
        [entityID] INT NULL,
        [oldValues] NVARCHAR(1000) NULL,
        [newValues] NVARCHAR(1000) NULL,
        [createdDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ipAddress] NVARCHAR(50) NULL,
        
        CONSTRAINT [FK_AuditLog_User] 
            FOREIGN KEY ([userID]) REFERENCES [dbo].[tblUser]([userID])
    );
    
    -- Create indexes for common queries
    CREATE INDEX [IX_AuditLog_UserID] ON [dbo].[tblAuditLog]([userID]);
    CREATE INDEX [IX_AuditLog_EntityType] ON [dbo].[tblAuditLog]([entityType]);
    CREATE INDEX [IX_AuditLog_EntityID] ON [dbo].[tblAuditLog]([entityID]);
    CREATE INDEX [IX_AuditLog_CreatedDate] ON [dbo].[tblAuditLog]([createdDate]);
    
    PRINT 'Table tblAuditLog created successfully';
END
ELSE
BEGIN
    PRINT 'Table tblAuditLog already exists';
END
