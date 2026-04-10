/*
  Reminders + maintenance/repair tables for Property Hub.

  Run against the ITSS SQL Server database.

  - tblReminder: only created if the table does not exist. If you already have tblReminder
    with different column names, align the database or adjust backend/Models/Reminder.cs mappings.
  - tblMaintenanceType + tblMaintenance: created if missing.
*/

SET NOCOUNT ON;

/* ---- tblReminder ---- */
IF OBJECT_ID(N'dbo.tblReminder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblReminder (
        reminderID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        propertyGrpID       INT NULL,
        propertyID           INT NULL,
        reminderTitle        NVARCHAR(255) NOT NULL,
        reminderNotes        NVARCHAR(MAX) NULL,
        dueDate              DATETIME2 NOT NULL,
        isCompleted          BIT NOT NULL CONSTRAINT DF_tblReminder_isCompleted DEFAULT (0),
        createdDate          DATETIME2 NULL CONSTRAINT DF_tblReminder_createdDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tblReminder_PropertyGroup FOREIGN KEY (propertyGrpID) REFERENCES dbo.tblPropertyGroup(propertyGrpID),
        CONSTRAINT FK_tblReminder_Property FOREIGN KEY (propertyID) REFERENCES dbo.tblProperty(propertyID)
    );
    PRINT 'Created dbo.tblReminder';
END
ELSE
    PRINT 'Skipped dbo.tblReminder (already exists). Verify columns match backend/Models/Reminder.cs if EF fails.';

/* ---- tblMaintenanceType (lookup) ---- */
IF OBJECT_ID(N'dbo.tblMaintenanceType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblMaintenanceType (
        maintenanceTypeID    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        maintenanceTypeName  NVARCHAR(200) NOT NULL,
        description          NVARCHAR(MAX) NULL,
        isActive             BIT NULL CONSTRAINT DF_tblMaintenanceType_isActive DEFAULT (1),
        createdDate          DATETIME2 NULL CONSTRAINT DF_tblMaintenanceType_created DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created dbo.tblMaintenanceType';
END
ELSE
    PRINT 'Skipped dbo.tblMaintenanceType (already exists).';

/* ---- tblMaintenance (many rows -> one maintenance type) ---- */
IF OBJECT_ID(N'dbo.tblMaintenance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblMaintenance (
        maintenanceID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        propertyGrpID        INT NOT NULL,
        propertyID           INT NOT NULL,
        maintenanceTypeID    INT NOT NULL,
        summary              NVARCHAR(500) NULL,
        detailNotes          NVARCHAR(MAX) NULL,
        workDate             DATETIME2 NULL,
        createdDate          DATETIME2 NULL CONSTRAINT DF_tblMaintenance_created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tblMaintenance_PropertyGroup FOREIGN KEY (propertyGrpID) REFERENCES dbo.tblPropertyGroup(propertyGrpID),
        CONSTRAINT FK_tblMaintenance_Property FOREIGN KEY (propertyID) REFERENCES dbo.tblProperty(propertyID),
        CONSTRAINT FK_tblMaintenance_Type FOREIGN KEY (maintenanceTypeID) REFERENCES dbo.tblMaintenanceType(maintenanceTypeID)
    );
    CREATE INDEX IX_tblMaintenance_propertyID ON dbo.tblMaintenance(propertyID);
    CREATE INDEX IX_tblMaintenance_propertyGrpID ON dbo.tblMaintenance(propertyGrpID);
    PRINT 'Created dbo.tblMaintenance';
END
ELSE
    PRINT 'Skipped dbo.tblMaintenance (already exists).';
