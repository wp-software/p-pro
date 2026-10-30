-- Share classes without a company_id are shared across the tenant.
-- Existing company-specific classes remain scoped to their original company.

ALTER TABLE share_classes
    MODIFY COLUMN company_id CHAR(36) NULL;

ALTER TABLE share_classes
    ADD COLUMN tenant_shared_code VARCHAR(20)
        GENERATED ALWAYS AS (
            CASE WHEN company_id IS NULL AND is_deleted = 0 THEN UPPER(code) ELSE NULL END
        ) STORED,
    ADD UNIQUE KEY uk_share_class_client_shared_code (client_id, tenant_shared_code),
    ADD KEY idx_share_class_tenant_scope_code (client_id, company_id, code);
