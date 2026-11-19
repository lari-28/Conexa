# CONEXA — Direção do projeto (decisão de 19/11/2026)

Base: handoff do Claude anterior + estado real do repositório.

## 1. Diagnóstico

- **O que existe de verdade no repo:** protótipo .NET 10 + React + PostgreSQL (login por papéis, evento, perfis com privacidade, diretório, fórum, moderação) e um tema escuro "Circuit Black Racing Chrome" (`index.css`, `.superdesign/`).
- **O que o handoff descreve mas NÃO está no repo:** a Fase 1 (`Conexa_fase1_overlay.zip`: home pública, entrada por perfil, tema claro índigo/coral) e as ferramentas de avaliação (`avaliacao_*.zip`). Não encontrei esses arquivos em Downloads/Documents/Desktop. Só o PDF e `catalogo_expositores_teste_conexa.xlsx` chegaram.
- **Conflito visual:** o commit "design system" aplicou um tema escuro vermelho/cromado; o handoff decidiu tema claro (índigo `#312E81`, coral `#FF5A36`, teal `#0D9488`). Os dois não podem coexistir.
- **Evidência de IA:** só sintética, autor único, TF-IDF como proxy. Útil para comparar métodos, não para prometer resultado.

## 2. Decisão

**Construir o núcleo "cadastro estruturado → matching explicável" sobre o protótipo atual, para um piloto em uma feira pequena. Insights e exportação vêm depois, sobre os dados que o piloto gerar.**

Razões:
1. O achado mais sólido do handoff é que *taxonomia por evento + texto livre do objetivo* dá matching bom (nDCG@5 ≈ 90% vs 60% de busca por palavra, em teste sintético). Esse é o diferencial; fórum e diretório não são.
2. Insights finais dependem de dados de comportamento que só existem após um evento real. Construí-los antes é construir no escuro.
3. Mais teste sintético tem retorno decrescente. O próximo dado útil vem de catálogo e visitantes reais.

## 3. Decisões tomadas

| Tema | Decisão |
| --- | --- |
| Tema visual | **Claro, guia índigo/coral/teal** (handoff). O tema escuro atual é descartado. `.superdesign/design-system.md` precisa ser reescrito. |
| Matching v1 | Híbrido: tema (chips) + texto livre + público-alvo, com explicação e limiar de "sem match". **Sem LLM obrigatório**: começa com TF-IDF/pgvector; LLM só para explicação e sugestão de tags. |
| Provedor de LLM | Adiado. Interface `ILlmProvider` no backend para trocar sem retrabalho. Decidir após teste com provedores reais. |
| Taxonomia | Por evento, 2 níveis, biblioteca-base por setor + sugestão por IA + **fila de aprovação em lote** (tela central). |
| Canal de aviso | E-mail + notificação no app. WhatsApp depois. |
| Momento | Matching antes do evento. QR/check-in na etapa seguinte. |
| LGPD | Consentimento por finalidade (perfil, compartilhar lead, enviar a LLM externo) desenhado desde o modelo de dados, não no fim. |

## 4. Ainda em aberto (precisa do Franco)

1. **Feira-piloto / catálogo real** — bloqueia validação de verdade.
2. **Monetização** — não bloqueia o código, bloqueia a página do organizador.
3. **Chave de API de algum LLM** — bloqueia teste de provedor.
4. **Recuperar os zips** (overlay da Fase 1 e avaliadores) do ambiente anterior.

## 5. Próximos passos (ordem)

1. **Recuperar ou refazer a Fase 1** (home pública, `/cadastro/{visitante|expositor|feira}`, tema claro) e revisar as promessas da home.
2. **Testes xUnit do backend** (`EventAccessService`, `Mapping.AuthorizedContacts`, papéis) — hoje não há nenhum.
3. **Modelo de dados da Etapa 2:** `InterestTag` hierárquica (sinônimos, descrição, status), `ParticipantEventProfile` (interesses + objetivo livre + prazo), temas em `Event`, oferta e público-alvo em `Company`. Nova migration.
4. **Cadastros:** wizard do visitante (≤3 min), perfil do estande, importação de expositores por planilha, fila de aprovação de temas.
5. **Matching v1** com `MatchScore` explicável e telas "Estandes para você" / "Leads para sua empresa".
6. **Piloto**, depois interações/QR, painéis e relatório final com IA.

## 6. Regras para não se enganar

- Números do avaliador são otimistas (mesmo autor para dados, taxonomia e gabarito). Comparações entre métodos valem; valores absolutos não.
- A home só pode prometer o que já existe; matching e relatórios estão marcados "Em breve" até as etapas 3 e 6.
- Medir com cátalogo e anotadores reais antes de fixar pesos ou limiar.

---

## 7. Atualização (19/11/2026) — estado e roteiro decidido

### Feito (branch `feature/fase1-reposicionamento`)

| Etapa | Entrega | Verificação |
| --- | --- | --- |
| 1 | Home pública, entrada por perfil, tema claro, "Em breve" no lugar da IA | build + navegador |
| Base | 54 testes xUnit do backend | testes (banco em memória) |
| 2 | Taxonomia por evento (API), biblioteca-base, fila de aprovação em lote, fusão | testes + chamadas reais ao PostgreSQL |
| 2 | Perfil do visitante por evento (objetivo livre, temas, 3 consentimentos) | idem |
| 2 | Perfil do estande (oferta, cliente ideal, até 3 temas) | idem |
| 2 | Telas: `/evento/meu-perfil`, `/evento/meu-estande`, `/organizador/taxonomia` | build; **telas não percorridas no navegador com a API** |

