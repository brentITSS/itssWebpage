-- Check if defaultLoginLandingPage column exists in tblUser table

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
    AND TABLE_NAME = 'tblUser'
    AND COLUMN_NAME = 'defaultLoginLandingPage';

-- If no rows are returned, the column doesn't exist yet
-- Run ADD_DEFAULT_LOGIN_LANDING_PAGE_COLUMN.sql to add it
