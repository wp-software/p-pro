-- Migration 074: Data Room — controle de acesso por perfil e log de atividades
-- Description: Pasta sem perfis vinculados = acesso liberado a todos os perfis da empresa.
--              Com perfis vinculados = somente esses perfis (Admin/SuperAdmin sempre acessam).

CREATE TABLE IF NOT EXISTS data_room_folder_roles (
  folder_id  CHAR(36)    NOT NULL,
  role       VARCHAR(50) NOT NULL,
  granted_by CHAR(36)    NULL,
  granted_at TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (folder_id, role),
  CONSTRAINT fk_drfr_folder FOREIGN KEY (folder_id) REFERENCES data_room_folders(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS data_room_activity_logs (
  id            CHAR(36)     NOT NULL,
  data_room_id  CHAR(36)     NOT NULL,
  folder_id     CHAR(36)     NULL,
  folder_name   VARCHAR(200) NULL,
  document_id   CHAR(36)     NULL,
  document_name VARCHAR(300) NULL,
  action        VARCHAR(50)  NOT NULL,
  user_id       CHAR(36)     NOT NULL,
  user_name     VARCHAR(200) NULL,
  details       VARCHAR(500) NULL,
  created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  INDEX idx_dral_room_date (data_room_id, created_at),
  INDEX idx_dral_folder (folder_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- DOWN (rollback)
-- DROP TABLE IF EXISTS data_room_activity_logs;
-- DROP TABLE IF EXISTS data_room_folder_roles;
