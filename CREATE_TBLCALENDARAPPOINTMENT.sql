/*
  Adds dbo.tblCalendarAppointment if missing.
  Used to link one calendar appointment to one source record
  (reminder / maintenance / contact log / journal log) and avoid conflicting copies.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.tblCalendarAppointment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblCalendarAppointment
    (
        calendarAppointmentID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblCalendarAppointment PRIMARY KEY,
        sourceType NVARCHAR(50) NOT NULL,
        sourceID INT NOT NULL,
        appointmentDate DATETIME2 NOT NULL,
        isAllDay BIT NOT NULL CONSTRAINT DF_tblCalendarAppointment_isAllDay DEFAULT(1),
        titleOverride NVARCHAR(255) NULL,
        notes NVARCHAR(MAX) NULL,
        active BIT NOT NULL CONSTRAINT DF_tblCalendarAppointment_active DEFAULT(1),
        createdDate DATETIME2 NOT NULL CONSTRAINT DF_tblCalendarAppointment_createdDate DEFAULT (SYSUTCDATETIME()),
        modifiedDate DATETIME2 NULL
    );

    CREATE UNIQUE INDEX UX_tblCalendarAppointment_source
        ON dbo.tblCalendarAppointment(sourceType, sourceID);

    CREATE INDEX IX_tblCalendarAppointment_appointmentDate
        ON dbo.tblCalendarAppointment(appointmentDate);

    PRINT 'Created dbo.tblCalendarAppointment + indexes.';
END
ELSE
BEGIN
    PRINT 'dbo.tblCalendarAppointment already exists.';
END;
