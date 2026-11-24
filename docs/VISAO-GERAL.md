# CONEXA — Visão geral completa do sistema

*Posição em 23/11/2026. Branch `feature/fase1-reposicionamento` (23 commits à frente da `main`, nada enviado ao GitHub). Este documento reúne o que existe, como funcionam os perfis e a taxonomia, o que foi verificado, o que não existe e o que vem a seguir.*

---

## 1. Onde estamos, em um minuto

- **O que é:** uma plataforma que conecta **organizadores de feiras**, **expositores** e **visitantes** por interesses em comum, recomenda estandes aos visitantes, aponta leads aos expositores e gera análises do evento.
- **O que está pronto e funcionando:** cadastro por perfil, taxonomia de temas por evento, perfil do visitante e do estande, **recomendações com explicação (sem IA generativa)**, leads com consentimento, convites por link, importação de expositores por planilha, check-in por QR, avisos dentro do app, painel do organizador e exportação em CSV.
- **O que NÃO existe:** nenhuma IA generativa (LLM) está ligada ao sistema, não há envio de e-mail ou WhatsApp, os textos de LGPD não foram revisados por advogado, não há monetização definida, e nada foi testado com uma feira real.
- **Verificação:** 116 testes automatizados passam (banco em memória); 57 verificações ponta a ponta passaram contra PostgreSQL real; no navegador só foram vistas 2 telas. Detalhes na seção 10.
- **Maior risco hoje:** a qualidade do matching só foi medida com dados fabricados por IA (mesmo autor). A vantagem sobre uma busca simples por palavra é real, mas modesta (+6 pontos), e o sinal de **tema** ainda não foi medido com dados.
- **O que depende de você:** feira-piloto ou catálogo real, chave de API de IA (a que foi colada no chat deve ser revogada), parecer jurídico, provedor de e-mail e decisão de monetização (seção 12).

---

## 2. A ideia e as decisões de produto

**Ideia original:** unir donos de feiras, expositores e visitantes para facilitar o networking por interesses em comum. A feira cadastra seus temas e ideias; o expositor cadastra o estande, o que oferece e quem quer atender; o visitante cadastra o que procura. O sistema cruza isso, avisa cada lado de quem combina com ele e, ao final, gera análises do evento.

**Decisões já tomadas**

| Tema | Decisão |
| --- | --- |
| Quem se cadastra primeiro | O organizador macro do evento. Organizador não tem cadastro público. |
| Momento do matching | Antes do evento (QR no evento entra como evolução, já implementado como check-in). |
| Canal de aviso | Aviso dentro do app. E-mail depois. WhatsApp ainda mais depois. |
| IA no matching | O motor atual **não usa LLM**. A IA entra depois, por cima, para sugerir temas, explicar e resumir. |
| Visual | Tema claro: índigo `#312E81` (estrutura), coral `#FF5A36` (ação), teal `#0D9488` (afinidade/IA), gradiente só em blocos de IA, fundo `#F8FAFC`. |
| Taxonomia | Por evento, em dois níveis, com aprovação do organizador em lote. |
| Privacidade | Consentimentos separados por finalidade, nada presumido. |

**Em aberto:** monetização, provedor de IA, feira-piloto, quem rotula os dados de avaliação, WhatsApp.

---

## 3. Os perfis (quem é quem no sistema)

Existem **dois níveis de papel**:

1. **Papel global da conta** (Identity): `Participant`, `Exhibitor`, `Organizer`, `PlatformAdmin`.
2. **Papel dentro de um evento** (vínculo `EventMembership`): `Participant`, `Exhibitor` ou `Organizer`, com status `Active`, `Disabled` ou `Removed`. Quase todas as regras de acesso usam este.

A autorização real é sempre do backend; a interface só esconde opções.

### 3.1 Visitante (`Participant`)

