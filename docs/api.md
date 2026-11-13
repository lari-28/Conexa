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
