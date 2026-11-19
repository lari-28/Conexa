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

## Perfil do estande (Etapa 2)

| Método | Rota | Acesso |
| --- | --- | --- |
| GET | `/api/events/{eventId}/companies/mine` | membro (estandes em que sou representante) |
| POST | `/api/events/{eventId}/companies` | expositor ativo (vira representante; um estande por evento) ou organizador |
| PUT | `/api/events/{eventId}/companies/{companyId}` | organizador ou representante |

O detalhe (`GET .../companies/{companyId}`) passou a trazer `idealCustomer` e `tags`.

## Matching v1 (Etapa 3)

Motor híbrido sem LLM: tema (chips, com crédito parcial para a mesma macro-área) + texto livre (TF-IDF de n-gramas de caracteres) + público-alvo. Sinais ausentes saem da soma; público-alvo sozinho não sustenta recomendação. Cada resultado traz score, faixa (`High`/`Medium`/`Low`; abaixo disso é "sem match" e não aparece) e a explicação em linguagem natural. **Pesos e limiares são hipótese não calibrada** (ver `MatchOptions`).

| Método | Rota | Acesso |
| --- | --- | --- |
| GET | `/api/events/{eventId}/matches/booths?limit=` | membro; exige perfil pronto e consentimento de matching |
| GET | `/api/events/{eventId}/companies/{companyId}/leads?limit=` | organizador ou representante; só visitantes que consentiram matching **e** compartilhamento; contato segue a privacidade do perfil |

Os scores ficam em `MatchScores` (decomposição + motivos).

## Convites por link e importação de expositores (Etapa 2)

**Convites:** o organizador gera um link com papel (visitante ou expositor), limite de usos e validade opcionais. Quem se cadastra com o código (`inviteCode` no registro) entra no evento do convite com o papel definido; o uso é consumido de forma atômica. O código tem 128 bits aleatórios. A pré-visualização pública só revela nome do evento e papel.

| Método | Rota | Acesso |
| --- | --- | --- |
| GET | `/api/invites/{code}` | público |
| GET / POST | `/api/organizer/events/{eventId}/invites` | organizador / admin |
| DELETE | `/api/organizer/events/{eventId}/invites/{inviteId}` | organizador / admin (cancela) |

**Importação do catálogo (CSV):** aceita `;`, `,` ou tab, aspas e BOM; cabeçalhos com variações (nome/empresa, segmento, produtos, cliente ideal, estande, site, temas, e-mail). Excel: salve como CSV. Até 1000 linhas. Duplicados por nome (sem acento/caixa) são pulados; temas desconhecidos viram sugestões na fila de aprovação; o responsável é vinculado se já tiver conta.

| Método | Rota | Acesso |
| --- | --- | --- |
| POST | `/api/organizer/events/{eventId}/exhibitors/import` (`{ csv, commit }`) | organizador / admin (`commit=false` só valida) |

## Check-in por QR, feedback e avisos (Etapa 4)

Cada estande tem um código de QR (gerado na primeira consulta). O visitante lê o QR no estande e o check-in registra a visita (repetir em até 30 min não duplica). "Isso foi útil?" vale só para estandes que foram recomendados ao usuário, com no máximo uma resposta por estande. Os avisos ficam dentro do app; **e-mail e WhatsApp ainda não são enviados** (dependem de provedor e de consentimento próprio).

| Método | Rota | Acesso |
| --- | --- | --- |
| GET | `/api/events/{eventId}/companies/{companyId}/checkin-code` | organizador ou representante |
| POST | `/api/events/{eventId}/checkins` (`{ code }`) | membro |
| POST | `/api/events/{eventId}/matches/feedback` (`{ companyId, useful }`) | membro |
| GET | `/api/notifications?unreadOnly=&limit=` · `/count` | próprio usuário |
| POST | `/api/notifications/{id}/read` · `/read-all` | próprio usuário |

Avisos gerados ao salvar o perfil do visitante: ao próprio visitante ("Estandes para você", uma vez) e, **só se ele consentiu o compartilhamento**, ao representante de cada estande com afinidade alta. Ao revogar o compartilhamento, os avisos com o nome do visitante são apagados.

## Painéis e exportação (Etapa 6)

Só agregados: nenhuma lista de pessoas sai dos painéis. Consideram apenas visitantes que consentiram o matching. Exportações em CSV (separador `;`, UTF-8 com BOM, células que começam com `=`, `+`, `-` ou `@` são neutralizadas contra injeção de fórmula); abra no Excel. **XLSX, PDF e relatório com IA ainda não existem** (o PDF sai pela impressão do navegador).

| Método | Rota | Acesso |
| --- | --- | --- |
| GET | `/api/organizer/events/{eventId}/insights` | organizador / admin |
| GET | `/api/organizer/events/{eventId}/export/booths.csv` · `/themes.csv` | organizador / admin |
| GET | `/api/events/{eventId}/companies/{companyId}/report` | organizador ou representante |
| GET | `/api/events/{eventId}/companies/{companyId}/leads.csv` | organizador ou representante (mesmas regras de consentimento dos leads) |

O painel traz: totais (visitantes, consentimentos, check-ins, "foi útil?"), demanda x oferta por tema e as lacunas (demanda sem nenhum estande), concentração de exposição entre estandes (Gini e fatia dos 20% mais recomendados), perfis por área de atuação e termos mais pedidos nos objetivos. Com menos de 10 visitantes o painel avisa que os números são indicativos.