- **Como entra:** cadastro público em `/cadastro/visitante`, ou por link de convite.
- **Dados da conta (globais):** nome, foto (URL), empresa, cargo, área de atuação, bio, interesses em texto, "o que busco" e "o que ofereço". Contatos (e-mail, telefone) ficam **ocultos por padrão**.
- **Perfil no evento (`/evento/meu-perfil`):**
  - **Objetivo em texto livre** (até 1000 caracteres), o sinal mais forte do matching.
  - **Prazo para decidir:** só explorando, neste trimestre, neste mês, agora.
  - **Temas de interesse (opcional):** até 8, escolhidos entre os temas aprovados do evento. Pode sugerir um tema novo.
  - **Três consentimentos, separados:** (1) usar o perfil para recomendar estandes; (2) permitir que expositores com afinidade o vejam e contatem; (3) enviar o objetivo, sem nome nem contato, a um serviço externo de IA. O 2 e o 3 só valem com o 1. Revogar o 1 revoga os outros dois. Conceder de novo preserva a data original.
- **O que faz:** vê estandes e pessoas do evento, recebe "Estandes para você" com o motivo de cada recomendação, responde "isso foi útil?", faz check-in por QR, recebe avisos, usa a Comunidade (fórum).
- **O que não vê:** leads, painéis, dados de contato de quem não liberou.

### 3.2 Expositor (`Exhibitor`)

- **Como entra:** `/cadastro/expositor`, convite de expositor, ou vinculado pela importação de planilha (se já tinha conta).
- **Estande (`/evento/meu-estande`):** um por expositor por evento. Campos: nome, segmento, descrição, produtos e serviços, localização, site, **cliente ideal** e **até 3 temas** aprovados. O expositor vira **representante** da empresa (`CompanyMembership`).
- **O que faz:** edita o estande; imprime o **QR de check-in**; vê os **leads** (visitantes com afinidade que consentiram matching e compartilhamento); vê o relatório do estande (check-ins, recomendações, "foi útil?"); exporta os leads em CSV; recebe aviso de lead com afinidade alta.
- **O que não vê:** visitantes que não consentiram o compartilhamento; painel do evento; estandes de outros (leads e relatório são só do próprio).

### 3.3 Organizador (`Organizer`)

- **Como entra:** **sem cadastro público.** É designado por um administrador (rota "adicionar organizador"). A página `/cadastro/feira` leva ao contato comercial (`VITE_CONTACT_EMAIL`, ainda não definido).
- **O que faz no evento:** edita dados do evento; gerencia membros; modera o fórum e denúncias; **define a taxonomia** (biblioteca, temas, fila de aprovação); gera **convites**; **importa expositores** por CSV; cria estandes; vê o **painel do evento**; exporta CSV de estandes e temas; vê leads e relatório de qualquer estande.
- **Limite:** só designa organizadores o administrador; um organizador não promove outro.

### 3.4 Administrador (`PlatformAdmin`)

Papel global. Cria eventos, designa organizadores, suspende contas. Tem acesso de organizador em **todos** os eventos (as políticas o tratam como membro e organizador global).

### 3.5 Como a pessoa entra no evento

1. Cadastro público sem convite: entra automaticamente no **evento ativo mais próximo** (comportamento original do protótipo).
2. Cadastro por **convite por link** (`/convite/{código}`): entra no evento **do convite** e com o **papel do convite** (visitante ou expositor). O código tem 128 bits aleatórios, pode ter limite de usos e validade, e pode ser cancelado. O uso é consumido de forma atômica, sem corrida no limite.
3. O organizador também pode vincular alguém já cadastrado pelo e-mail.

### 3.6 Contas de demonstração (só ambiente local)

Senha de todas: `Conexa@123`. `ana@`, `bruno@` (visitantes), `carla@` (expositora da Nimbus Cloud), `organizador@`, `admin@`, mais `diego@`, `eva@` e `felipe@` (visitantes da demonstração de matching), todas `@conexa.demo`. O evento fictício é o "CONEXA Summit".

---

## 4. A taxonomia (o sistema de temas)

### 4.1 Por que existe

