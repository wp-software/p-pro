-- Links a user to a shareholder (person). A shareholder can have at most one user.
-- Companies reachable by the user are derived from shareholder_companies.

ALTER TABLE users
    ADD COLUMN shareholder_id CHAR(36) NULL AFTER company_id;

ALTER TABLE users
    ADD UNIQUE INDEX idx_user_shareholder (shareholder_id),
    ADD CONSTRAINT fk_user_shareholder FOREIGN KEY (shareholder_id)
        REFERENCES shareholders(id) ON DELETE SET NULL;

-- Backfill by e-mail, only when the user/shareholder match is one-to-one inside the tenant.
UPDATE users u
JOIN (
    SELECT user_id, shareholder_id
    FROM (
        SELECT u2.id AS user_id,
               sh.id AS shareholder_id,
               COUNT(*) OVER (PARTITION BY u2.id) AS shareholders_per_user,
               COUNT(*) OVER (PARTITION BY sh.id) AS users_per_shareholder
        FROM users u2
        JOIN shareholders sh
          ON sh.client_id = u2.client_id
         AND LOWER(sh.email) = LOWER(u2.email)
         AND sh.is_deleted = 0
        WHERE u2.is_deleted = 0
    ) pairs
    WHERE shareholders_per_user = 1 AND users_per_shareholder = 1
) m ON m.user_id = u.id
SET u.shareholder_id = m.shareholder_id
WHERE u.shareholder_id IS NULL;

