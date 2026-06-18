-- Grant the email-processor SQL user access to calendar appointments created by workflows.
-- Replace [your_email_processor_db_user] with the login/user from the function app
-- ConnectionStrings__DefaultConnection (Azure SQL user name only, not the full connection string).

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tblCalendarAppointment')
BEGIN
    -- MERGE requires SELECT + INSERT + UPDATE on the target table.
    GRANT SELECT, INSERT, UPDATE ON dbo.tblCalendarAppointment TO [your_email_processor_db_user];
END;
