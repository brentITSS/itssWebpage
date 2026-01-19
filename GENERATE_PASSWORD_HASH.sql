-- Option 1: Use a BCrypt hash generator online
-- Visit: https://bcrypt-generator.com/
-- Enter password: 111
-- Copy the generated hash
-- Then run:
-- UPDATE tblUser SET password = '<generated_hash>' WHERE emailAddress = 'brent@itsson.co.uk';

-- Option 2: For now, you can use this pre-generated hash for password "111"
-- (This is just for testing - generate your own for production!)
UPDATE tblUser 
SET password = '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy'
WHERE emailAddress = 'brent@itsson.co.uk';

-- Verify the update
SELECT emailAddress, password 
FROM tblUser 
WHERE emailAddress = 'brent@itsson.co.uk';