Fumaça real (PostgreSQL, banco descartável): migrations aplicam; importação da biblioteca é idempotente; sugestão de visitante cai na fila e só o organizador a vê; limite de 3 temas por estande devolve 400; perfil do visitante fica `readyForMatching`.

### Roteiro (ordem decidida)

1. **Matching v1** — motor híbrido em C# (tema + texto + público-alvo), `MatchScore` persistido com a decomposição, explicação por regras (sem LLM), limiar de "sem match", telas "Estandes para você" e "Leads para sua empresa". Pesos iniciais do avaliador (0,2 / 0,7 / 0,1) tratados como **hipótese**, não como valor validado. Só considera visitantes com consentimento de matching; leads só aparecem ao expositor se o visitante consentiu compartilhar.
2. **Cadastro em massa** — importação de expositores por planilha e convites por link (hoje o expositor se cadastra sozinho ou o organizador cria o vínculo).
3. **Dados de demonstração** — taxonomia, estandes e visitantes de exemplo no seed para demonstrar o fluxo completo sem preencher à mão.
4. **Interações e avisos (Etapa 4)** — registro de ações, check-in por QR, aviso por e-mail e no app.
5. **Insights e exportação (Etapa 6)** — painéis, CSV/XLSX/PDF, relatório final.
6. **Camada de LLM (Etapa 5)** — interface `ILlmProvider`; sugestão de temas, explicações e relatório; só depois do teste com provedores reais e do parecer de LGPD.
7. **Piloto e produção (Etapa 7)** — feira pequena, LGPD, observabilidade.

### Dependências externas (não bloqueiam os itens 1 a 3)

- Feira-piloto ou catálogo real (necessário para validar pesos e limiar).
- Chave de API de um LLM (item 6).
- Parecer jurídico sobre consentimento, compartilhamento de leads e envio a LLM.
- Monetização (afeta só a página do organizador).

---

## 8. Atualização (19/11/2026, fim do trabalho autônomo) — o que falta depende de você

### Concluído (itens 1 a 5 do roteiro, mais as bases do 4 e do 5)

| Item | Entrega | Verificação |
| --- | --- | --- |
| Matching v1 | Motor híbrido (tema + texto + público-alvo), `MatchScore`, estandes recomendados e leads, com motivos | 116 testes; fumaça real |
| Cadastro em massa | Importação de expositores por CSV (prévia + confirmação) e convites por link | testes; fumaça real |
| Demonstração | Seed aditivo: taxonomia, estandes, 5 visitantes com consentimentos variados | fumaça real |
| Interações e avisos | Check-in por QR, "isso foi útil?", avisos no app (com apagamento ao revogar consentimento) | testes; fumaça real |
| Insights | Painel (lacunas, demanda x oferta, concentração de exposição, perfis, termos), relatório do estande, CSV | testes; fumaça real |

`node scripts/smoke-e2e.mjs` (57 verificações) roda contra uma API local com `Seed:DemoData=true` e um **banco descartável**; a última execução passou inteira em PostgreSQL real. No navegador foram vistas, com a API real, a página do evento do visitante e o painel do organizador (em tela de celular); **as demais telas só passaram no build**.

Para ver tudo no seu ambiente: pare a API, rode `dotnet ef database update` (migrations `Etapa2Taxonomia`, `Etapa3Matching`, `Etapa4ConvitesImportacao`, `Etapa5CheckinAvisos`) e inicie de novo. O seed é aditivo e cria os dados de demonstração novos no banco existente.

### O que NÃO foi feito e por quê

| Pendência | Bloqueio |
| --- | --- |
| Validar/calibrar pesos e limiares do matching | **Feira-piloto ou catálogo real** e rotulagem por pessoas independentes. Os números atuais são hipótese (teste sintético de autor único). |
| Camada de LLM (sugerir temas por site/catálogo, explicações, relatório final com IA) | **Chave de API** e teste de provedores; **parecer jurídico** sobre envio de dados a terceiros. O consentimento "IA externa" já existe no modelo, mas nada é enviado. |
| Aviso por e-mail e WhatsApp | **Provedor de envio** (SMTP ou serviço) e texto de consentimento próprio, revisado juridicamente. Hoje só há aviso no app. |
| LGPD (bases legais, textos de consentimento, retenção, direitos do titular, DPO) | **Parecer jurídico.** Os consentimentos por finalidade e a regra de apagar avisos ao revogar existem, mas os textos não foram revisados. |
| Monetização e formulário comercial do organizador | **Decisão de negócio.** |
| Piloto, produção, observabilidade, backup, HTTPS | **Feira-piloto e ambiente de hospedagem.** |
| Exportação XLSX e PDF do servidor | Só CSV e impressão do navegador; fica para quando houver necessidade confirmada. |

### Limites conhecidos (não resolvidos)

- Fonte IBM Plex Sans não é carregada localmente; a rota `/empresas` ainda não foi renomeada para `/estandes`.
- Não há testes automatizados de frontend; o backend é testado com banco em memória e com a fumaça acima.
- Faixa "alta" do matching exige sinal de tema além do texto (texto sozinho tende a ficar em "média"); é consequência dos pesos atuais e deve ser reavaliada com dados reais.
- Cobertura de leads depende de o visitante ter salvo o perfil (os scores são calculados ao consultar e ao salvar).
