# Understanding the Database Schema

## What We Know

From your `tblRole` table, I can see it has:
- `roleID`
- `userID` 
- `roleTypeID`
- `active`
- `SSMA_TimeStamp`

This suggests `tblRole` is actually a **junction table** (linking users to roles), not a Role definition table.

## Questions

1. **Is there a separate table that defines role names?** 
   - For example: `tblRoleType` or similar?
   - Or are role names stored somewhere else?

2. **What does `roleID` represent in `tblRole`?**
   - Is it a foreign key to another table?
   - Or is it just an identifier?

## What We Need to Know

Please run this query to see ALL tables:

```sql
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

This will help us understand the complete schema structure.
