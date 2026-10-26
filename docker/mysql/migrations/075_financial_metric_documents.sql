-- Migration 075: Documentos vinculados a períodos/indicadores financeiros
-- Description: Liga documentos enviados ao Data Room (pasta Financeiro/MM-AAAA) a um período
--              e, opcionalmente, a um indicador específico (metric_key). metric_key NULL = documento do período.

CREATE TABLE IF NOT EXISTS financial_metric_documents (
  id           CHAR(36)    NOT NULL,
  period_id    CHAR(36)    NOT NULL,
  metric_key   VARCHAR(60) NULL,
  document_id  CHAR(36)    NOT NULL,
  folder_id    CHAR(36)    NOT NULL,
  created_by   CHAR(36)    NULL,
  created_at   TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_fmdoc_document (document_id),
  INDEX idx_fmdoc_period_metric (period_id, metric_key),
  CONSTRAINT fk_fmdoc_period FOREIGN KEY (period_id) REFERENCES financial_periods(id),
  CONSTRAINT fk_fmdoc_document FOREIGN KEY (document_id) REFERENCES documents(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- DOWN (rollback)
-- DROP TABLE IF EXISTS financial_metric_documents;
