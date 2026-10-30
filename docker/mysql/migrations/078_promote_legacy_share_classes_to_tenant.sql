-- =====================================================
-- 078: Promove classes de ações legadas (por empresa) para o escopo do tenant
-- Requer a migração 077. Para cada (client_id, código) mantém UMA classe
-- (a mais antiga) como tenant-wide (company_id = NULL). Classes duplicadas
-- com o mesmo código continuam vinculadas à sua empresa e, na empresa,
-- prevalecem sobre a classe do tenant (preserva direitos distintos e ações já emitidas).
-- =====================================================

UPDATE share_classes sc
JOIN (
    SELECT id
    FROM (
        SELECT id,
               ROW_NUMBER() OVER (
                   PARTITION BY client_id, UPPER(code)
                   ORDER BY created_at, id
               ) AS rn
        FROM share_classes
        WHERE is_deleted = 0 AND company_id IS NOT NULL
    ) ranked
    WHERE rn = 1
) pick ON pick.id = sc.id
SET sc.company_id = NULL
WHERE NOT EXISTS (
    SELECT 1 FROM (
        SELECT client_id, UPPER(code) AS code_up
        FROM share_classes
        WHERE is_deleted = 0 AND company_id IS NULL
    ) t
    WHERE t.client_id = sc.client_id AND t.code_up = UPPER(sc.code)
);
