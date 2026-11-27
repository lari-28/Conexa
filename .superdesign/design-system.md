# CONEXA Design System — EventHub Operations

## Fonte visual

- **Projeto Superdesign:** `a5e3f57c-3a50-4610-9b7b-0563f8758408`
- **Referência consultada:** preview `EventHub Admin Dashboard` no Superdesign, incluindo a resposta de criação e a renderização em desktop.
- **Elementos observados:** sidebar escura com navegação agrupada; cabeçalho com busca e contexto; cartões de métricas; grade visual de eventos; navegação inferior no celular; superfícies translúcidas e transições suaves.
- **Paleta explícita na especificação do projeto:** fundo `#121212`, ciano `#00D4FF`, violeta `#9D4EDD`, verde `#00FF88`.
- Tons de superfície e semânticos abaixo foram definidos para reproduzir a hierarquia do preview e manter legibilidade; não foram apresentados como tokens explícitos pelo Superdesign.

## Produto e direção

CONEXA conecta participantes, expositores e organizadores de eventos. A interface deve parecer um centro moderno de operações: escura, focada, rica em dados e fácil de percorrer. A energia vem dos acentos ciano, violeta e verde em pontos específicos, enquanto o conteúdo continua sendo o foco.

Use uma sidebar persistente em telas largas, um cabeçalho compacto para o evento e a conta, e uma área principal que prioriza informações e tarefas. Em telas estreitas, recolha a navegação lateral e preserve a barra inferior e a folha de navegação existentes. Cards devem agrupar métricas, eventos e conteúdo social sem competir entre si.

Use superfícies translúcidas e desfoque de fundo em camadas de navegação, popovers e painéis elevados. Mantenha fundos de leitura e formulários sólidos. Não usar identidade de automobilismo, vermelho como cor de marca, glass em cada componente, brilho neon permanente ou decoração sem relação com dados e navegação.

## Tokens

### Cor

| Token | Valor | Uso |
| --- | --- | --- |
| `canvas` | `#121212` | Fundo geral |
| `surface` | `#191A1D` | Cards e painéis |
| `surface-raised` | `#222328` | Menus, hover e camadas elevadas |
| `surface-inset` | `#101114` | Sidebar e campos |
| `surface-glass` | `rgb(31 32 37 / 82%)` | Cabeçalho translúcido e overlays |
| `ink` | `#F5F6F8` | Texto principal |
| `muted` | `#A4A7B0` | Texto secundário |
| `subtle` | `#737782` | Metadados e rótulos discretos |
| `border` | `#33353D` | Bordas e divisores |
| `cyan` | `#00D4FF` | Ação primária, seleção e links ativos |
| `cyan-strong` | `#00A9CC` | Hover e variação de ação |
| `cyan-soft` | `#07303A` | Fundo leve de seleção |
| `purple` | `#9D4EDD` | Segunda série de dados e destaque de categoria |
| `purple-soft` | `#2B1938` | Fundo leve violeta |
| `green` | `#00FF88` | Confirmação, disponibilidade e métricas positivas |
| `green-soft` | `#0B3324` | Fundo leve de sucesso |
| `danger` | `#FF6B75` | Erros e ações destrutivas |
| `warning` | `#FFCC66` | Atenção e pendências |

Use ciano, violeta e verde como sinais separados. Não colorir cartões inteiros com os tons saturados. Status deve ter rótulo além da cor.

### Tipografia

- Interface: `Inter`, fallback `Segoe UI`, `ui-sans-serif`, `system-ui`.
- Texto principal: 14–16px; labels e metadados: 11–13px.
- Título da página: 28–34px; título de seção: 18–22px.
- Use numerais tabulares em totais, datas, contagens e métricas.
- Fonte monoespaçada fica restrita a IDs, horários técnicos e pequenos rótulos de dados.

### Layout, forma e movimento

- Escala de espaçamento baseada em 4px: 4, 8, 12, 16, 24, 32, 48, 64px.
- Sidebar desktop: aproximadamente 248px; conteúdo principal fluido, com largura máxima de 1500px.
- Campos e botões: altura mínima de 44px; raio de 8px. Cards: raio de 12–16px. Pills: raio completo.
- Separação por borda fina; sombras curtas e escuras em menus e modais. Desfoque de 14–20px apenas em superfícies sobrepostas.
- Transições de 120–180ms; respeitar `prefers-reduced-motion`.
- Breakpoint do shell: 1100px. Abaixo dele, usar navegação inferior e menu compacto.

## Componentes e acessibilidade

- Ações: primária ciano com texto escuro para contraste; secundária neutra; terciária sem fundo; destrutiva em tom de erro. Incluir hover, foco, loading e disabled.
- Campos: rótulo persistente, placeholder auxiliar, erro junto ao campo e foco com contorno visível.
- Sidebar: categorias e itens agrupados, item atual marcado por fundo sutil e filete ciano; ícone sempre acompanhado de texto.
- Cards de métricas podem usar ciano, violeta e verde como pequenos indicadores, sem depender da cor para transmitir o valor.
- Manter landmarks semânticos, navegação por teclado, foco visível, contraste de texto e alvos de toque de pelo menos 44px.
- Mobile: preservar rótulos e área útil; não deixar filtros, ações ou textos longos causarem overflow.

## Página de biblioteca

`/ui-kit` documenta em português os componentes compartilhados reais: shell e navegação, botões, campos, avatares, badges, cards, feedback, carregamento/vazio, paginação, modal, cartão de participante e padrão de publicação. Use conteúdo de exemplo do CONEXA Summit e mostre o padrão de sidebar/dashboard do projeto Superdesign. A biblioteca é uma referência de produto, não um bloco de códigos.

## Contexto de implementação

- Aplicação: React 19 + TypeScript + Vite.
- Tokens e padrões globais: `frontend/conexa-web/src/index.css`.
- Shell autenticado: `frontend/conexa-web/src/components/layout/AppLayout.tsx`.
- Componentes partilhados: `frontend/conexa-web/src/components/ui/index.tsx` e `frontend/conexa-web/src/components/common/index.tsx`.
- Painel do organizador: `frontend/conexa-web/src/pages/organizer/OrganizerDashboardPage.tsx`.
- Biblioteca visual: `frontend/conexa-web/src/pages/DesignSystemPage.tsx`.
