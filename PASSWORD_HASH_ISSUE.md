# Password Hash Issue - "Invalid salt version"

## The Problem

The error "Invalid salt version" occurs when BCrypt tries to verify a password hash that isn't in BCrypt format.

## Possible Causes

1. **Passwords are stored in plain text** (not hashed)
2. **Passwords use a different hashing algorithm** (not BCrypt)
3. **Password format is incorrect**

## Solution Options

### Option 1: Check what format passwords are stored in

Run this SQL query to see what the password hash looks like:
```sql
SELECT TOP 1 emailAddress, password 
FROM tblUser 
WHERE emailAddress = 'brent@itsson.co.uk';
```

BCrypt hashes typically start with `$2a$`, `$2b$`, or `$2y$` and are 60 characters long.

### Option 2: If passwords are plain text

We'll need to either:
- Hash existing passwords with BCrypt
- OR temporarily use a different verification method for existing users

### Option 3: If passwords use a different algorithm

We'll need to update the verification logic to match the existing algorithm.

## Next Steps

Please run the SQL query above and share what the password value looks like. This will help determine the best fix.
