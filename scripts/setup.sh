#!/usr/bin/env bash
# Prepara o ambiente local: banco (Docker), restore do backend e migration inicial (se ainda não existir).
set -euo pipefail
cd "$(dirname "$0")/.."

echo "▶ Subindo PostgreSQL (docker compose)…"
docker compose -f infra/docker-compose.yml up -d db

echo "▶ Backend: restore…"
dotnet tool restore
(cd backend/Conexa.Api && dotnet restore)

if ! ls backend/Conexa.Api/Migrations/*.cs >/dev/null 2>&1; then
  echo "▶ Criando migration inicial…"
  (cd backend/Conexa.Api && dotnet ef migrations add InitialCreate)
fi

echo "▶ Frontend: npm install…"
(cd frontend/conexa-web && npm install)

echo
echo "Pronto. Em dois terminais:"
echo "  1) cd backend/Conexa.Api && dotnet run        (aplica migrations e seed automaticamente em Development)"
echo "  2) cd frontend/conexa-web && npm run dev"
