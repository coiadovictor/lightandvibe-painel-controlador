# Logs Internos do Ambiente — Design

**Data:** 2026-06-11
**Branch:** `feat/logs-internos-ambiente`

## Objetivo

Criar uma página no painel admin chamada **"Logs Internos do Ambiente"** que mostra,
quase em tempo real, a saúde e as falhas dos containers do stack do chatbot
(Evolution API, n8n e Postgres) rodando no EasyPanel/Docker Swarm da VPS.

Motivação: diagnosticar se o serviço do WhatsApp (Evolution) caiu/reiniciou/foi
OOM-killed/desconectou, com horário e ocorrido — sem precisar abrir SSH no host.

## Não-objetivos (YAGNI)

- Não controla containers (sem start/stop/restart). **Estritamente leitura.**
- Não persiste histórico de logs em banco. Lê on-demand do Docker.
- Não substitui observabilidade completa (Grafana/Loki). É um diagnóstico rápido.

## Fonte dos dados

A Docker Engine API, acessada via **Unix socket** `/var/run/docker.sock` montado
**read-only** no container do backend do painel.

- Saúde: `GET /containers/{id}/json` → `State.Status`, `State.StartedAt`,
  `State.OOMKilled`, `State.ExitCode`, `RestartCount`, `Config.Tty`, `Image`.
- Logs: `GET /containers/{id}/logs?stdout=1&stderr=1&timestamps=1&tail=N` →
  stream multiplexado (header de 8 bytes por frame quando o container não tem TTY).
- Lista: `GET /containers/json?all=true` → para resolver nome→id.

Sem dependência externa: usa `SocketsHttpHandler.ConnectCallback` com
`UnixDomainSocketEndPoint` (nativo no .NET 8).

### Resolução de nomes (Docker Swarm)

No Swarm os containers se chamam `stack_service.replica.taskid` e o `taskid` muda
a cada deploy. Por isso o match é por **prefixo de serviço + "."**:
um container casa com a chave `K` se `nome` começa com `K + "."` (ou `nome == K`,
ou, como fallback, contém `K`). Ex.: a chave `n8n_evolution-api` casa com
`n8n_evolution-api.1.lgmw...` mas **não** com `n8n_evolution-api-db.1...`.

### Configuração

`MONITORED_CONTAINERS` (env var, CSV). Cada item é `matcher` ou `alias=matcher`.
Default:
```
MONITORED_CONTAINERS=Evolution=n8n_evolution-api,n8n=n8n_n8n,Postgres Evolution=n8n_evolution-api-db,Postgres n8n=n8n_n8n-postgres
```
`DOCKER_SOCKET_PATH` (env var, opcional). Default `/var/run/docker.sock`.

## Arquitetura

### Backend (ASP.NET Core 8)

- `Configuration/DockerOptions.cs` — lê `MONITORED_CONTAINERS` e `DOCKER_SOCKET_PATH`,
  expõe a lista parseada de `(Alias, Matcher)` e `SocketPath`.
- `Dtos/AmbienteDtos.cs` — `ContainerHealthDto`, `LogLineDto`, `IncidentDto`,
  `AmbienteOverviewDto`, `ContainerLogsDto`.
- `Services/IDockerLogsService.cs` + `DockerLogsService.cs` — cliente HTTP sobre o
  socket. Responsável por: listar/resolver containers, inspecionar saúde, buscar e
  **demultiplexar** logs, e derivar incidentes.
- `Controllers/AmbienteController.cs` — `[Authorize]`, read-only.

Endpoints:

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/ambiente/overview` | saúde de todos os monitorados + incidentes agregados |
| GET | `/api/ambiente/logs?container={alias}&tail={N}` | tail de log de 1 container |

Resposta degrada com elegância: se o socket não existir/falhar, `available=false`
e mensagem clara (em vez de 500). Frontend mostra banner de "socket não montado".

### Detecção de incidentes

Combina fatos estruturais (do inspect) + varredura de log (tail ~500):

- `RestartCount > 0` → incidente "reiniciou N vez(es)".
- `OOMKilled == true` → incidente "morto por falta de memória (OOM)".
- `Status == exited && ExitCode != 0` → incidente "saiu com código X".
- Linhas de log batendo (case-insensitive) em:
  `disconnect`, `connection closed`, `logout`, `out of memory`, `fatal`,
  `econnrefused`, `econnreset`, `panic`, `rate limit`, `429`.
  Timestamp extraído do prefixo RFC3339 da linha.

### Frontend (React + Vite + TS + Tailwind)

- `pages/EnvironmentLogsPage.tsx` em `/ambiente-logs`.
  - Topo: cartões de saúde por container (status, restarts, OOM, "no ar desde HH:MM").
  - Meio: lista de **Incidentes** (hora + container + ocorrido).
  - Base: seletor de container + **tail de log** com timestamp, filtro de texto e
    destaque de linhas de erro.
  - Toggle **Auto refresh** (react-query `refetchInterval` 5s).
- `types/api.ts` — tipos novos.
- Rota em `App.tsx` + item no `Sidebar.tsx` (ícone `Activity`).

Segue os componentes existentes: `PageHeader`, `Card`, `StatCard`, `DataTable`,
cliente `api` (axios) e `@tanstack/react-query`.

## Segurança

- Todos os endpoints sob `[Authorize]` (admin logado), igual ao resto da API.
- Socket montado `:ro` no EasyPanel → painel só lê.
- Serviço só executa requisições GET de leitura à Docker API; nenhuma escrita.
- Mudança no EasyPanel é **aditiva** e só no service do painel-backend → demais
  containers (Evolution/n8n/Postgres) não são tocados nem reiniciam.

## Mudança de infra (feita pelo usuário no EasyPanel)

No service **painel-controlador / backend**:
1. Volume mount: `/var/run/docker.sock:/var/run/docker.sock:ro`.
2. Env var `MONITORED_CONTAINERS` (default acima).

## Testes

- Build do backend (`dotnet build`) e do frontend (`vite build`/`tsc`) sem erros.
- Teste manual: com socket disponível, overview retorna os 4 containers;
  sem socket, retorna `available=false` sem quebrar.
- Demux de log validado contra container sem TTY (postgres) e com saída real.
