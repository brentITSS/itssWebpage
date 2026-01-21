-- Check if tenantID is an identity column
SELECT 
    c.name AS ColumnName,
    c.is_identity AS IsIdentity,
    c.seed_value AS SeedValue,
    c.increment_value AS IncrementValue
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'dbo'
    AND t.name = 'tblTenant'
    AND c.name = 'tenantID';

-- If the above shows IsIdentity = 0, then run the following to make it an identity column
-- NOTE: This requires dropping and recreating the column, which will lose data
-- Only run this if you're sure the table is empty or you've backed up the data

-- Step 1: Check if table has data (if it does, you'll need to back it up first)
SELECT COUNT(*) AS [RowCount] FROM [dbo].[tblTenant];

-- Step 2: If table is empty or you've backed up, run these commands:
-- (Uncomment and run if needed)

/*
-- Create a temporary table with the same structure
SELECT * INTO [dbo].[tblTenant_temp] FROM [dbo].[tblTenant] WHERE 1 = 0;

-- Drop the original table
DROP TABLE [dbo].[tblTenant];

-- Recreate the table with tenantID as identity
CREATE TABLE [dbo].[tblTenant] (
    [tenantID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [tenancyID] INT NULL,
    [firstName] NVARCHAR(100) NOT NULL,
    [secondName] NVARCHAR(100) NOT NULL,
    [tenantDOB] DATETIME NOT NULL,
    [tenantEmail] NVARCHAR(255) NULL,
    [identification] NVARCHAR(255) NULL,
    [mobile] NVARCHAR(50) NULL
);

-- Restore data from temp table (if any)
SET IDENTITY_INSERT [dbo].[tblTenant] ON;
INSERT INTO [dbo].[tblTenant] ([tenantID], [tenancyID], [firstName], [secondName], [tenantDOB], [tenantEmail], [identification], [mobile])
SELECT [tenantID], [tenancyID], [firstName], [secondName], [tenantDOB], [tenantEmail], [identification], [mobile]
FROM [dbo].[tblTenant_temp];
SET IDENTITY_INSERT [dbo].[tblTenant] OFF;

-- Drop temp table
DROP TABLE [dbo].[tblTenant_temp];
*/
