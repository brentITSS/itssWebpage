-- Grants for itss-email-processor workflow writes on PropertyHub.
-- Replace [your_email_processor_db_user] with the SQL user from the function app
-- ConnectionStrings__DefaultConnection (user name only, not the full connection string).
--
-- Run in the PropertyHub database as a user with GRANT permission (e.g. dbo).

DECLARE @User SYSNAME = N'your_email_processor_db_user';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tblCalendarAppointment')
BEGIN
    GRANT SELECT, INSERT, UPDATE ON dbo.tblCalendarAppointment TO [your_email_processor_db_user];
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tblTagLog')
BEGIN
    GRANT SELECT, INSERT ON dbo.tblTagLog TO [your_email_processor_db_user];
END;

-- Optional reference (likely already granted if journal logs work):
-- GRANT SELECT, INSERT ON dbo.tblJournalLog TO [your_email_processor_db_user];
-- GRANT SELECT, INSERT ON dbo.tblJournalLogAttachment TO [your_email_processor_db_user];
-- GRANT SELECT, INSERT, UPDATE ON dbo.tbldocumentworkflowauditrun TO [your_email_processor_db_user];
-- GRANT SELECT, INSERT, UPDATE ON dbo.tbldocumentworkflowauditstep TO [your_email_processor_db_user];
-- GRANT SELECT, INSERT ON dbo.tbldocumentworkflowextractionsnapshot TO [your_email_processor_db_user];
