-- =====================================================
-- Migration 068: Vínculo N:N entre sócios e empresas
-- Description: Permite que um mesmo sócio (pessoa) participe de várias
--              empresas do grupo. shareholders.company_id continua sendo
--              a empresa principal/de origem.
-- =====================================================

USE partnership_manager;

CREATE TABLE IF NOT EXISTS shareholder_companies (
    shareholder_id CHAR(36) NOT NULL,
    company_id CHAR(36) NOT NULL,
    client_id CHAR(36) NOT NULL,
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    PRIMARY KEY (shareholder_id, company_id),
    INDEX idx_shareholder_companies_company (company_id),
    CONSTRAINT fk_shcomp_shareholder FOREIGN KEY (shareholder_id) REFERENCES shareholders(id),
    CONSTRAINT fk_shcomp_company FOREIGN KEY (company_id) REFERENCES companies(id),
    CONSTRAINT fk_shcomp_client FOREIGN KEY (client_id) REFERENCES clients(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Empresas às quais cada sócio está vinculado';

INSERT IGNORE INTO shareholder_companies (shareholder_id, company_id, client_id)
SELECT id, company_id, client_id FROM shareholders;
