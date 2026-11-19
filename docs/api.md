# API REST

Base: `http://localhost:5000`. Swagger (Development): `/swagger`. Autenticação: `Authorization: Bearer <token>`.
Enums trafegam como texto. Listas paginadas retornam `{ items, page, pageSize, totalCount, totalPages }`.
Erros seguem `ProblemDetails` (`400` validação, `401`, `403`, `404`, `409`, `429`).

| Método | Rota | Acesso |
| --- | --- | --- |
| POST | `/api/auth/register` | público (se `Registration:Open`) |
| POST | `/api/auth/login` | público |
| POST | `/api/auth/logout` | autenticado |
| GET | `/api/auth/me` | autenticado |
| GET / PUT | `/api/profile/me` | autenticado |
| PUT | `/api/profile/me/privacy` | autenticado |
| GET | `/api/users/{id}/profile` | quem compartilha evento com o usuário |
| GET | `/api/events/mine` ★ | autenticado |
| GET | `/api/events/{eventId}` ★ | membro |
| GET | `/api/events/{eventId}/participants?q=&area=&interest=&page=&pageSize=` | membro |
| GET | `/api/events/{eventId}/participants/filters` ★ | membro |
| GET | `/api/events/{eventId}/participants/{userId}` | membro |
| GET | `/api/events/{eventId}/companies?q=&page=` · `/{companyId}` | membro |
| GET / POST | `/api/events/{eventId}/posts?page=&category=` | membro |
| PUT / DELETE | `/api/posts/{postId}` | autor (DELETE: autor ou organizador) |
| GET / POST | `/api/posts/{postId}/comments` | membro |
| DELETE | `/api/comments/{commentId}` | autor ou organizador |
| POST / DELETE | `/api/posts/{postId}/reactions` | membro |
| POST | `/api/reports` | membro |
| PUT | `/api/organizer/events/{eventId}` ★ | organizador do evento / admin |
| GET / POST | `/api/organizer/events/{eventId}/members` | organizador / admin |
| PATCH | `/api/organizer/events/{eventId}/members/{userId}` | organizador / admin |
| GET | `/api/organizer/events/{eventId}/reports?status=` | organizador / admin |
| PATCH | `/api/organizer/reports/{reportId}` | organizador do evento da denúncia / admin |
| GET / POST | `/api/admin/events` | admin |
| GET | `/api/admin/users?q=&page=` | admin |
| PATCH | `/api/admin/users/{id}` ★ | admin |
| POST | `/api/admin/events/{eventId}/organizers` ★ | admin |

★ = acrescentado à proposta inicial do documento técnico.

## Taxonomia por evento (Etapa 2)

Dois níveis: macro-área e tema. Sinônimos e descrição servem para distinguir termos ambíguos. Um estande escolhe até 3 temas aprovados.

| Método | Rota | Acesso |
| --- | --- | --- |
| GET | `/api/events/{eventId}/taxonomy?status=` | membro (só aprovados); organizador filtra por status |
| GET | `/api/events/{eventId}/taxonomy/library` | membro |
| POST | `/api/events/{eventId}/taxonomy` | organizador / admin |
| PUT / DELETE | `/api/events/{eventId}/taxonomy/{tagId}` | organizador / admin (DELETE arquiva) |
| POST | `/api/events/{eventId}/taxonomy/suggestions` | membro (devolve o tema existente se já houver) |
| GET | `/api/events/{eventId}/taxonomy/suggestions` | organizador / admin (agrupado, indica fusão) |
| POST | `/api/events/{eventId}/taxonomy/review` | organizador / admin (`Approve`, `Reject`, `Merge` em lote) |
| POST | `/api/events/{eventId}/taxonomy/library/import` | organizador / admin (setor da biblioteca-base) |
| GET / PUT | `/api/events/{eventId}/companies/{companyId}/tags` | PUT: organizador ou representante da empresa |

## Perfil do visitante por evento (Etapa 2)

Perfil do próprio usuário dentro do evento. O objetivo em texto livre é o sinal mais forte para o matching; os temas (até 8, aprovados) são opcionais. Consentimentos separados por finalidade: `ConsentMatching`, `ConsentShareWithExhibitors`, `ConsentExternalAi`. Compartilhar com expositores e usar IA externa exigem o consentimento de matching; revogar o matching revoga os dois. Conceder de novo preserva a data original.

| Método | Rota | Acesso |
| --- | --- | --- |
| GET | `/api/events/{eventId}/my-profile` | membro (vazio e sem consentimentos se ainda não existir) |
| PUT | `/api/events/{eventId}/my-profile` | membro (cria ou atualiza o próprio perfil) |

`ReadyForMatching` = consentiu matching e informou objetivo (10+ caracteres) ou ao menos um tema.
