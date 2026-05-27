-- =============================================================================
-- Migration 001 — Schema painel + tabela de usuários do dashboard
-- Como executar: Supabase Studio → SQL Editor → cole e execute.
-- =============================================================================

-- Schema dedicado ao painel controlador (separado do schema público do ERP)
CREATE SCHEMA IF NOT EXISTS painel;

-- Tabela de usuários do dashboard
CREATE TABLE IF NOT EXISTS painel.users (
    id           SERIAL PRIMARY KEY,
    username     VARCHAR(50)  NOT NULL UNIQUE,
    password_hash TEXT         NOT NULL,
    is_active    BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Usuários iniciais (senhas hasheadas com BCrypt cost=10)
-- marcelo → senha: marcelo
-- victor  → senha: victor
INSERT INTO painel.users (username, password_hash) VALUES
    ('marcelo', '$2a$10$Hu0JbzARAdc9.JwuQ0NGI.BAM5tYQo7n1TMzP5RnxyN4jWz76EYU.'),
    ('victor',  '$2a$10$tcUcZ/BJk148/DtBdXcQMe1KFkW/DKNucSx3aRUYbyDiyp5BGgSBa')
ON CONFLICT (username) DO NOTHING;

-- Permite que o PostgREST acesse a tabela com a service_role key
GRANT USAGE ON SCHEMA painel TO service_role;
GRANT ALL   ON TABLE  painel.users TO service_role;
GRANT USAGE, SELECT ON SEQUENCE painel.users_id_seq TO service_role;

-- Verificação final
SELECT id, username, is_active, created_at FROM painel.users;
