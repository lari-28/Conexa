# Dados de teste do matching (gerados pelo Gemini)

Dois arquivos **congelados**: não edite depois de ver resultados (se editar, atualize o hash e refaça todos os números).

| Arquivo | O que é | SHA-256 |
| --- | --- | --- |
| `catalogo/catalogo_150_gemini.csv` | 150 expositores fictícios, formato do importador do Conexa (`;`, colunas `temas` e `email` vazias de propósito) | `1eedf1dc436897bfcce426406b154f5c6ba5b02746c4b60f1d96469af29f5ca6` |
| `matching/visitantes_60_gemini.json` | 60 visitantes fictícios com objetivo em texto livre e estandes relevantes (`relevantes_muito` = ganho 2, `relevantes_algum` = ganho 1) | `c4588a0ad3d16754c836156ade93d6efe008974583f962968a75168a442fd02a` |

## O que foi conferido
- Catálogo: 150 linhas, 9 colunas, nomes únicos, sites `.example`, sem temas nem e-mails. 44 linhas sem cliente ideal, 29 descrições curtas, 30 longas.
- Visitantes: 60 objetos, todos os nomes de estande dos rótulos existem no catálogo.

## Problemas conhecidos nos dados
1. **Códigos de estande repetidos (20):** o lote 1 vazou para os pavilhões A, B e C, então "Pavilhão B • Estande 11 a 20" e "Pavilhão C • Estande 21 a 30" aparecem duas vezes. Não afeta o importador (a chave é o nome), mas a localização desses estandes não é única.
2. **Mesmo autor, vocabulário compartilhado:** catálogo, visitantes e gabarito vêm do mesmo modelo, e os visitantes específicos (V21 a V60) descrevem a necessidade com os termos técnicos dos estandes. Os números absolutos são otimistas.
3. **Objetivos vagos (V09 a V20):** o rótulo "alguma relevância" para 3 ou 4 estandes foi escolhido sem base no texto ("vim dar uma geral na feira"). São reportados à parte e **não** servem para ajustar pesos.
4. **Divisão fixa:** entre os 40 específicos, ids ímpares = desenvolvimento, pares = teste (oficial). Os 9 visitantes sem estande relevante medem o limiar de "sem match".

## Como reavaliar
```powershell
dotnet run --project backend/Conexa.Eval --configuration EfTemp -- dados-teste/catalogo/catalogo_150_gemini.csv dados-teste/matching/visitantes_60_gemini.json docs/avaliacao-matching-gemini.md
```
(`--configuration EfTemp` evita o bloqueio de arquivos quando a API está rodando.) O relatório gerado está em [docs/avaliacao-matching-gemini.md](../docs/avaliacao-matching-gemini.md).
