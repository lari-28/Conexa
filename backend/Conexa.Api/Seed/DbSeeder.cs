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
