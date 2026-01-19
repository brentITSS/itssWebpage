-- Run this query in your PropertyHub database to see the actual column names in tblUser

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tblUser'
ORDER BY ORDINAL_POSITION;
