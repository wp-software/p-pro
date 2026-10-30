-- Migration 076: Grupos de aprovação e modelos (templates) de fluxo
-- Description: Permite que uma etapa seja atribuída a um grupo de aprovadores com modo
--              'any' (basta um membro aprovar) ou 'all' (todos os membros devem aprovar),
--              e salvar fluxos como modelos reutilizáveis.

CREATE TABLE IF NOT EXISTS approval_groups (
  id            CHAR(36)     NOT NULL,
  company_id    CHAR(36)     NOT NULL,
  name          VARCHAR(100) NOT NULL,
  description   VARCHAR(500) NULL,
  approval_mode ENUM('any','all') NOT NULL DEFAULT 'any',
  is_active     TINYINT(1)   NOT NULL DEFAULT 1,
  created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  created_by    CHAR(36)     NULL,
  updated_by    CHAR(36)     NULL,
  PRIMARY KEY (id),
  CONSTRAINT fk_apgrp_company FOREIGN KEY (company_id) REFERENCES companies(id),
  UNIQUE KEY uq_apgrp_company_name (company_id, name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS approval_group_members (
  group_id CHAR(36)  NOT NULL,
  user_id  CHAR(36)  NOT NULL,
  added_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (group_id, user_id),
  CONSTRAINT fk_apgm_group FOREIGN KEY (group_id) REFERENCES approval_groups(id) ON DELETE CASCADE,
  CONSTRAINT fk_apgm_user  FOREIGN KEY (user_id)  REFERENCES users(id),
  INDEX idx_apgm_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS workflow_templates (
  id            CHAR(36)     NOT NULL,
  company_id    CHAR(36)     NOT NULL,
  name          VARCHAR(150) NOT NULL,
  description   VARCHAR(500) NULL,
  workflow_type VARCHAR(50)  NOT NULL,
  priority      ENUM('low','medium','high','urgent') NOT NULL DEFAULT 'medium',
  is_active     TINYINT(1)   NOT NULL DEFAULT 1,
  created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  created_by    CHAR(36)     NULL,
  updated_by    CHAR(36)     NULL,
  PRIMARY KEY (id),
  CONSTRAINT fk_wftpl_company FOREIGN KEY (company_id) REFERENCES companies(id),
  INDEX idx_wftpl_company (company_id, is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS workflow_template_steps (
  id               CHAR(36)     NOT NULL,
  template_id      CHAR(36)     NOT NULL,
  step_order       INT          NOT NULL,
  name             VARCHAR(100) NOT NULL,
  description      TEXT         NULL,
  step_type        ENUM('approval','review','notification','automated') NOT NULL DEFAULT 'approval',
  assigned_role    VARCHAR(50)  NULL,
  assigned_user_id CHAR(36)     NULL,
  assigned_group_id CHAR(36)    NULL,
  approval_mode    ENUM('any','all') NULL,
  due_mode         ENUM('none','calendar_days','business_days') NOT NULL DEFAULT 'none',
  due_days         INT          NOT NULL DEFAULT 0,
  PRIMARY KEY (id),
  CONSTRAINT fk_wftplstep_tpl   FOREIGN KEY (template_id)       REFERENCES workflow_templates(id) ON DELETE CASCADE,
  CONSTRAINT fk_wftplstep_group FOREIGN KEY (assigned_group_id) REFERENCES approval_groups(id) ON DELETE SET NULL,
  INDEX idx_wftplstep_tpl (template_id, step_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Etapas de workflow passam a poder apontar para um grupo; os membros elegíveis
-- são "congelados" em workflow_step_approvers na criação do fluxo.
ALTER TABLE workflow_steps
  ADD COLUMN assigned_group_id CHAR(36) NULL,
  ADD COLUMN approval_mode ENUM('any','all') NULL,
  ADD CONSTRAINT fk_wfstep_group FOREIGN KEY (assigned_group_id) REFERENCES approval_groups(id) ON DELETE SET NULL;

CREATE TABLE IF NOT EXISTS workflow_step_approvers (
  workflow_step_id CHAR(36) NOT NULL,
  user_id          CHAR(36) NOT NULL,
  PRIMARY KEY (workflow_step_id, user_id),
  CONSTRAINT fk_wfsa_step FOREIGN KEY (workflow_step_id) REFERENCES workflow_steps(id) ON DELETE CASCADE,
  CONSTRAINT fk_wfsa_user FOREIGN KEY (user_id)          REFERENCES users(id),
  INDEX idx_wfsa_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- DOWN (rollback)
-- DROP TABLE IF EXISTS workflow_step_approvers;
-- ALTER TABLE workflow_steps DROP FOREIGN KEY fk_wfstep_group, DROP COLUMN assigned_group_id, DROP COLUMN approval_mode;
-- DROP TABLE IF EXISTS workflow_template_steps;
-- DROP TABLE IF EXISTS workflow_templates;
-- DROP TABLE IF EXISTS approval_group_members;
-- DROP TABLE IF EXISTS approval_groups;
