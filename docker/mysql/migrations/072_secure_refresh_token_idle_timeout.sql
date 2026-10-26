-- Hash active refresh tokens and limit existing sessions to five more minutes.
-- Run once before deploying the API version that looks up refresh tokens by SHA-256.
USE partnership_manager;

UPDATE users
SET refresh_token = LOWER(SHA2(refresh_token, 256))
WHERE refresh_token IS NOT NULL
  AND refresh_token NOT REGEXP '^[0-9a-f]{64}$';

UPDATE users
SET refresh_token_expiry = LEAST(
    refresh_token_expiry,
    DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 5 MINUTE)
)
WHERE refresh_token IS NOT NULL
  AND refresh_token_expiry IS NOT NULL;
