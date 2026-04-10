/*
  Reminders + maintenance/repair tables for Property Hub.

  Run against the ITSS SQL Server database.

  - tblReminder: production table already exists. The app maps to:
      reminderID, tenantID, tenancyID, propertyGrpID, propertyID,
      reminder, reminderDetail, createdBy, createdDate, reminderActive, SSMA_TimeStamp.
    This script does NOT create tblReminder.
  - tblMaintenanceType + tblMaintenance: created if missing.
  - For maintenance status + reminder priority lookups and FK columns, run
    ADD_MAINTENANCE_STATUS_REMINDER_PRIORITY.sql (separate script).
*/

SET NOCOUNT ON;

/* ---- tblReminder (existing — no DDL here) ---- */
IF OBJECT_ID(N'dbo.tblReminder', N'U') IS NOT NULL
    PRINT 'dbo.tblReminder present — ensure FKs exist for tenantID/tenancyID/propertyGrpID/propertyID as needed.';
ELSE
    PRINT 'WARNING: dbo.tblReminder missing — create it to match backend/Models/Reminder.cs before using reminders API.';

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
