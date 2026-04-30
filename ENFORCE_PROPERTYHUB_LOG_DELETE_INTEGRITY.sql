/*
Purpose:
  Enforce non-orphan behavior for Journal/Contact log children.

Design notes:
  1) For direct FK relationships (attachments + tags), use ON DELETE CASCADE.
  2) For tblCalendarAppointment (polymorphic sourceType/sourceID), SQL FK is not possible.
     We enforce integrity for journallog/contactlog source types via cleanup + trigger.
  3) This script is idempotent and safe to re-run.
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '1) Cleaning existing orphan child rows...';

-- Orphan attachments
DELETE a
FROM tblJournalLogAttachment a
LEFT JOIN tblJournalLog j ON j.journalLogID = a.journalLogID
WHERE a.journalLogID IS NOT NULL
  AND j.journalLogID IS NULL;

DELETE a
FROM tblContactLogAttachment a
LEFT JOIN tblContactLog c ON c.contactLogID = a.contactLogID
WHERE a.contactLogID IS NOT NULL
  AND c.contactLogID IS NULL;

-- Orphan tags against journal/contact
DELETE t
FROM tblTagLog t
LEFT JOIN tblJournalLog j ON j.journalLogID = t.journalLogID
WHERE t.journalLogID IS NOT NULL
  AND j.journalLogID IS NULL;

DELETE t
FROM tblTagLog t
LEFT JOIN tblContactLog c ON c.contactLogID = t.contactLogID
WHERE t.contactLogID IS NOT NULL
  AND c.contactLogID IS NULL;

-- Orphan calendar rows for journal/contact source types
DELETE ca
FROM tblCalendarAppointment ca
LEFT JOIN tblJournalLog j
  ON ca.sourceType = 'journallog'
 AND ca.sourceID = j.journalLogID
WHERE ca.sourceType = 'journallog'
  AND j.journalLogID IS NULL;

DELETE ca
FROM tblCalendarAppointment ca
LEFT JOIN tblContactLog c
  ON ca.sourceType = 'contactlog'
 AND ca.sourceID = c.contactLogID
WHERE ca.sourceType = 'contactlog'
  AND c.contactLogID IS NULL;

PRINT '2) Rebuilding key FK constraints with ON DELETE CASCADE...';

DECLARE @sql NVARCHAR(MAX);

-- Drop any FK currently bound to tblJournalLogAttachment.journalLogID
SELECT @sql = STRING_AGG(
    'ALTER TABLE [dbo].[tblJournalLogAttachment] DROP CONSTRAINT [' + fk.name + '];',
    ' '
)
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.tblJournalLogAttachment')
  AND c.name = 'journalLogID';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

-- Drop any FK currently bound to tblContactLogAttachment.contactLogID
SELECT @sql = STRING_AGG(
    'ALTER TABLE [dbo].[tblContactLogAttachment] DROP CONSTRAINT [' + fk.name + '];',
    ' '
)
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.tblContactLogAttachment')
  AND c.name = 'contactLogID';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

-- Drop any FK currently bound to tblTagLog.journalLogID
SELECT @sql = STRING_AGG(
    'ALTER TABLE [dbo].[tblTagLog] DROP CONSTRAINT [' + fk.name + '];',
    ' '
)
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.tblTagLog')
  AND c.name = 'journalLogID';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

-- Drop any FK currently bound to tblTagLog.contactLogID
SELECT @sql = STRING_AGG(
    'ALTER TABLE [dbo].[tblTagLog] DROP CONSTRAINT [' + fk.name + '];',
    ' '
)
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.tblTagLog')
  AND c.name = 'contactLogID';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

-- Recreate with explicit, stable names
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblJournalLogAttachment_tblJournalLog_Cascade')
BEGIN
    ALTER TABLE dbo.tblJournalLogAttachment WITH CHECK
        ADD CONSTRAINT FK_tblJournalLogAttachment_tblJournalLog_Cascade
            FOREIGN KEY (journalLogID) REFERENCES dbo.tblJournalLog (journalLogID)
            ON DELETE CASCADE;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblContactLogAttachment_tblContactLog_Cascade')
BEGIN
    ALTER TABLE dbo.tblContactLogAttachment WITH CHECK
        ADD CONSTRAINT FK_tblContactLogAttachment_tblContactLog_Cascade
            FOREIGN KEY (contactLogID) REFERENCES dbo.tblContactLog (contactLogID)
            ON DELETE CASCADE;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblTagLog_tblJournalLog_Cascade')
BEGIN
    ALTER TABLE dbo.tblTagLog WITH CHECK
        ADD CONSTRAINT FK_tblTagLog_tblJournalLog_Cascade
            FOREIGN KEY (journalLogID) REFERENCES dbo.tblJournalLog (journalLogID)
            ON DELETE CASCADE;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblTagLog_tblContactLog_Cascade')
BEGIN
    ALTER TABLE dbo.tblTagLog WITH CHECK
        ADD CONSTRAINT FK_tblTagLog_tblContactLog_Cascade
            FOREIGN KEY (contactLogID) REFERENCES dbo.tblContactLog (contactLogID)
            ON DELETE CASCADE;
END;

PRINT '3) Enforcing calendar appointment integrity for journal/contact source types...';

-- Trigger ensures new/updated journal/contact calendar rows cannot reference missing parents.
EXEC('
CREATE OR ALTER TRIGGER dbo.TR_tblCalendarAppointment_ValidateSource
ON dbo.tblCalendarAppointment
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN dbo.tblJournalLog j
          ON i.sourceType = ''journallog''
         AND i.sourceID = j.journalLogID
        WHERE i.sourceType = ''journallog''
          AND j.journalLogID IS NULL
    )
    BEGIN
        THROW 51001, ''Invalid CalendarAppointment source: journallog sourceID does not exist.'', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN dbo.tblContactLog c
          ON i.sourceType = ''contactlog''
         AND i.sourceID = c.contactLogID
        WHERE i.sourceType = ''contactlog''
          AND c.contactLogID IS NULL
    )
    BEGIN
        THROW 51002, ''Invalid CalendarAppointment source: contactlog sourceID does not exist.'', 1;
    END
END;
');

COMMIT TRANSACTION;
PRINT 'Integrity enforcement complete.';
