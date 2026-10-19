-- =====================================================
-- Migration 071: Escopo (anual / competência) dos widgets do dashboard financeiro
-- Description: Separa informações anuais das informações por competência (mês).
--              scope: annual | monthly (somente KPIs podem ser mensais).
--              aggregation: last | sum | avg (consolidação de KPIs anuais).
-- =====================================================

USE partnership_manager;

ALTER TABLE financial_dashboard_widgets
    ADD COLUMN scope VARCHAR(10) NOT NULL DEFAULT 'annual' COMMENT 'annual | monthly' AFTER size,
    ADD COLUMN aggregation VARCHAR(10) NOT NULL DEFAULT 'last' COMMENT 'last | sum | avg' AFTER scope;

-- KPIs já existentes mostravam o mês mais recente: passam a ser KPIs por competência.
UPDATE financial_dashboard_widgets SET scope = 'monthly' WHERE widget_type = 'kpi';
