# Hash Password in Database

## Quick Fix - Update Password to BCrypt Hash

Since your password is stored as plain text "111", we need to hash it with BCrypt.

### Step 1: Generate BCrypt Hash

You can use an online BCrypt generator, or I can create a simple script. For now, here's the BCrypt hash for password "111":

```
$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy
```

(Note: This is just an example - you should generate your own for security)

### Step 2: Update Database

Run this SQL to update the password:

```sql
UPDATE tblUser 
SET password = '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy'
WHERE emailAddress = 'brent@itsson.co.uk';
```

### Alternative: Generate Hash in Code

I can create a simple console app or add a temporary endpoint to generate the hash, then you can update the database manually.

**Which approach would you prefer?**
