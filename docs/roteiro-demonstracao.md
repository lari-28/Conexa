# Roteiro de demonstração

Pré-requisito: README seguido, API e web rodando, seed aplicado.

1. **Dois usuários simultâneos** — janela normal: `ana@conexa.demo`; janela anônima (ou outro dispositivo na LAN): `bruno@conexa.demo`.
2. **Networking** — Ana abre *Pessoas*, busca "Prisma", filtra por interesse "startups" e abre o perfil do Bruno: **sem contatos** (ele não autorizou).
3. **Privacidade** — Bruno vai em *Perfil → Privacidade*, marca o e-mail, aceita o consentimento e salva. Ana recarrega o perfil do Bruno: o e-mail aparece. Bruno desmarca: o e-mail some.
4. **Empresas** — Ana abre *Empresas → Nimbus Cloud* e vê a representante Carla, com e-mail visível (ela autorizou).
5. **Fórum** — Ana publica uma dúvida; Bruno comenta e reage; Ana vê o contador atualizar ao recarregar.
6. **Denúncia** — Bruno denuncia a publicação de Ana informando um motivo.
7. **Moderação** — `organizador@conexa.demo` abre *Organizador · Moderação*, vê a denúncia, oculta o conteúdo e/ou suspende o autor no evento.
8. **Acessos** — em *Organizador · Participantes* a organizadora habilita novamente o usuário suspenso.
9. **Isolamento** — Ana (sem vínculo) tenta acessar o evento de outro: a API responde 403/404.
10. **Admin** — `admin@conexa.demo` cria um novo evento em *Admin · Eventos* e designa um organizador.
11. **Persistência** — reinicie a API (`Ctrl+C` e `dotnet run`): os dados continuam.
