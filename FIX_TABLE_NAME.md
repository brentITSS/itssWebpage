# Fix Table Name Issue

## The Problem

Error: `Invalid object name 'tblUserRole'`

This means the table name in the database doesn't match what the model expects.

## Check Actual Table Names

Run this SQL query in your `PropertyHub` database:

```sql
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

Look for tables related to user roles - they might be named:
- `tblUserRoles` (plural)
- `UserRole` (no tbl prefix)
- `tblUser_Role` (with underscore)
- Or something else

## Common Variations

The database might use:
- Plural table names: `tblUserRoles` instead of `tblUserRole`
- No prefix: `UserRole` instead of `tblUserRole`
- Different casing: `tblUserrole` or `tblUSERROLE`

**Please run the query and share what table names you see, especially any that relate to users and roles.**
