# Avaliação do matching: catálogo e visitantes gerados pelo Gemini

Gerado por `Conexa.Eval` em 23/11/2026. Catálogo: 150 estandes. Visitantes: 60 (40 com objetivo específico, 11 vagos, 9 sem estande relevante).

> **Leia isto antes dos números.** Catálogo, visitantes e gabarito vieram do mesmo modelo de IA, e o objetivo dos visitantes reaproveita muitas palavras do catálogo (os visitantes específicos descrevem a necessidade com os termos técnicos dos estandes). Os valores absolutos são otimistas; as comparações entre métodos são mais confiáveis do que os números isolados. O catálogo não tem taxonomia, então o sinal de **tema não foi medido** aqui.

## Qualidade do ranking (visitantes específicos)

Desenvolvimento = ids ímpares (20); **teste = ids pares (20), números oficiais**. nDCG@5 usa ganho 2 (muito relevante) e 1 (alguma relevância).

### Teste (oficial) (n = 20)

| Método | nDCG@5 | Precisão@5 | Revocação@5 | Acerta em top-3 | MRR |
|---|---|---|---|---|---|
| aleatório (média de 200 sorteios) | 3% | 3% | 3% | 8% | 0,11 |
| busca por palavra (sem sinônimos) | 72% | 40% | 50% | 100% | 1,00 |
| motor: só texto (TF-IDF de n-gramas) | 77% | 44% | 55% | 100% | 0,98 |
| motor: texto + público-alvo (padrão atual) | 79% | 46% | 58% | 100% | 1,00 |
| motor: texto 50% + público-alvo 50% | 78% | 48% | 60% | 100% | 1,00 |
| motor: adaptativo (público-alvo pesa mais se o texto é fraco) | 79% | 46% | 58% | 100% | 1,00 |

### Desenvolvimento (n = 20)

| Método | nDCG@5 | Precisão@5 | Revocação@5 | Acerta em top-3 | MRR |
|---|---|---|---|---|---|
| aleatório (média de 200 sorteios) | 3% | 3% | 3% | 8% | 0,10 |
| busca por palavra (sem sinônimos) | 68% | 37% | 45% | 100% | 0,98 |
| motor: só texto (TF-IDF de n-gramas) | 77% | 45% | 54% | 100% | 1,00 |
| motor: texto + público-alvo (padrão atual) | 80% | 51% | 62% | 100% | 1,00 |
| motor: texto 50% + público-alvo 50% | 67% | 39% | 48% | 95% | 0,92 |
| motor: adaptativo (público-alvo pesa mais se o texto é fraco) | 78% | 49% | 59% | 100% | 1,00 |

Diferença de nDCG@5, motor padrão menos busca por palavra (bootstrap pareado, 5000 reamostragens): **teste +7,4 pontos (IC 95%: +2,6 a +12,7)**; desenvolvimento +12,2 pontos (IC: +6,7 a +19,1).

### Objetivo vago (V09 a V20): só descritivo, o rótulo é discutível (n = 11)

| Método | nDCG@5 | Precisão@5 | Revocação@5 | Acerta em top-3 | MRR |
|---|---|---|---|---|---|
| aleatório (média de 200 sorteios) | 3% | 2% | 3% | 8% | 0,09 |
| busca por palavra (sem sinônimos) | 2% | 2% | 2% | 9% | 0,07 |
| motor: só texto (TF-IDF de n-gramas) | 4% | 4% | 5% | 18% | 0,14 |
| motor: texto + público-alvo (padrão atual) | 19% | 15% | 22% | 36% | 0,35 |
| motor: texto 50% + público-alvo 50% | 33% | 20% | 30% | 64% | 0,64 |
| motor: adaptativo (público-alvo pesa mais se o texto é fraco) | 33% | 20% | 30% | 64% | 0,64 |

## Limiar de "sem match" e calibração das faixas

