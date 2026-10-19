-- =====================================================
-- Migration 069: Configuração societária por empresa
-- Description: Tipo de sócio e cláusulas passam a ser definidos por
--              vínculo sócio x empresa (shareholder_companies).
-- =====================================================

ALTER TABLE shareholder_companies
    ADD COLUMN type VARCHAR(20) NOT NULL DEFAULT 'Other',
    ADD COLUMN earn_out TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN tag_along TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN drag_along TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN shareholders_agreement TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN right_of_first_refusal TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN liquidation_preference_right TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN anti_dilution TINYINT(1) NOT NULL DEFAULT 0;

UPDATE shareholder_companies sc
JOIN shareholders s ON s.id = sc.shareholder_id
SET sc.type = s.type,
    sc.earn_out = s.earn_out,
    sc.tag_along = s.tag_along,
    sc.drag_along = s.drag_along,
    sc.shareholders_agreement = s.shareholders_agreement,
    sc.right_of_first_refusal = s.right_of_first_refusal,
    sc.liquidation_preference_right = s.liquidation_preference_right,
    sc.anti_dilution = s.anti_dilution;
