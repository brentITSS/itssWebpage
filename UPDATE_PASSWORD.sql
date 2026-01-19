-- Step 1: Generate BCrypt hash for password "111"
-- Visit: https://bcrypt-generator.com/
-- Enter password: 111
-- Copy the generated hash (it will look like: $2a$11$...)

-- Step 2: Update the password in the database
-- Replace <HASH_HERE> with the hash you generated
UPDATE tblUser 
SET password = '<HASH_HERE>'
WHERE emailAddress = 'brent@itsson.co.uk';

-- Step 3: Verify the update
SELECT emailAddress, LEFT(password, 20) AS password_preview
FROM tblUser 
WHERE emailAddress = 'brent@itsson.co.uk';
-- The password should now start with $2a$ or $2b$
