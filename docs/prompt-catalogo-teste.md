# Prompt para gerar o catálogo de teste (150 expositores)

Objetivo: ter um catálogo **fictício, porém com a bagunça de um catálogo real**, para medir a taxonomia e o matching com mais fundamento do que a planilha de 40 estandes (autor único, dados "limpos").

## Por que 150 linhas, em 5 lotes de 30

- Com 40 estandes, uma diferença de 2 a 3 estandes já muda os percentuais: era ruído. Com 150, cada tema relevante (cerca de 25 a 30) tem em média 4 a 6 estandes, e a cauda longa (empresas que não cabem em tema nenhum) aparece em número suficiente para medir.
- Em lotes de 30 a IA não trunca a resposta, não se repete e dá para variar o foco de cada lote. Depois é só juntar.
- Mais que 150 não compensa agora: o gargalo passa a ser rotular os dados, não gerá-los.

## Regras de independência (importantes para o teste valer)

1. **Não peça temas nem gabarito neste prompt.** A coluna `temas` sai vazia de propósito: quem vai propor temas depois é o sistema, sem ter visto nenhuma resposta.
2. Se for medir o matching, gere os **visitantes e as relevâncias** em outra chamada (prompt 2 abaixo), de preferência com **outro modelo ou outra conversa**, e sem mostrar a taxonomia do Conexa.
3. Idealmente uma pessoa confere uma amostra dos estandes marcados como relevantes.
4. Não corrija o catálogo para "ajudar" o sistema depois de ver os resultados.

## Prompt 1 — catálogo (rode 5 vezes, trocando só o bloco LOTE)

Copie tudo entre as linhas, trocando `{{...}}` do lote.

````text
Você vai criar parte de um catálogo FICTÍCIO de expositores de uma feira multissetorial brasileira de negócios (indústria, tecnologia e serviços para empresas), com 150 expositores no total, divididos em 5 lotes de 30. Este é o lote {{N}} de 5.

OBJETIVO: o catálogo servirá para testar um sistema de recomendação. Por isso ele precisa parecer REAL, com a bagunça de um catálogo de verdade. Não deve parecer organizado por quem conhece o sistema.

LOTE {{N}}: {{FOCO}}
Numeração dos estandes deste lote: {{FAIXA}}

