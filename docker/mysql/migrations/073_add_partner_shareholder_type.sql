USE partnership_manager;

ALTER TABLE shareholders DROP CONSTRAINT chk_shareholder_type;
ALTER TABLE shareholders
ADD CONSTRAINT chk_shareholder_type
CHECK (type IN ('Founder', 'Investor', 'Employee', 'Advisor', 'ESOP', 'Partner', 'Other'));
