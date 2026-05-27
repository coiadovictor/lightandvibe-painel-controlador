-- Cria tabela de usuários do painel no schema public (PostgREST expõe apenas public/storage/graphql_public)
CREATE TABLE IF NOT EXISTS public.painel_users (
    id            SERIAL PRIMARY KEY,
    username      VARCHAR(50)  NOT NULL UNIQUE,
    password_hash TEXT         NOT NULL,
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Usuários iniciais (senhas hasheadas com BCrypt cost=10)
-- marcelo → senha: marcelo
-- victor  → senha: victor
INSERT INTO public.painel_users (username, password_hash) VALUES
    ('marcelo', '$2a$10$Hu0JbzARAdc9.JwuQ0NGI.BAM5tYQo7n1TMzP5RnxyN4jWz76EYU.'),
    ('victor',  '$2a$10$tcUcZ/BJk148/DtBdXcQMe1KFkW/DKNucSx3aRUYbyDiyp5BGgSBa')
ON CONFLICT (username) DO NOTHING;

-- Verificação final
SELECT id, username, is_active, created_at FROM public.painel_users;
