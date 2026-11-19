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
