-- Grant the email-processor SQL user access to workflow extraction snapshot rows.
-- Replace [your_email_processor_db_user] with the login/user from the function app
-- ConnectionStrings__DefaultConnection (Azure SQL user name only, not the full connection string).

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbldocumentworkflowextractionsnapshot')
BEGIN
    GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.tbldocumentworkflowextractionsnapshot TO [your_email_processor_db_user];
END;
