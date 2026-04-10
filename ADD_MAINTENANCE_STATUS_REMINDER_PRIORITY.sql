/*
  Lookup tables: tblMaintenanceStatus, tblReminderPriority
  Adds optional FK columns to tblMaintenance and tblReminder.

  Run on ITSS SQL Server after base Property Hub tables exist.
*/

SET NOCOUNT ON;

/* ---- tblMaintenanceStatus ---- */
IF OBJECT_ID(N'dbo.tblMaintenanceStatus', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblMaintenanceStatus (
        maintenanceStatusID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        maintenanceStatusName NVARCHAR(200) NOT NULL,
        description           NVARCHAR(MAX) NULL,
        sortOrder             INT NULL CONSTRAINT DF_tblMaintenanceStatus_sort DEFAULT (0),
        isActive              BIT NULL CONSTRAINT DF_tblMaintenanceStatus_active DEFAULT (1),
        createdDate           DATETIME2 NULL CONSTRAINT DF_tblMaintenanceStatus_created DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created dbo.tblMaintenanceStatus';
END
ELSE
    PRINT 'Skipped dbo.tblMaintenanceStatus (already exists).';

/* ---- tblReminderPriority ---- */
IF OBJECT_ID(N'dbo.tblReminderPriority', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblReminderPriority (
        reminderPriorityID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        reminderPriorityName NVARCHAR(200) NOT NULL,
        description          NVARCHAR(MAX) NULL,
        displayColor         NVARCHAR(32) NULL,
        sortOrder            INT NULL CONSTRAINT DF_tblReminderPriority_sort DEFAULT (0),
        isActive             BIT NULL CONSTRAINT DF_tblReminderPriority_active DEFAULT (1),
        createdDate          DATETIME2 NULL CONSTRAINT DF_tblReminderPriority_created DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created dbo.tblReminderPriority';
END
ELSE
    PRINT 'Skipped dbo.tblReminderPriority (already exists).';

/* ---- tblMaintenance.maintenanceStatusID ---- */
IF OBJECT_ID(N'dbo.tblMaintenance', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.tblMaintenance') AND name = N'maintenanceStatusID')
BEGIN
    ALTER TABLE dbo.tblMaintenance ADD maintenanceStatusID INT NULL;
    PRINT 'Added tblMaintenance.maintenanceStatusID';
END
ELSE IF OBJECT_ID(N'dbo.tblMaintenance', N'U') IS NOT NULL
    PRINT 'tblMaintenance.maintenanceStatusID already present or table missing.';

/* ---- FK maintenance -> status (when both exist) ---- */
IF OBJECT_ID(N'dbo.tblMaintenance', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.tblMaintenanceStatus', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_tblMaintenance_MaintenanceStatus')
BEGIN
    ALTER TABLE dbo.tblMaintenance
        ADD CONSTRAINT FK_tblMaintenance_MaintenanceStatus
        FOREIGN KEY (maintenanceStatusID) REFERENCES dbo.tblMaintenanceStatus(maintenanceStatusID);
    PRINT 'Added FK_tblMaintenance_MaintenanceStatus';
END

/* ---- tblReminder.reminderPriorityID ---- */
IF OBJECT_ID(N'dbo.tblReminder', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.tblReminder') AND name = N'reminderPriorityID')
BEGIN
    ALTER TABLE dbo.tblReminder ADD reminderPriorityID INT NULL;
    PRINT 'Added tblReminder.reminderPriorityID';
END
ELSE IF OBJECT_ID(N'dbo.tblReminder', N'U') IS NOT NULL
    PRINT 'tblReminder.reminderPriorityID already present or table missing.';

IF OBJECT_ID(N'dbo.tblReminder', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.tblReminderPriority', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_tblReminder_ReminderPriority')
BEGIN
    ALTER TABLE dbo.tblReminder
        ADD CONSTRAINT FK_tblReminder_ReminderPriority
        FOREIGN KEY (reminderPriorityID) REFERENCES dbo.tblReminderPriority(reminderPriorityID);
    PRINT 'Added FK_tblReminder_ReminderPriority';
END

PRINT 'Done. Seed statuses/priorities via Property Hub Admin > Lookups.';