Teste com um catálogo fantasia mostrou que a **categoria que o expositor escreve não serve**: 40 estandes tinham 40 categorias diferentes. Também que "IA", "inteligência artificial" e "machine learning" nunca se encontram em texto livre, e que a mesma palavra pode ter sentidos diferentes ("apontamento" em ERP e em IoT). A taxonomia é a camada que faz as pessoas se encontrarem.

### 4.2 Como é

- **Por evento:** cada feira tem a sua.
- **Dois níveis:** **macro-área** (sem pai) e **tema** (filho de uma macro-área). Não há terceiro nível. Uma macro-área sem temas pode ser usada como tema.
- **Cada tema tem:** nome (até 100 caracteres), descrição curta (até 300), **sinônimos** (até 15, cada um até 60), status e, se veio da biblioteca, uma chave de origem.
- **Status:** `Suggested` (sugerido), `Approved` (aprovado), `Rejected` (rejeitado/arquivado).
- **Limite:** 500 temas por evento. Nomes duplicados no mesmo nível são recusados comparando sem acento e sem diferença de maiúsculas.

### 4.3 Ciclo de vida de um tema

1. **Nasce** de três formas: o organizador cria (já **aprovado**); qualquer membro **sugere** (fica `Suggested`); o organizador importa a **biblioteca-base** (aprovado); ou a **importação de planilha** traz um tema desconhecido (vira sugestão).
2. **Sugestão inteligente de duplicata:** se alguém sugere um tema cujo nome **ou sinônimo** já existe, o sistema devolve o existente em vez de duplicar (e a tela já o seleciona).
3. **Fila de aprovação em lote** (`/organizador/taxonomia`): as sugestões pendentes aparecem **agrupadas por nome parecido** (sem acento e caixa). Se o nome bate com um tema aprovado, o grupo mostra "Parece o tema X" e oferece **Fundir**. O organizador marca vários e escolhe:
   - **Aprovar** (exige que a macro-área do tema esteja aprovada ou seja aprovada junto);
   - **Rejeitar**;
   - **Fundir** em um tema aprovado: os vínculos de estandes e visitantes passam para o destino sem duplicar, e o nome (e sinônimos) da sugestão viram **sinônimos** do destino. Não funde macro-área que tem temas.
4. **Arquivar** (botão da lista): marca como `Rejected`, preservando os vínculos existentes. Não arquiva macro-área que ainda tem temas ativos.

### 4.4 Biblioteca-base por setor

Ponto de partida, não exaustiva. Importação **idempotente** (não duplica o que já existe):

| Setor | Macro-áreas | Temas |
| --- | --- | --- |
| Indústria e tecnologia industrial | Gestão e sistemas; Automação e IoT; Máquinas e insumos | ERP; controle de produção (MES); IoT industrial; robótica; usinagem; EPIs |
| Tecnologia e negócios digitais | Dados e IA; Infraestrutura e segurança | IA; análise de dados e BI; nuvem; cibersegurança |
| Varejo e serviços | Vendas e pagamentos | ponto de venda e e-commerce; meios de pagamento |

São 6 macro-áreas e 12 temas, com descrição e sinônimos. **O conteúdo foi escrito por mim e precisa de revisão por quem conhece os setores.** O seed de demonstração acrescenta duas áreas próprias (agronegócio, energia).

### 4.5 Onde a taxonomia é usada

| Uso | Regra |
| --- | --- |
| Estande | Escolhe **até 3** temas aprovados do evento. Só o organizador ou um representante altera. |
| Visitante | Escolhe **até 8** temas aprovados, **opcional**. |
| Matching | Sinal de **tema**: o visitante ganha 1 por tema que o estande tem e **0,5** por tema da **mesma macro-área** (conexão entre áreas próximas), média sobre os temas escolhidos. Só temas **aprovados** contam. |
| Texto do estande | Nomes e sinônimos dos temas do estande entram no texto usado na comparação com o objetivo livre. Assim o sinônimo ajuda mesmo quando o visitante não escolheu chips. |
| Painel | **Demanda** (visitantes que escolheram o tema **ou o mencionaram** no objetivo, por nome/sinônimo, palavra inteira, sem acento) contra **oferta** (estandes com o tema). Demanda sem nenhuma oferta aparece como **lacuna**. |
| Importação CSV | A coluna `temas` casa por nome ou sinônimo; desconhecidos viram sugestões. |

