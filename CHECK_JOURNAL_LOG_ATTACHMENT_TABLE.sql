-- Check if tblJournalLogAttachment table exists and what columns it has
-- Run this query to see the exact column names

-- First, check if table exists
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'tblJournalLogAttachment'
    AND TABLE_SCHEMA = 'dbo';

-- If table exists, get all columns
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tblJournalLogAttachment'
    AND TABLE_SCHEMA = 'dbo'
ORDER BY ORDINAL_POSITION;

-- Also check for similar table names (in case of typo)
SELECT 
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%Journal%Attachment%'
    AND TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME;