Motor padrão. Para cada limiar de score: **cobertura** = % dos visitantes específicos com ao menos uma recomendação; **falso alarme** = % dos visitantes sem estande relevante que ainda receberiam alguma recomendação; **precisão** = % das recomendações que são relevantes (visitantes específicos, até 10 por pessoa).

| Limiar | Cobertura | Falso alarme | Precisão das recomendações |
|---|---|---|---|
| 0,05 | 100% | 100% | 28% (400 itens) |
| 0,08 | 100% | 100% | 29% (384 itens) |
| 0,10 | 100% | 56% | 36% (294 itens) |
| 0,12 | 100% | 44% | 56% (165 itens) |
| 0,15 | 100% | 22% | 84% (90 itens) |
| 0,20 | 97% | 0% | 98% (60 itens) |
| 0,25 | 60% | 0% | 100% (33 itens) |
| 0,30 | 35% | 0% | 100% (16 itens) |
| 0,40 | 7% | 0% | 100% (3 itens) |
| 0,45 | 3% | 0% | 100% (1 itens) |

Limiares atuais do sistema: baixa ≥ 0,12, média ≥ 0,25, alta ≥ 0,45.

| Faixa atual | Recomendações (específicos) | % relevantes |
|---|---|---|
| alta | 1 | 100% |
| média | 32 | 100% |
| baixa | 132 | 45% |

Melhor score de cada visitante sem estande relevante (deveriam ficar abaixo do limiar):

| Visitante | Objetivo | Melhor score | Faixa |
|---|---|---|---|
| V01 | To procurando fornecedores de ração super premium, petiscos naturais … | 0,08 | sem match |
| V02 | Vim ver novidades de tinturas, progressiva sem formol e cadeiras hidr… | 0,17 | baixa |
| V03 | Buscando livros didáticos bilíngues, brinquedos pedagógicos e mobiliá… | 0,08 | sem match |
| V04 | Quero fechar parceria com importadora ou fabricante de violões, tecla… | 0,10 | sem match |
| V05 | Procurando um sistema pra anunciar apartamentos de aluguel nos portai… | 0,09 | sem match |
| V06 | Vim atrás de tecidos finos, rendas importadas e pedrarias para confec… | 0,18 | baixa |
| V07 | Precisamos trocar os equipamentos da academia do clube, esteiras prof… | 0,13 | baixa |
| V08 | Tô precisando de elevador automotivo de 2 colunas, scanner de injeção… | 0,13 | baixa |
| V19 | Vim conhecer a feira pra ver na prática como funcionam as tecnologias… | 0,10 | sem match |

Melhor score dos visitantes específicos: mínimo 0,19, mediana 0,26, máximo 0,47.

## Onde o motor erra (visitantes específicos, pior nDCG@5 primeiro)

| Visitante | nDCG@5 | 1º lugar do motor | Relevantes (muito) |
|---|---|---|---|
| V31 | 58% | AgroPulse Agricultura Digital Ltda | AgroPulse Agricultura Digital Ltda; NexaNet Conectividade Satelital |
| V29 | 60% | VetorPragas Controle Ambiental Ltda | VetorPragas Controle Ambiental Ltda; KronoPisos Revestimentos Industr… |
| V41 | 64% | MedCloud Prontuário & IA | MedCloud Prontuário & IA |
| V54 | 68% | B2BBridge Marketplace Industrial | B2BBridge Marketplace Industrial |
| V57 | 68% | Dataloom Engenharia de Dados | Dataloom Engenharia de Dados |
| V32 | 70% | MentePlena Saúde Emocional B2B | MentePlena Saúde Emocional B2B |
| V24 | 71% | NexusOMS Orquestração de Pedidos | NexusOMS Orquestração de Pedidos; SmartLocker Pontos de Retirada; Sel… |
| V59 | 76% | Perícia & Valor Consultoria Contábil | Perícia & Valor Consultoria Contábil |

## Exposição entre estandes (top-5 de cada um dos 60 visitantes)

Estandes que aparecem para alguém: 106 de 150. Nunca aparecem: 44. Mais recomendado: 4 visitantes. Gini 0,46.