### 4.5.1 Limites atuais da taxonomia (honestos)

- Na tela do organizador é possível **criar, arquivar, importar e aprovar/rejeitar/fundir pela fila**. **Não há tela para editar** um tema existente (a API de edição existe) nem para fundir dois temas aprovados à escolha (a API aceita; a tela só oferece a fusão sugerida).
- **Nenhuma IA propõe temas ainda.** O plano é a IA ler o catálogo ou o site e propor temas, que caem na mesma fila de aprovação.
- O catálogo de teste de 150 estandes **não tem taxonomia aplicada**, então o sinal de tema nunca foi medido com dados.
- Em teste anterior, cerca de **30% dos estandes não couberam em nenhuma biblioteca-base**. Por isso a fila de aprovação é uma tela central, não exceção.

---

## 5. O matching (as recomendações)

**Princípio:** o ranking vem de um motor determinístico, **sem LLM**, e cada resultado vem com explicação por regras. Todo o resto (IA) fica por cima.

**Três sinais combinados por soma ponderada**

| Sinal | Como calcula | Peso inicial |
| --- | --- | --- |
| Tema | Fração dos temas escolhidos pelo visitante cobertos pelo estande (crédito parcial para a mesma macro-área) | 0,2 |
| Texto | Similaridade (TF-IDF de n-gramas de caracteres, 3 a 5) entre o objetivo do visitante e o texto do estande (nome, segmento, descrição, produtos, nomes e sinônimos dos temas). Ignora acento e caixa. | 0,7 |
| Público-alvo | Similaridade entre cargo + setor do visitante e o campo "cliente ideal" do estande | 0,1 |

- **Sinal ausente sai da soma** e os pesos restantes são renormalizados. **Público-alvo sozinho não sustenta recomendação** (evita recomendar só por cargo).
- **Faixas:** alta ≥ 0,45; média ≥ 0,25; baixa ≥ 0,12. Abaixo disso é "sem match" e **não aparece**. A tela mostra faixa (sem percentual) e avisa que a recomendação pode errar.
- **Explicação por regras:** temas em comum, palavras em comum com o objetivo (com a grafia do visitante) e semelhança de público-alvo.
- **Pesos e limiares são hipótese**, vindos de um teste sintético. Não são calibração.
- **Privacidade:** só calcula para quem consentiu o uso do perfil. **Leads** exigem consentimento de matching **e** de compartilhamento, usuário ativo e vínculo ativo; o contato só aparece se a pessoa o liberou nas configurações de privacidade (independente do compartilhamento).
- **Persistência:** os scores ficam na tabela `MatchScore` (decomposição, faixa, motivos). São recalculados ao consultar e ao salvar o perfil.

---

## 6. Demais funcionalidades

