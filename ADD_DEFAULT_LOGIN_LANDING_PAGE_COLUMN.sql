-- Add defaultLoginLandingPage column to tblUser table
-- This column stores the default page path that a user should be redirected to after login

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('tblUser') 
    AND name = 'defaultLoginLandingPage'
)
BEGIN
    ALTER TABLE tblUser
    ADD defaultLoginLandingPage NVARCHAR(255) NULL;
    
    PRINT 'Column defaultLoginLandingPage added to tblUser table';
END
ELSE
BEGIN
    PRINT 'Column defaultLoginLandingPage already exists in tblUser table';
END

-- Example values that could be set:
-- '/Admin' - for Global Admin users
-- '/Property Hub/Home' - for Property Hub users
-- '/Property Hub/Admin' - for Property Hub Admin users
-- NULL - will default to '/Admin' if not set
