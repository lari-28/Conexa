# Arquitetura

- **Frontend** (`frontend/conexa-web`): SPA React + TypeScript (Vite), mobile-first. Fala com a API por REST/JSON. O token JWT fica em `localStorage`; `services/api/client.ts` injeta o `Authorization` e trata erros (`ApiError`).
- **Backend** (`backend/Conexa.Api`): ASP.NET Core Web API. Controllers finos + EF Core direto no `DbContext` (sem camada de repositórios, que era opcional).
- **Banco**: PostgreSQL via Npgsql. Enums como texto; interesses como `text[]` com índice GIN.

## Autenticação e autorização

1. Identity gerencia contas/senhas (hash) e papéis globais: `Participant`, `Exhibitor`, `Organizer`, `PlatformAdmin`.
2. Login devolve um JWT (claims: id, e-mail, nome, papéis). Logout é descartar o token (stateless).
3. A cada requisição, `OnTokenValidated` confere `IsActive`: contas suspensas perdem acesso imediatamente.
4. Políticas por evento, lendo `{eventId}` da rota:
   - `EventMember`: vínculo `Active` ao evento (ou `PlatformAdmin`);
   - `EventOrganizer`: vínculo `Active` com `RoleInEvent = Organizer` (ou `PlatformAdmin`).
5. Endpoints sem `{eventId}` (posts, comentários, denúncias) validam o vínculo via `IEventAccessService` e respondem **404** quando o usuário não tem acesso, sem revelar a existência do recurso.
6. A UI esconde opções por papel, mas **a autorização real é sempre do backend** (RNF05).

## Privacidade

DTOs de lista nunca incluem contato. O detalhe só inclui e-mail/telefone se o titular marcou a visibilidade **e** consentiu (`ConsentGivenAt`). Ver `Mapping.AuthorizedContacts`.

## Proteções básicas

Limite de publicações (3/min) e comentários (6/min) por usuário, tamanhos máximos validados, bloqueio de login após 5 falhas (10 min), `[Url]` na foto (somente http/https), registro de ações de moderação/admin em `AuditLog`.