| Funcionalidade | O que faz |
| --- | --- |
| **Home pública e entrada por perfil** | `/` com exemplo ilustrativo (marcado como fictício), três portas (visitante, expositor, organizador), "como funciona". `/cadastro/{visitante,expositor,feira}`. |
| **Convites por link** | Papel, limite de usos, validade, cancelamento; pré-visualização pública mostra só evento e papel. |
| **Importação de expositores (CSV)** | Aceita `;`, `,` ou tab, aspas e BOM; cabeçalhos com variações; até 1000 linhas; **prévia antes de gravar**; pula duplicados (por nome); temas desconhecidos viram sugestões; vincula o responsável pelo e-mail se já tem conta. |
| **Check-in por QR** | Cada estande tem um QR (gerado na primeira consulta). Repetir em até 30 min não duplica. QR imprimível na página do expositor. |
| **"Isso foi útil?"** | Uma resposta por visitante e estande, só para estandes que foram recomendados a ele. Alimenta o reajuste de pesos com dados reais. |
| **Avisos no app** | Sino com contador e `/avisos`. Ao visitante: "Estandes para você" (uma vez). Ao representante: lead de afinidade alta, **só se o visitante consentiu compartilhar**. Ao revogar o compartilhamento, os avisos com o nome dele são apagados. |
| **Painel do evento** | Totais, check-ins, "foi útil?", **lacunas**, demanda × oferta por tema, **concentração de exposição** (Gini e fatia dos 20% mais recomendados), perfis por área de atuação, termos mais pedidos. Avisa quando há menos de 10 visitantes. |
| **Relatório do estande** | Check-ins, recomendações, afinidade alta, feedback, leads disponíveis. |
| **Exportação** | CSV de estandes, temas e leads (separador `;`, UTF-8 com BOM, proteção contra injeção de fórmula). PDF pela impressão do navegador. |
| **Já existiam no protótipo** | Login com papéis, diretório de pessoas e empresas com privacidade, fórum (agora "Comunidade" no menu do usuário), moderação e denúncias, administração, auditoria. |

---

## 7. Modelo de dados

| Tabela | Para quê |
| --- | --- |
| Usuários e papéis (Identity), `UserProfile`, `ContactPrivacy` | Conta, perfil global, privacidade de contato |
| `Event`, `EventMembership` | Evento (agora com `MainThemes`) e vínculo com papel e status |
| `Company`, `CompanyMembership` | Estande (agora com `IdealCustomer` e `CheckInCode`) e representantes |
| `InterestTag` | Taxonomia: macro-áreas e temas por evento |
| `CompanyTag` | Até 3 temas por estande |
| `ParticipantEventProfile`, `ParticipantTag` | Perfil do visitante no evento: objetivo, prazo, consentimentos por finalidade, temas |
| `MatchScore` | Afinidade visitante × estande com decomposição e motivos |
| `EventInvite` | Convites por link |
| `Interaction` | Check-in e feedback |
| `Notification` | Avisos no app (com chave anti-repetição) |
| `Post`, `Comment`, `Reaction`, `Report`, `AuditLog` | Fórum, moderação e auditoria |

**5 migrations:** `InitialCreate` (do protótipo) e quatro novas: `Etapa2Taxonomia`, `Etapa3Matching`, `Etapa4ConvitesImportacao`, `Etapa5CheckinAvisos`. **Nenhuma foi aplicada ao seu banco.**

---

## 8. A API e as telas

**API:** 16 controllers e cerca de 70 endpoints. A lista completa e atualizada está em [`docs/api.md`](api.md). Blocos novos: taxonomia, perfil do visitante, perfil do estande, matching, convites, importação, check-in/feedback, avisos, painéis e exportação.

**Telas (rotas do frontend)**

| Público | Logado: todos | Visitante | Expositor | Organizador/Admin |
| --- | --- | --- | --- | --- |
| `/`, `/login`, `/cadastro`, `/cadastro/:tipo`, `/convite/:code` | `/evento`, `/empresas`, `/empresas/:id`, `/participantes`, `/perfil`, `/forum`, `/avisos`, `/checkin/:code` | `/evento/meu-perfil` | `/evento/meu-estande` | `/organizador/{painel, taxonomia, convites, importacao, evento, participantes, moderacao}`, `/admin/{eventos, usuarios}` |

Tamanho aproximado do código: backend ~4.000 linhas (sem migrations), frontend ~3.200, testes ~1.600.

---

## 9. Linha do tempo do que foi feito

