# Prepara o ambiente local (Windows / PowerShell).
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

Write-Host '> Subindo PostgreSQL (docker compose)...'
docker compose -f infra/docker-compose.yml up -d db

Write-Host '> Backend: restore...'
dotnet tool restore
Push-Location backend/Conexa.Api
dotnet restore
if (-not (Get-ChildItem Migrations -Filter *.cs -ErrorAction SilentlyContinue)) {
  Write-Host '> Criando migration inicial...'
  dotnet ef migrations add InitialCreate
}
Pop-Location

Write-Host '> Frontend: npm install...'
Push-Location frontend/conexa-web
npm install
Pop-Location

Write-Host ''
Write-Host 'Pronto. Em dois terminais:'
Write-Host '  1) cd backend/Conexa.Api; dotnet run'
Write-Host '  2) cd frontend/conexa-web; npm run dev'
