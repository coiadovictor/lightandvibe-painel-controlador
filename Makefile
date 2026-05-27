.PHONY: help install dev dev-front dev-back build build-front build-back test clean

help:
	@echo "Comandos disponíveis:"
	@echo "  make install     - Instala dependências de frontend e backend"
	@echo "  make dev         - Sobe frontend e backend via docker-compose (com hot-reload)"
	@echo "  make dev-front   - Roda apenas o frontend localmente (sem docker)"
	@echo "  make dev-back    - Roda apenas o backend localmente (sem docker)"
	@echo "  make build       - Builda frontend e backend"
	@echo "  make test        - Roda testes do backend"
	@echo "  make clean       - Limpa artefatos de build"

install:
	cd apps/frontend && npm install
	cd apps/backend && dotnet restore

dev:
	docker compose -f docker-compose.dev.yml up --build

dev-front:
	cd apps/frontend && npm run dev

dev-back:
	cd apps/backend/PainelControlador.Api && dotnet run

build: build-front build-back

build-front:
	cd apps/frontend && npm run build

build-back:
	cd apps/backend && dotnet publish -c Release

test:
	cd apps/backend && dotnet test

clean:
	rm -rf apps/frontend/dist apps/frontend/node_modules
	cd apps/backend && dotnet clean
