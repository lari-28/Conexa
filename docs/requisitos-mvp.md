# Requisitos do MVP (resumo)

Fonte: *CONEXA — Especificação de Requisitos (MVP)*. Esta tabela mapeia cada requisito à implementação.

| ID | Requisito | Onde está |
| --- | --- | --- |
| RF01 | Cadastro (nome, e-mail, senha, tipo) | `POST /api/auth/register`; organizador/admin só por seed/painel |
| RF02 | Login/logout, páginas protegidas | `AuthController`, `ProtectedRoute` |
| RF03–RF04 | Perfil, interesses, busca/oferece | `ProfileController`, `/perfil` |
| RF05 | Privacidade de contato | `PUT /api/profile/me/privacy`, `Mapping.AuthorizedContacts` |
| RF06 | Vínculo com evento | `EventMembership` |
| RF07–RF09 | Lista, busca/filtros, detalhe de participantes | `ParticipantsController`, `/participantes` |
| RF10–RF11 | Diretório e detalhe de empresas | `CompaniesController`, `/empresas` |
| RF12–RF16 | Fórum | `ForumController`, `/forum` |
| RF17–RF18 | Evento e usuários (organizador) | `OrganizerController`, `/organizador/*` |
| RF19 | Papéis e permissões | Roles + políticas `EventMember`/`EventOrganizer` |
| RF20 | Denúncias e moderação | `ReportsController`, `OrganizerController` |
| RF21 | Painel administrativo | `AdminController`, `/admin/*` |

| Regra | Implementação |
| --- | --- |
| RN01 | `RequireUniqueEmail` + `FindByEmailAsync` no cadastro |
| RN02/RN06 | Política `EventMember` + `IEventAccessService`; perfis só entre membros do mesmo evento |
| RN03/RN07 | Papéis globais + `EventMembership.RoleInEvent`; admin global, organizador por evento |
| RN04/RN05 | Contato só sai com `ConsentGivenAt` e flag de visibilidade; editável a qualquer momento |
| RN08 | Usuário comum só exclui o próprio conteúdo; terceiros → denúncia |
| RN09 | Suspensão por `IsActive`/`MembershipStatus`; nada é apagado fisicamente |