1. **Análise e decisão de direção:** documento de direção, escolha do tema claro, roteiro por etapas.
2. **Fase 1 (frontend):** tema claro, home pública, entrada por perfil, página do evento por perfil, nova navegação.
3. **Testes do backend:** de zero para 19 testes (consentimento de contato, vínculo ao evento, normalização).
4. **Etapa 2 (cadastros estruturados):** modelo e API de taxonomia, perfil do visitante e do estande; telas do wizard do visitante, do estande e da taxonomia do organizador.
5. **Etapa 3 (matching):** motor, `MatchScore`, API e telas "Estandes para você" e "Leads para sua empresa".
6. **Cadastro em massa:** importação por CSV e convites por link.
7. **Demonstração:** seed aditivo com taxonomia, estandes e visitantes com consentimentos variados.
8. **Etapa 4/5 (engajamento):** check-in por QR, feedback, avisos no app.
9. **Etapa 6 (análise):** painel, relatório do estande, exportação CSV.
10. **Dados de teste:** prompt e script para gerar catálogo no Gemini, recebimento do catálogo (150) e dos visitantes (60), ferramenta de avaliação `Conexa.Eval` e primeira medição.

---

## 10. O que foi verificado e como

| Verificação | Resultado | Limite |
| --- | --- | --- |
| **116 testes xUnit** do backend | Passam | Usam banco **em memória**: não validam SQL do PostgreSQL |
| **`scripts/smoke-e2e.mjs`** (57 checagens) contra a API real em PostgreSQL, num banco descartável | Todas passaram: matching, leads e privacidade, convites, importação, avisos, check-in, feedback, painel, CSV e permissões | Exige API local com dados de demonstração |
| **Build do frontend** | Passa | **Não há testes automatizados de frontend** |
| **Navegador com API real** | Vistas: página do evento do visitante (celular) e painel do organizador | **As demais telas só passaram no build** |
| **Script do Gemini** | Testado contra servidor falso (retentativa, limpeza de markdown, mescla) | **Não rodou de verdade com o Gemini** pela chave não usada; o modelo padrão é `gemini-3.8-flash` |

---

## 11. Primeira medição do matching com dados

Dados: 150 estandes e 60 visitantes gerados pelo Gemini (`dados-teste/`, congelados por hash). Mede **texto + público-alvo**; **tema não foi medido** (o catálogo não tem taxonomia). Relatório completo: [`docs/avaliacao-matching-gemini.md`](avaliacao-matching-gemini.md).

- **Teste oficial (20 visitantes específicos):** estande relevante no top-3 em 100% dos casos; nDCG@5 de 79%, contra 73% da busca por palavra: **+6,4 pontos** (IC 95%: +3,5 a +10,0). Aleatório: 3%.
- **Cuidado:** os visitantes reaproveitam o vocabulário do catálogo e tudo vem do mesmo autor. Os números absolutos são otimistas.
- **Objetivo vago (11 visitantes, rótulo discutível):** só 19%. Uma variante que dá mais peso ao público-alvo quando o texto é fraco chegou a 33%, mas não foi adotada.
- **Limiares:** com o corte atual (baixa ≥ 0,12), 4 de 9 visitantes **fora do escopo** ainda receberiam recomendação, e a faixa "baixa" acerta só 45%; a "média" acertou 100%. Não alterei, porque o vocabulário compartilhado infla os scores.
- **Problemas nos dados:** 20 códigos de estande repetidos (o lote 1 vazou para os pavilhões A, B e C).

---

## 12. O que NÃO existe e do que depende