SAÍDA: somente um CSV, sem nenhum texto antes ou depois, sem markdown, sem bloco de código.
- Codificação UTF-8, separador ponto e vírgula (;), todo campo de texto entre aspas duplas, sem quebras de linha dentro dos campos.
- Primeira linha, exatamente:
nome;segmento;descricao;produtos;cliente_ideal;estande;site;temas;email
- Exatamente 30 linhas de dados.
- Deixe as colunas `temas` e `email` SEMPRE vazias ("").
- `estande`: no formato "Pavilhão X • Estande NN", seguindo a faixa do lote.
- `site`: use apenas domínios fictícios terminados em .example (ex.: https://prodtrack.example).

REALISMO (siga as proporções aproximadas dentro dos 30):
- Nomes de empresas inventados e plausíveis; nenhum nome de empresa real ou parecido com um conhecido. Misture estilos: siglas, sobrenomes, nomes em inglês, "Ltda", nomes curtos.
- ~20% com descrição curta e vaga (1 frase genérica, como "soluções completas para o seu negócio").
- ~60% com descrição e produtos concretos (2 a 4 frases, termos técnicos do setor).
- ~20% com descrição longa e cheia de jargão.
- Pelo menos 2 com um pequeno erro de digitação ou frase mal escrita, como acontece em cadastros reais.
- `segmento`: escreva como a própria empresa escreveria. NÃO padronize: nomes diferentes para coisas parecidas, alguns genéricos ("Serviços", "Tecnologia"), alguns muito específicos. Quase nunca repita o mesmo segmento.
- `cliente_ideal`: preencha em ~70% das linhas, de forma específica ("indústrias de médio porte com gerente de produção"); nas demais deixe vazio.
- ~30% das empresas devem ser da "cauda longa": atividades que não cabem em categorias comuns de feira (ex.: crédito para empresas, comércio exterior, EPIs, embalagens, consultoria tributária, treinamento técnico, logística reversa, certificações, seguros industriais).
- ~15% devem conectar dois mundos diferentes (ex.: software para agronegócio, energia para varejo, IoT para saúde), para que haja conexões entre áreas.
- Inclua termos ambíguos que significam coisas diferentes em contextos diferentes (por exemplo "apontamento" em ERP e em IoT industrial; "monitoramento" em segurança, em saúde e em infraestrutura), sem explicá-los.
- Não repita o mesmo produto em empresas diferentes do lote; não crie dois estandes da mesma empresa.

NÃO FAÇA: não classifique em temas, não explique, não numere as linhas, não escreva "exemplo", não inclua nada além do CSV.
````

### Blocos de lote

| N | FOCO | FAIXA |
| --- | --- | --- |
| 1 | Indústria: automação, máquinas, usinagem, metalmecânica, insumos, EPIs, embalagens, manutenção. | Pavilhão A • Estande 01 a 30 |
| 2 | Tecnologia para empresas: software de gestão, dados e IA, nuvem, segurança da informação, telecom, dispositivos. | Pavilhão B • Estande 01 a 30 |
| 3 | Varejo, logística e serviços: e-commerce, pagamentos, crédito, comércio exterior, transporte, armazenagem, seguros, contabilidade. | Pavilhão C • Estande 01 a 30 |
| 4 | Agronegócio, energia e sustentabilidade: agricultura digital, insumos, energia solar e eficiência, resíduos, água, certificações ambientais. | Pavilhão D • Estande 01 a 30 |
| 5 | Mistura: saúde e bem-estar corporativo, educação e treinamento, construção e infraestrutura, alimentos e bebidas B2B, marketing e eventos, e várias empresas de nicho difícil de classificar. | Pavilhão E • Estande 01 a 30 |

## Depois de gerar

1. Junte os 5 CSVs numa só planilha (um só cabeçalho). Se o Excel bagunçar os acentos, abra como UTF-8.
2. Confira rapidamente: 150 linhas, nomes únicos, nenhuma empresa real reconhecível.
3. No Conexa: **Organizador → Importar expositores**, cole o conteúdo e clique em **Validar**. A prévia mostra linhas inválidas e duplicadas antes de gravar.
4. Guarde uma cópia congelada (não edite depois de ver os resultados) e anote qual modelo e qual data geraram cada lote.

## Prompt 2 — visitantes e relevâncias (opcional, para o matching)

Use **outra conversa ou outro modelo**, e dê a ele o catálogo gerado (somente as colunas nome, segmento, descricao, produtos, cliente_ideal, estande).

````text
Abaixo está o catálogo de uma feira (CSV). Crie 60 VISITANTES FICTÍCIOS que poderiam ir a essa feira e, para cada um, indique quais estandes seriam realmente relevantes.

Para cada visitante, produza um objeto JSON com:
- "id": "V01" a "V60"
- "cargo": cargo realista
- "setor": setor da empresa dele
- "porte": "micro", "pequena", "média" ou "grande"
- "objetivo": 1 a 3 frases, escritas como a PESSOA escreveria num formulário (informal, às vezes vago ou com erro), sem copiar palavras do catálogo de propósito
- "relevantes_muito": lista de nomes de estandes MUITO relevantes (0 a 3)
- "relevantes_algum": lista de nomes de estandes com alguma relevância (0 a 4)

Regras:
- Use apenas nomes de estandes que existam no catálogo, escritos exatamente como lá.
- Pelo menos 6 visitantes devem NÃO ter nenhum estande relevante (0 e 0), porque o objetivo deles não é atendido pela feira.
- Pelo menos 10 visitantes devem ter objetivo vago, e pelo menos 10 devem ter relevância em estandes de pavilhões diferentes.
- Varie cargos e setores; evite repetir o mesmo perfil.
- Julgue a relevância pelo que o visitante realmente precisa, não pelo nome do estande.

Responda somente com um array JSON válido, sem texto antes ou depois.

CATÁLOGO:
{{cole aqui o CSV}}
````

Esse arquivo vira o gabarito de matching. Divida os 60 em 30 para ajustar pesos e 30 para teste, e não mexa nos de teste depois de ver os resultados.

## Rodar automaticamente (script)

`scripts/gerar-catalogo.mjs` executa os 5 lotes no Google Gemini usando este mesmo arquivo como fonte do prompt. Cada lote é validado (cabeçalho, 30 linhas, 9 colunas, estandes na faixa, sites `.example`, nomes únicos entre todos os lotes) e refeito até 3 vezes, devolvendo ao modelo a lista de erros. Saída em `dados-teste/catalogo/`: `lote-1..5.csv`, `catalogo_150.csv` (cabeçalho único, BOM para o Excel) e `metadados.json` (modelo, data, temperatura, hash do prompt).

```powershell
# guarda a chave fora do git (não cole a chave em chats)
dotnet user-secrets set "Llm:ApiKey" "<sua-chave>" --project backend/Conexa.Api

node scripts/gerar-catalogo.mjs --dry-run     # só mostra o prompt do lote 1
node scripts/gerar-catalogo.mjs               # roda os 5 lotes
node scripts/gerar-catalogo.mjs --lotes 3     # refaz só o lote 3
node scripts/gerar-catalogo.mjs --model <nome> --temperature 1.0
```

O modelo padrão é `gemini-3.5-flash`. Em 23/11/2026 a chave foi validada (a lista de modelos respondeu HTTP 200) e o `gemini-3.5-flash` respondeu; o `gemini-3.8-flash` e outros devolveram 503 de alta demanda (temporário) e o `gemini-2.5-flash` está bloqueado para contas novas. Esses modelos "pensam" antes de responder, e o raciocínio consome o limite de saída (por isso o script usa um limite alto); a latência varia de segundos a cerca de 30 s num pedido trivial, então cada lote pode levar minutos. Troque o modelo com `--model` ou `GEMINI_MODEL`. Alternativa à chave nos secrets: `GEMINI_API_KEY`. O script ainda **não gerou um catálogo de verdade** contra o Gemini (só foi testado contra servidor falso).
