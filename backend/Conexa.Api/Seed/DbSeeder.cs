namespace Conexa.Api.Seed;

public static class DbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();

        if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
        {
            if (!db.Database.GetMigrations().Any())
                throw new InvalidOperationException(
                    "Nenhuma migration encontrada. Execute uma vez: dotnet ef migrations add InitialCreate (veja o README).");
            await db.Database.MigrateAsync();
        }

        // Só semeia se o schema existir (evita falhar quando o banco ainda não foi migrado em produção).
        if (await db.Database.CanConnectAsync() && (await db.Database.GetAppliedMigrationsAsync()).Any())
            await DbSeeder.SeedAsync(sp, app.Configuration);
    }
}

/// <summary>Seed idempotente: papéis sempre; admin se configurado; dados de demonstração se Seed:DemoData = true.</summary>
public static class DbSeeder
{
    private const string DemoEventName = "CONEXA Summit";

    public static async Task SeedAsync(IServiceProvider sp, IConfiguration cfg)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));

        var adminEmail = cfg["Seed:AdminEmail"];
        var adminPassword = cfg["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            await EnsureUserAsync(sp, adminEmail, "Administrador CONEXA", adminPassword, [Roles.PlatformAdmin]);

        if (!cfg.GetValue<bool>("Seed:DemoData")) return;
        var demoPassword = string.IsNullOrWhiteSpace(cfg["Seed:DemoPassword"]) ? "Conexa@123" : cfg["Seed:DemoPassword"]!;
        await SeedDemoAsync(sp, db, demoPassword);
    }

    private static async Task SeedDemoAsync(IServiceProvider sp, ApplicationDbContext db, string pwd)
    {
        // Evento fictício
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Name == DemoEventName);
        if (ev is null)
        {
            var start = DateTime.UtcNow.Date.AddHours(12);
            ev = new Event
            {
                Name = DemoEventName,
                Description = "Evento fictício para demonstração do CONEXA: conecte-se com pessoas e empresas, tire dúvidas e troque ideias.",
                StartsAt = new DateTimeOffset(start, TimeSpan.Zero),
                EndsAt = new DateTimeOffset(start.AddDays(2), TimeSpan.Zero),
                Location = "Centro de Convenções Exemplo — Porto Alegre/RS",
                Status = EventStatus.Active
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync();
        }

        // Contas fictícias
        var organizer = await EnsureUserAsync(sp, "organizador@conexa.demo", "Marina Teixeira", pwd, [Roles.Organizer],
            new UserProfile { CompanyName = "Organização CONEXA Summit", JobTitle = "Coordenadora do evento", Area = "Eventos",
                Bio = "Responsável pela organização e moderação do evento.", Interests = ["eventos", "networking"] });
        var ana = await EnsureUserAsync(sp, "ana@conexa.demo", "Ana Souza", pwd, [Roles.Participant],
            new UserProfile { CompanyName = "Lumen Labs", JobTitle = "Product Manager", Area = "Tecnologia",
                Bio = "Produto B2B há 8 anos. Gosto de conectar times técnicos e de negócio.",
                Interests = ["inteligência artificial", "produto", "startups"],
                LookingFor = "Mentoria e parcerias com empresas de infraestrutura.",
                Offers = "Experiência em descoberta de produto e validação com clientes." },
            new ContactPrivacy { EmailVisible = true, ConsentGivenAt = DateTimeOffset.UtcNow });
        var bruno = await EnsureUserAsync(sp, "bruno@conexa.demo", "Bruno Lima", pwd, [Roles.Participant],
            new UserProfile { CompanyName = "Estúdio Prisma", JobTitle = "Diretor de Marca", Area = "Marketing",
                Bio = "Branding e comunicação para negócios em crescimento.",
                Interests = ["branding", "vendas", "startups"],
                LookingFor = "Clientes na área de tecnologia e agro.",
                Offers = "Posicionamento de marca e identidade visual." });
        var carla = await EnsureUserAsync(sp, "carla@conexa.demo", "Carla Menezes", pwd, [Roles.Exhibitor],
            new UserProfile { CompanyName = "Nimbus Cloud", JobTitle = "Head de Parcerias", Area = "Infraestrutura",
                Bio = "Representante da Nimbus Cloud no evento.",
                Interests = ["nuvem", "segurança", "inteligência artificial"],
                LookingFor = "Parcerias com startups.", Offers = "Créditos de nuvem para projetos selecionados." },
            new ContactPrivacy { EmailVisible = true, ConsentGivenAt = DateTimeOffset.UtcNow });

        // Vínculos com o evento
        await EnsureMembershipAsync(db, ev.Id, organizer.Id, EventRole.Organizer);
        await EnsureMembershipAsync(db, ev.Id, ana.Id, EventRole.Participant);
        await EnsureMembershipAsync(db, ev.Id, bruno.Id, EventRole.Participant);
        await EnsureMembershipAsync(db, ev.Id, carla.Id, EventRole.Exhibitor);
        await db.SaveChangesAsync();

        // Empresas
        if (!await db.Companies.AnyAsync(c => c.EventId == ev.Id))
        {
            var nimbus = new Company { EventId = ev.Id, Name = "Nimbus Cloud", Segment = "Infraestrutura e nuvem",
                Description = "Plataforma de nuvem para startups e médias empresas.",
                ProductsServices = "Hospedagem, bancos gerenciados, observabilidade e créditos para startups.",
                BoothLocation = "Pavilhão A • Estande 12", Website = "https://nimbus.example" };
            var agro = new Company { EventId = ev.Id, Name = "AgroTech Sul", Segment = "Agronegócio",
                Description = "Soluções digitais para produtores rurais.",
                ProductsServices = "Monitoramento de lavouras e gestão de safra.",
                BoothLocation = "Pavilhão B • Estande 03", Website = "https://agrotech.example" };
            var verde = new Company { EventId = ev.Id, Name = "Verde Energia", Segment = "Energia",
                Description = "Energia solar para empresas.",
                ProductsServices = "Projetos fotovoltaicos e consultoria energética.",
                BoothLocation = "Pavilhão A • Estande 21" };
            db.Companies.AddRange(nimbus, agro, verde);
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = nimbus.Id, UserId = carla.Id, Title = "Head de Parcerias" });
            await db.SaveChangesAsync();
        }

        // Fórum inicial
        if (!await db.Posts.AnyAsync(p => p.EventId == ev.Id))
        {
            var now = DateTimeOffset.UtcNow;
            var p1 = new Post { EventId = ev.Id, AuthorId = ana.Id, Category = "Dúvida", CreatedAt = now.AddMinutes(-40),
                Text = "Alguém vai assistir à palestra sobre IA aplicada ao varejo? Queria trocar ideias depois." };
            var p2 = new Post { EventId = ev.Id, AuthorId = bruno.Id, Category = "Opinião", CreatedAt = now.AddMinutes(-15),
                Text = "Ótimo painel de abertura! Recomendo passar no estande da Nimbus Cloud, conversa muito boa." };
            db.Posts.AddRange(p1, p2);
            db.Comments.Add(new Comment { PostId = p1.Id, AuthorId = bruno.Id, CreatedAt = now.AddMinutes(-30),
                Text = "Eu vou! Podemos nos encontrar no coffee break." });
            db.Reactions.Add(new Reaction { PostId = p1.Id, UserId = bruno.Id, Type = ReactionType.Like });
            db.Reactions.Add(new Reaction { PostId = p2.Id, UserId = ana.Id, Type = ReactionType.Insightful });
            await db.SaveChangesAsync();
        }

        await SeedMatchingDemoAsync(sp, db, ev, pwd, ana, bruno);
    }

    /// <summary>
    /// Dados fictícios para demonstrar taxonomia, matching e leads sem preencher à mão. Idempotente e
    /// aditivo: só cria o que falta, então também funciona num banco de demonstração já existente.
    /// </summary>
    private static async Task SeedMatchingDemoAsync(IServiceProvider sp, ApplicationDbContext db, Event ev, string pwd,
        ApplicationUser ana, ApplicationUser bruno)
    {
        // Taxonomia: bibliotecas de setor + dois temas próprios do evento
        async Task<InterestTag> Ensure(string key, string name, string? desc, Guid? parent, string[] synonyms)
        {
            var t = await db.InterestTags.FirstOrDefaultAsync(x => x.EventId == ev.Id && x.LibraryKey == key);
            if (t is not null) return t;
            t = new InterestTag { EventId = ev.Id, ParentId = parent, Name = name, Description = desc, LibraryKey = key,
                Synonyms = TaxonomyRules.NormalizeSynonyms(synonyms, name), Status = TagStatus.Approved };
            db.InterestTags.Add(t);
            await db.SaveChangesAsync();
            return t;
        }
        foreach (var sector in TaxonomyLibrary.Sectors.Where(s => s.Key is "industria" or "tecnologia"))
            foreach (var area in sector.Areas)
            {
                var a = await Ensure(area.Key, area.Name, null, null, []);
                foreach (var th in area.Themes) await Ensure(th.Key, th.Name, th.Description, a.Id, th.Synonyms);
            }
        var agroArea = await Ensure("demo.agro", "Agronegócio", null, null, []);
        var agroTag = await Ensure("demo.agro.digital", "Agricultura digital", "Monitoramento de lavouras e gestão de safra com dados.",
            agroArea.Id, ["agro", "lavoura", "safra", "agricultura de precisão"]);
        var energiaArea = await Ensure("demo.energia", "Energia", null, null, []);
        var solarTag = await Ensure("demo.energia.solar", "Energia solar", "Projetos fotovoltaicos e eficiência energética para empresas.",
            energiaArea.Id, ["fotovoltaica", "painéis solares", "energia renovável"]);

        InterestTag Tag(string key) => db.InterestTags.Local.First(t => t.EventId == ev.Id && t.LibraryKey == key);
        await db.InterestTags.Where(t => t.EventId == ev.Id).LoadAsync();

        // Empresas: cliente ideal e temas nas existentes; duas novas do setor industrial
        var ideal = new Dictionary<string, (string Ideal, string[] Tags)>
        {
            ["Nimbus Cloud"] = ("Startups e médias empresas que querem migrar ou operar sistemas em nuvem.", ["tec.infra.cloud", "tec.infra.ciber"]),
            ["AgroTech Sul"] = ("Produtores rurais e cooperativas que querem acompanhar lavouras e safra com dados.", ["demo.agro.digital"]),
            ["Verde Energia"] = ("Indústrias e comércios com alto consumo de energia que querem reduzir a conta de luz.", ["demo.energia.solar"]),
        };
        var extra = new[]
        {
            new Company { EventId = ev.Id, Name = "Prodtrack Sistemas", Segment = "Software industrial",
                Description = "Software de controle de produção e apontamento de ordens no chão de fábrica, integrado ao ERP.",
                ProductsServices = "MES, apontamento de produção, indicadores de eficiência (OEE) e integração com ERP.",
                BoothLocation = "Pavilhão C • Estande 06", IdealCustomer = "Indústrias de transformação, com gerente de produção ou de operações." },
            new Company { EventId = ev.Id, Name = "Sensa IoT", Segment = "Automação industrial",
                Description = "Sensores e monitoramento de máquinas em tempo real.",
                ProductsServices = "Sensores de vibração e temperatura, painel de paradas de máquina e manutenção preditiva.",
                BoothLocation = "Pavilhão C • Estande 12", IdealCustomer = "Fábricas que querem reduzir paradas de máquina e custo de manutenção." },
        };
        foreach (var c in extra)
            if (!await db.Companies.AnyAsync(x => x.EventId == ev.Id && x.Name == c.Name)) db.Companies.Add(c);
        await db.SaveChangesAsync();
        ideal["Prodtrack Sistemas"] = (extra[0].IdealCustomer!, ["ind.gestao.mes", "ind.gestao.erp"]);
        ideal["Sensa IoT"] = (extra[1].IdealCustomer!, ["ind.automacao.iot"]);

        foreach (var c in await db.Companies.Include(x => x.Tags).Where(x => x.EventId == ev.Id).ToListAsync())
        {
            if (!ideal.TryGetValue(c.Name, out var cfg)) continue;
            c.IdealCustomer ??= cfg.Ideal;
            if (c.Tags.Count == 0)
                foreach (var key in cfg.Tags) c.Tags.Add(new CompanyTag { CompanyId = c.Id, TagId = Tag(key).Id });
        }
        await db.SaveChangesAsync();

        // Visitantes fictícios com perfil no evento (consentimentos variados para demonstrar as regras)
        var diego = await EnsureUserAsync(sp, "diego@conexa.demo", "Diego Rocha", pwd, [Roles.Participant],
            new UserProfile { CompanyName = "Metalúrgica Rocha", JobTitle = "Gerente de produção", Area = "Indústria metalmecânica" },
            new ContactPrivacy { EmailVisible = true, ConsentGivenAt = DateTimeOffset.UtcNow });
        var eva = await EnsureUserAsync(sp, "eva@conexa.demo", "Eva Martins", pwd, [Roles.Participant],
            new UserProfile { CompanyName = "Fazenda Boa Safra", JobTitle = "Produtora rural", Area = "Agronegócio" });
        var felipe = await EnsureUserAsync(sp, "felipe@conexa.demo", "Felipe Costa", pwd, [Roles.Participant],
            new UserProfile { CompanyName = "Costa Têxtil", JobTitle = "Diretor", Area = "Indústria têxtil" });
        foreach (var u in new[] { diego, eva, felipe }) await EnsureMembershipAsync(db, ev.Id, u.Id, EventRole.Participant);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        void Profile(ApplicationUser u, string goal, VisitTimeframe tf, bool share, params string[] tagKeys)
        {
            if (db.ParticipantEventProfiles.Any(p => p.EventId == ev.Id && p.UserId == u.Id)) return;
            var p = new ParticipantEventProfile { EventId = ev.Id, UserId = u.Id, Goal = goal, Timeframe = tf,
                ConsentMatchingAt = now, ConsentShareWithExhibitorsAt = share ? now : null };
            foreach (var k in tagKeys) p.Tags.Add(new ParticipantTag { EventId = ev.Id, UserId = u.Id, TagId = Tag(k).Id });
            db.ParticipantEventProfiles.Add(p);
        }
        Profile(diego, "Quero automatizar o controle de produção da minha fábrica e acompanhar as paradas de máquina.",
            VisitTimeframe.ThisQuarter, share: true, "ind.gestao.mes");
        Profile(eva, "Procuro uma solução para monitorar minhas lavouras e planejar a safra com dados.",
            VisitTimeframe.ThisMonth, share: true);
        Profile(felipe, "Preciso reduzir a conta de energia da minha fábrica têxtil.",
            VisitTimeframe.Exploring, share: false); // aceita recomendações, mas NÃO o compartilhamento: não aparece como lead
        Profile(ana, "Quero migrar a infraestrutura da minha startup para a nuvem com segurança e bom custo.",
            VisitTimeframe.Now, share: true, "tec.infra.cloud");
        Profile(bruno, "Procuro clientes e parceiros na área de tecnologia.", VisitTimeframe.Exploring, share: false);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureMembershipAsync(ApplicationDbContext db, Guid eventId, Guid userId, EventRole role)
    {
        if (!await db.EventMemberships.AnyAsync(m => m.EventId == eventId && m.UserId == userId))
            db.EventMemberships.Add(new EventMembership { EventId = eventId, UserId = userId, RoleInEvent = role });
    }

    private static async Task<ApplicationUser> EnsureUserAsync(IServiceProvider sp, string email, string name,
        string password, string[] roles, UserProfile? profile = null, ContactPrivacy? privacy = null)
    {
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var existing = await users.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = name };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Seed: falha ao criar {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        await users.AddToRolesAsync(user, roles);

        profile ??= new UserProfile();
        profile.UserId = user.Id;
        db.UserProfiles.Add(profile);
        privacy ??= new ContactPrivacy();
        privacy.UserId = user.Id;
        db.ContactPrivacies.Add(privacy);
        await db.SaveChangesAsync();
        return user;
    }
}
