# lightandvibe-painel-controlador

Monorepo do Painel Controlador — dashboard interno para acompanhamento de funcionários, acessos, conversas e logs.

## Stack

- **Frontend**: React 18 + Vite + TypeScript + Tailwind CSS
- **Backend**: ASP.NET Core 8 Web API + EF Core + Npgsql
- **Banco**: Supabase / Postgres (hospedado na mesma VPS Hostinger)
- **Deploy**: EasyPanel (na VPS Hostinger) — cada app é um service que builda direto do `Dockerfile` do repositório

## Estrutura

```
apps/
  frontend/   # SPA React (Vite + TS + Tailwind)
  backend/    # ASP.NET Core 8 Web API
infra/
  scripts/    # scripts utilitários de dev
.github/
  workflows/  # CI (build + test)
docker-compose.dev.yml   # ambiente de desenvolvimento local
```

## Setup local

### Pré-requisitos
- Node.js 20+
- .NET SDK 8.0+
- Docker + Docker Compose (opcional, só para `make dev`)

### Configuração das variáveis de ambiente

O backend conecta no **Supabase de homologação que roda na VPS Hostinger** — você não precisa subir Postgres local.

1. Copie os arquivos de exemplo:
   ```bash
   cp .env.example .env
   cp apps/backend/PainelControlador.Api/.env.example apps/backend/PainelControlador.Api/.env
   cp apps/frontend/.env.example apps/frontend/.env
   ```
2. Edite `apps/backend/PainelControlador.Api/.env` e preencha:
   - `<HOSTINGER_VPS_HOST>` → IP ou hostname da sua VPS
   - `<PORT>` → porta exposta do Postgres do Supabase (ex.: 5432 ou 54322)
   - `<POSTGRES_PASSWORD>` → senha do Postgres definida na instalação do Supabase

### Rodar

**Opção A — via Docker Compose (recomendado):**
```bash
make dev
```
Sobe frontend em `http://localhost:5173` e backend em `http://localhost:5080`.

**Opção B — sem Docker (2 terminais):**
```bash
# terminal 1
make dev-back

# terminal 2
make dev-front
```

### Health check
```bash
curl http://localhost:5080/api/health
```

## Build

```bash
make build       # builda os dois
make build-front # só frontend
make build-back  # só backend
make test        # roda testes do backend
```

## Deploy

Deploy é feito pelo **EasyPanel** na VPS. Cada app é configurado como um App separado apontando para este repositório:

- **App frontend**: build path = `apps/frontend`, Dockerfile = `apps/frontend/Dockerfile`, build arg `VITE_API_BASE_URL=/api`.
- **App backend**: build path = `apps/backend`, Dockerfile = `apps/backend/Dockerfile`, env vars:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ConnectionStrings__Postgres=Host=<service-postgres-supabase>;Port=5432;Database=postgres;Username=postgres;Password=<senha>`
  - `Cors__AllowedOrigins=https://<dominio-do-frontend>`

Configuração detalhada (domínios, SSL, auto-deploy via webhook) fica a cargo do painel do EasyPanel.

## Endpoints da API

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/health` | status + versão |
| GET | `/api/dashboard/summary` | métricas macro do dashboard |
| GET | `/api/employees` | lista de funcionários |
| GET | `/api/employees/{id}` | detalhes de funcionário |
| GET | `/api/information-types` | tipos de informação |
| GET | `/api/logs` | logs (estrutura inicial) |

Swagger disponível em `http://localhost:5080/swagger` em dev.