| Item | Estado | O que falta |
| --- | --- | --- |
| **IA generativa (LLM)** | Não existe. Há apenas o consentimento "IA externa" no modelo. | **Chave de API** (revogue a que foi colada no chat e gere outra, guardada com `dotnet user-secrets`) e **parecer jurídico**. |
| **E-mail e WhatsApp** | Não enviam. Só aviso no app. | **Provedor de envio** e texto de consentimento próprio, revisado. |
| **LGPD** | Consentimentos por finalidade existem, e revogar apaga avisos com o nome. Os **textos não foram revisados.** Não há política de privacidade, retenção, nem fluxo de pedidos de titulares. | **Parecer jurídico** (você vai trabalhar nisso). |
| **Calibração do matching** | Pesos e limiares são hipótese. | **Feira-piloto/catálogo real**, ou visitantes escritos por outras pessoas. |
| **Monetização** | Indefinida. | **Decisão de negócio.** Afeta a página do organizador. |
| **Piloto e produção** | Não feitos. | Feira-piloto, hospedagem, HTTPS, backup, monitoramento. |
| **Edição de tema e fusão livre na tela** | Só API. | Pequeno trabalho de tela. |
| **Outros limites** | Fonte IBM Plex Sans não carregada localmente; rota `/empresas` ainda não virou `/estandes`; sem XLSX/PDF gerado no servidor; foto de perfil só por URL; sem testes de frontend; não encontrei fluxo de recuperação de senha. | Trabalho de produto/engenharia. |

---

## 13. Próximos passos (ordem recomendada)

**Agora, sem depender de ninguém**
1. **Aplicar as migrations e ver tudo rodando:** pare a API, rode `dotnet ef database update` e inicie de novo (a API em execução ainda é o build antigo).
2. **Percorrer as telas novas no navegador** com a API real (visitante, expositor, organizador). Hoje só 2 foram vistas.
3. **Revogar a chave do Gemini** que foi colada no chat e gerar outra.
4. **Decidir o que fazer com o PR:** a branch tem 23 commits e ainda não foi enviada. Abrir pull request quando você quiser.

**Com a sua nova chave (primeiro uso de IA, sem dados de pessoas)**
5. **Sugerir temas por IA** a partir do catálogo de 150 estandes, entrando na fila de aprovação existente.
6. **Aplicar a taxonomia ao catálogo e medir o sinal de tema** (o que falta na medição).
7. Rodar o **gerador de catálogo no Gemini de verdade** (ou aproveitar o catálogo já recebido).

**Depois**
8. **Dados independentes:** visitantes escritos por outras pessoas (ou reais) para ajustar pesos e limiares com honestidade.
9. **Tratar o objetivo vago como problema de produto** (pedir cargo e setor com mais ênfase no cadastro).
10. **Explicações e relatório final por IA** (relatório só com dados agregados; explicações só depois do parecer de LGPD).
11. **E-mail** (provedor + consentimento revisado).
12. **Piloto** em uma feira pequena, LGPD, observabilidade, hospedagem.

**Pequenos acabamentos:** tela de edição de tema, renomear `/empresas`, fonte local, testes de frontend, recuperação de senha, upload de foto.

---

## 14. Como rodar

```powershell
# banco (PostgreSQL em Docker)
docker compose -f infra/docker-compose.yml up -d db

# backend (aplica migrations e o seed de demonstração em Development)
cd backend/Conexa.Api
dotnet ef database update
dotnet run                      # http://localhost:5000  ·  Swagger em /swagger

# frontend
cd frontend/conexa-web
npm install
npm run dev                     # http://localhost:5173

# testes do backend
cd backend
dotnet test Conexa.Api.Tests

# verificação ponta a ponta (use banco descartável e API com Seed:DemoData=true)
$env:CONEXA_API="http://127.0.0.1:5000"; node scripts/smoke-e2e.mjs

# avaliação do matching com os dados de teste
dotnet run --project backend/Conexa.Eval -- dados-teste/catalogo/catalogo_150_gemini.csv dados-teste/matching/visitantes_60_gemini.json docs/avaliacao-matching-gemini.md
```

Dica: se a API estiver rodando, o build trava os arquivos. Use `--configuration EfTemp` nos comandos de build e teste.

**Documentos relacionados:** [`direcao-do-projeto.md`](direcao-do-projeto.md) (decisões e roteiro), [`api.md`](api.md) (endpoints), [`prompt-catalogo-teste.md`](prompt-catalogo-teste.md) (geração de dados), [`avaliacao-matching-gemini.md`](avaliacao-matching-gemini.md) (medição), [`../dados-teste/LEIA-ME.md`](../dados-teste/LEIA-ME.md) (dados congelados).
