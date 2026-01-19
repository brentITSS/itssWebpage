# Fix Database Column Name Mismatch

## The Problem

The error shows:
```
Invalid column name 'Email'
Invalid column name 'CreatedDate'
Invalid column name 'IsActive'
Invalid column name 'ModifiedDate'
Invalid column name 'PasswordHash'
```

This means the database column names don't match the model property names.

## Solution

We need to add `[Column]` attributes to the User model to map the properties to the actual database column names.

## Step 1: Check Actual Column Names

Run this SQL query in your `PropertyHub` database:

```sql
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tblUser'
ORDER BY ORDINAL_POSITION;
```

This will show you the actual column names in your database.

## Step 2: Update the User Model

Once you know the actual column names, we'll update `backend/Models/User.cs` to add `[Column]` attributes like this:

```csharp
[Column("EmailAddress")]  // if the column is actually "EmailAddress"
public string Email { get; set; } = string.Empty;
```

## Common Column Name Variations

The database might use:
- `EmailAddress` instead of `Email`
- `EmailAddress` instead of `Email`
- `CreatedOn` instead of `CreatedDate`
- `IsActive` might be `Active` or `Enabled`
- `Password` instead of `PasswordHash`

**Please run the SQL query and share the column names, then I'll update the model for you.**
