-- Check column names for tblJournalLogAttachment table
SELECT 'tblJournalLogAttachment' AS TableName, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tblJournalLogAttachment'
ORDER BY ORDINAL_POSITION;
