-- =====================================================
-- Migration 070: Métricas financeiras configuráveis por empresa
-- Description: Substitui o conjunto fixo de métricas por definições
--              configuráveis por empresa (financial_metric_definitions),
--              valores das métricas customizadas por período
--              (financial_metric_values) e widgets configuráveis do
--              dashboard financeiro (financial_dashboard_widgets).
--              As métricas padrão (legado) são criadas sob demanda pela
--              aplicação como definições is_system = 1.
-- =====================================================

USE partnership_manager;

CREATE TABLE IF NOT EXISTS financial_metric_definitions (
    id            CHAR(36)      NOT NULL,
    client_id     CHAR(36)      NOT NULL,
    company_id    CHAR(36)      NOT NULL,
    metric_key    VARCHAR(60)   NOT NULL COMMENT 'Identificador estável (snake_case) da métrica na empresa',
    label         VARCHAR(120)  NOT NULL,
    category      VARCHAR(80)   NOT NULL DEFAULT 'Geral',
    data_type     VARCHAR(20)   NOT NULL DEFAULT 'number' COMMENT 'currency | number | integer | percentage',
    unit          VARCHAR(20)   NULL     COMMENT 'Sufixo/unidade opcional exibida (ex.: meses, pts)',
    description   VARCHAR(500)  NULL,
    display_order INT           NOT NULL DEFAULT 0,
    is_system     TINYINT(1)    NOT NULL DEFAULT 0 COMMENT 'Métrica padrão (mapeada para financial_metrics)',
    is_calculated TINYINT(1)    NOT NULL DEFAULT 0 COMMENT 'Calculada pelo sistema, somente leitura',
    is_active     TINYINT(1)    NOT NULL DEFAULT 1,
    created_at    DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    updated_at    DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),

    PRIMARY KEY (id),
    UNIQUE KEY uq_fmd_company_key (company_id, metric_key),
    INDEX idx_fmd_client_company (client_id, company_id),
    CONSTRAINT fk_fmd_client FOREIGN KEY (client_id) REFERENCES clients(id),
    CONSTRAINT fk_fmd_company FOREIGN KEY (company_id) REFERENCES companies(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Definição das métricas financeiras de cada empresa';

CREATE TABLE IF NOT EXISTS financial_metric_values (
    id            CHAR(36)       NOT NULL,
    client_id     CHAR(36)       NOT NULL,
    period_id     CHAR(36)       NOT NULL,
    definition_id CHAR(36)       NOT NULL,
    value         DECIMAL(24,6)  NULL,
    created_at    DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    updated_at    DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),

    PRIMARY KEY (id),
    UNIQUE KEY uq_fmv_period_definition (period_id, definition_id),
    INDEX idx_fmv_definition (definition_id),
    CONSTRAINT fk_fmv_period FOREIGN KEY (period_id) REFERENCES financial_periods(id),
    CONSTRAINT fk_fmv_definition FOREIGN KEY (definition_id) REFERENCES financial_metric_definitions(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Valores das métricas customizadas por período';

CREATE TABLE IF NOT EXISTS financial_dashboard_widgets (
    id            CHAR(36)      NOT NULL,
    client_id     CHAR(36)      NOT NULL,
    company_id    CHAR(36)      NOT NULL,
    title         VARCHAR(120)  NOT NULL,
    widget_type   VARCHAR(20)   NOT NULL COMMENT 'kpi | line | bar | area | gauge',
    metric_keys   VARCHAR(500)  NOT NULL COMMENT 'Lista separada por vírgula de metric_key',
    size          VARCHAR(10)   NOT NULL DEFAULT 'small' COMMENT 'small | large',
    display_order INT           NOT NULL DEFAULT 0,
    created_at    DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    updated_at    DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),

    PRIMARY KEY (id),
    INDEX idx_fdw_client_company (client_id, company_id),
    CONSTRAINT fk_fdw_client FOREIGN KEY (client_id) REFERENCES clients(id),
    CONSTRAINT fk_fdw_company FOREIGN KEY (company_id) REFERENCES companies(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Widgets configuráveis do dashboard financeiro por empresa';
