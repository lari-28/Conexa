# CONEXA — MVP

Aplicação web responsiva (mobile-first) para networking em eventos: perfis, diretório de pessoas e empresas, fórum, moderação e administração.

**Stack:** React + TypeScript + Vite · ASP.NET Core Web API (.NET 10) · EF Core + Npgsql · PostgreSQL · ASP.NET Core Identity + JWT.

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (para .NET 8, troque `TargetFramework` e as versões dos pacotes no `.csproj`)
- Node.js 20.19+ (ou 22+)
- Docker (para o PostgreSQL) — ou um PostgreSQL local equivalente

## Executando localmente

```bash
# 1) Preparação (banco, restore, migration inicial, npm install)
./scripts/setup.sh            # Windows: .\scripts\setup.ps1

# 2) API  (terminal 1) — aplica migrations e seed automaticamente em Development
cd backend/Conexa.Api
dotnet run                    # http://localhost:5000  ·  Swagger em /swagger

# 3) Web  (terminal 2)
cd frontend/conexa-web
npm run dev                   # http://localhost:5173
```

Passo a passo manual equivalente ao `setup`:

```bash
docker compose -f infra/docker-compose.yml up -d db
dotnet tool restore
cd backend/Conexa.Api
dotnet ef migrations add InitialCreate    # apenas na primeira vez (gera a pasta Migrations/)
dotnet ef database update                 # opcional: em Development o `dotnet run` já migra
dotnet run
```

> **Por que a migration inicial é gerada por você?** As migrations dependem do SDK instalado; o repositório traz o modelo, e o comando acima gera o schema correspondente. Depois de gerada, **versione a pasta `Migrations/`**.

## Contas de demonstração (somente ambiente local)

Senha de todas: `Conexa@123`

| Conta | Papel |
| --- | --- |
| `ana@conexa.demo` | Participante (e-mail visível) |
| `bruno@conexa.demo` | Participante (contatos ocultos) |
| `carla@conexa.demo` | Expositora — Nimbus Cloud |
| `organizador@conexa.demo` | Organizadora do CONEXA Summit |
| `admin@conexa.demo` | Administrador da plataforma |

O seed é idempotente (`Seed:DemoData`) e cria o evento **CONEXA Summit**, empresas, publicações, comentários e reações. **Troque/remova essas credenciais antes de publicar.** Na tela de login (modo dev) há atalhos para preencher as contas.

Roteiro de demonstração: [`docs/roteiro-demonstracao.md`](docs/roteiro-demonstracao.md).

## Acesso por outro computador (mesma rede)

1. API escutando na rede: `dotnet run --launch-profile lan` (usa `http://0.0.0.0:5000`).
2. Frontend já escuta em `0.0.0.0` (`vite.config.ts`).
3. Libere as portas **5173** e **5000** no firewall do computador servidor.
4. No outro computador/celular, abra `http://<IP-do-servidor>:5173`. O frontend usa automaticamente `http://<mesmo-host>:5000` como API (ou defina `VITE_API_URL`).
5. Em Development o CORS aceita qualquer origem. Cada pessoa entra com a própria conta; ambos usam o mesmo banco. Evite dados pessoais reais em LAN/demonstração.

## Configuração e segredos

- `appsettings.Development.json` contém **apenas** valores locais de desenvolvimento (banco do docker-compose e chave JWT de teste).
- Overrides locais não versionados: `appsettings.Local.json` ou `dotnet user-secrets`.
- Em produção, use variáveis de ambiente: `ConnectionStrings__Default`, `Jwt__Key` (≥ 32 bytes), `Cors__AllowedOrigins__0`, `Seed__AdminEmail`/`Seed__AdminPassword`; mantenha `Seed__DemoData=false` e HTTPS.
- Cadastro: `Registration:Open` e `Registration:InviteCode` controlam abertura e convite.

## Estrutura

```
backend/Conexa.Api   Controllers · Contracts · Data · Domain · Services · Authorization · Seed
frontend/conexa-web  components · features · pages · routes · services · hooks · types · utils
infra/               docker-compose (PostgreSQL)
docs/                requisitos, arquitetura, modelo de dados, API, roteiro
```

## Status por etapa (plano da Estrutura Técnica)

| Etapa | Escopo | Situação |
| --- | --- | --- |
| 0 — Setup | Repositório, API, Vite, PostgreSQL, README | ✅ |
| 1 — Base | Entidades, Identity, roles, seed, login | ✅ (migration a gerar — ver acima) |
| 2 — Evento/perfis | Evento ativo, vínculo, perfil, privacidade | ✅ |
| 3 — Networking | Lista, filtros, perfil público | ✅ |
| 4 — Empresas | Diretório e detalhes | ✅ (somente leitura) |
| 5 — Fórum | Posts, comentários, reações, exclusão | ✅ (edição de post só na API) |
| 6 — Organizador | Evento, membros, denúncias | ✅ API + telas básicas |
| 7 — UI/testes | Estados, testes, ajustes | 🔶 estados de loading/erro/vazio feitos; **testes automatizados ainda não** |

Pendências conhecidas: CRUD de empresas/expositores pela interface, edição de publicação na UI, upload de foto (hoje é URL), testes automatizados, log de auditoria sem tela de consulta.

## Fora do escopo do MVP

Pulseira/NFC, Bluetooth, chat em tempo real, matching por IA, proximidade, pagamentos/ingressos/CRM e analytics avançado.
