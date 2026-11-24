namespace Conexa.Api.Tests;

public class CatalogCsvTests
{
    [Fact]
    public void Semicolon_file_with_accents_and_header_variations_is_read()
    {
        var csv = "﻿Empresa;Segmento;Produtos e Serviços;Cliente Ideal;Estande;Site;Temas;E-mail\n" +
                  "Prodtrack;Software;\"Controle de produção; apontamento\";Indústrias;B-06;https://p.example;ERP | IoT;a@p.com\n";
        var r = CatalogCsv.Parse(csv);
        Assert.Empty(r.FileErrors);
        var row = Assert.Single(r.Rows);
        Assert.Equal("Prodtrack", row.Name);
        Assert.Equal("Controle de produção; apontamento", row.Products); // aspas protegem o delimitador
        Assert.Equal("Indústrias", row.IdealCustomer);
        Assert.Equal(["ERP", "IoT"], row.Themes);
        Assert.Equal("a@p.com", row.Email);
        Assert.Empty(row.Problems);
    }

    [Fact]
    public void Comma_file_with_quoted_newlines_and_escaped_quotes()
    {
        var csv = "nome,descricao\r\n\"Acme \"\"Max\"\"\",\"linha 1\nlinha 2\"\r\nBeta,ok\r\n";
        var r = CatalogCsv.Parse(csv);
        Assert.Equal(2, r.Rows.Count);
        Assert.Equal("Acme \"Max\"", r.Rows[0].Name);
        Assert.Equal("linha 1\nlinha 2", r.Rows[0].Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("segmento,site\nx,y")]       // sem coluna de nome
    [InlineData("nome\n")]                   // só cabeçalho
    public void Unusable_files_report_a_file_error(string csv) => Assert.NotEmpty(CatalogCsv.Parse(csv).FileErrors);

    [Fact]
    public void Row_problems_are_flagged_and_blocking_ones_are_distinguished()
    {
        var csv = "nome,site,email,temas\n,https://ok.example,,\nA,ftp://x,sem-arroba,\nBeta,,,t1;t2;t3;t4\n";
        var r = CatalogCsv.Parse(csv);
        Assert.Equal(3, r.Rows.Count);
        Assert.True(CatalogCsv.IsBlocking(r.Rows[0]));  // sem nome
        Assert.True(CatalogCsv.IsBlocking(r.Rows[1]));  // site e e-mail inválidos
        Assert.False(CatalogCsv.IsBlocking(r.Rows[2])); // só aviso de excesso de temas
        Assert.Equal(3, r.Rows[2].Themes.Count);
        Assert.Single(r.Rows[2].Problems);
    }

    [Fact]
    public void Too_many_rows_is_rejected_and_blank_lines_are_ignored()
    {
        var many = "nome\n" + string.Join("\n", Enumerable.Range(0, CatalogCsv.MaxRows + 1).Select(i => $"E{i}"));
        Assert.NotEmpty(CatalogCsv.Parse(many).FileErrors);
        Assert.Equal(2, CatalogCsv.Parse("nome\nA\n\n,\nB\n").Rows.Count(r => r.Name.Length > 0));
    }
}

public class InviteRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Valid_invite_has_no_reason() => Assert.Null(new EventInvite().InvalidReason(Now));

    [Fact]
    public void Revoked_expired_and_exhausted_invites_are_invalid()
    {
        Assert.NotNull(new EventInvite { RevokedAt = Now }.InvalidReason(Now));
        Assert.NotNull(new EventInvite { ExpiresAt = Now }.InvalidReason(Now));          // expira no instante
        Assert.Null(new EventInvite { ExpiresAt = Now.AddSeconds(1) }.InvalidReason(Now));
        Assert.NotNull(new EventInvite { MaxUses = 2, Uses = 2 }.InvalidReason(Now));
        Assert.Null(new EventInvite { MaxUses = 2, Uses = 1 }.InvalidReason(Now));
    }

    [Fact]
    public void Codes_are_url_safe_unique_and_long_enough()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => EventInvite.NewCode()).ToList();
        Assert.Equal(200, codes.Distinct().Count());
        Assert.All(codes, c => { Assert.Equal(22, c.Length); Assert.Matches("^[A-Za-z0-9_-]+$", c); });
    }
}

public class ExhibitorImportServiceTests
{
    private static ApplicationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private const string Csv =
        "nome;segmento;temas;email\n" +
        "Prodtrack;Software;ERP;resp@p.com\n" +
        "Sensa;IoT;Sensores industriais;\n" +
        "prodtrack;Software;;\n" +        // duplicado no arquivo (caixa diferente)
        ";x;;\n";                         // sem nome

    private static async Task<(ApplicationDbContext Db, Guid Ev, InterestTag Erp)> Setup()
    {
        var db = NewDb();
        var ev = new Event { Name = "Feira" };
        var erp = new InterestTag { EventId = ev.Id, Name = "ERP e gestão", Status = TagStatus.Approved, Synonyms = ["erp"] };
        db.AddRange(ev, erp);
        await db.SaveChangesAsync();
        return (db, ev.Id, erp);
    }

    [Fact]
    public async Task Preview_reports_without_writing_anything()
    {
        var (db, ev, _) = await Setup();
        var rep = await new ExhibitorImportService(db).RunAsync(ev, Csv, commit: false, Guid.NewGuid(), default);
        Assert.False(rep.Committed);
        Assert.Equal((2, 1, 1), (rep.Created, rep.Skipped, rep.Invalid));
        Assert.Equal(0, await db.Companies.CountAsync());
        Assert.Equal(0, await db.InterestTags.CountAsync(t => t.Status == TagStatus.Suggested));
    }

    [Fact]
    public async Task Commit_creates_companies_links_known_themes_and_queues_unknown_ones()
    {
        var (db, ev, erp) = await Setup();
        var rep = await new ExhibitorImportService(db).RunAsync(ev, Csv, commit: true, Guid.NewGuid(), default);

        Assert.True(rep.Committed);
        Assert.Equal(2, await db.Companies.CountAsync());
        var prod = await db.Companies.SingleAsync(c => c.Name == "Prodtrack");
        Assert.Equal([erp.Id], await db.CompanyTags.Where(t => t.CompanyId == prod.Id).Select(t => t.TagId).ToListAsync()); // sinônimo "erp"
        var sug = await db.InterestTags.SingleAsync(t => t.Status == TagStatus.Suggested);
        Assert.Equal("Sensores industriais", sug.Name);
        Assert.Equal(["Sensores industriais"], rep.NewThemeSuggestions);
        Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.Action == "exhibitors.import"));
    }

    [Fact]
    public async Task Rerunning_the_same_file_creates_nothing_new()
    {
        var (db, ev, _) = await Setup();
        var svc = new ExhibitorImportService(db);
        await svc.RunAsync(ev, Csv, true, Guid.NewGuid(), default);
        var again = await svc.RunAsync(ev, Csv, true, Guid.NewGuid(), default);
        Assert.Equal(0, again.Created);
        Assert.Equal(2, await db.Companies.CountAsync());
        Assert.Equal(1, await db.InterestTags.CountAsync(t => t.Status == TagStatus.Suggested)); // sem sugestão repetida
    }

    [Fact]
    public async Task Representative_with_existing_account_is_linked_and_keeps_stronger_role()
    {
        var (db, ev, _) = await Setup();
        var resp = new ApplicationUser { UserName = "resp@p.com", Email = "resp@p.com", NormalizedEmail = "RESP@P.COM", DisplayName = "Resp" };
        db.Add(resp);
        db.Add(new EventMembership { EventId = ev, UserId = resp.Id, RoleInEvent = EventRole.Organizer });
        await db.SaveChangesAsync();

        await new ExhibitorImportService(db).RunAsync(ev, Csv, true, Guid.NewGuid(), default);
        var prod = await db.Companies.SingleAsync(c => c.Name == "Prodtrack");
        Assert.Single(await db.CompanyMemberships.Where(m => m.CompanyId == prod.Id && m.UserId == resp.Id).ToListAsync());
        Assert.Equal(EventRole.Organizer, (await db.EventMemberships.SingleAsync(m => m.UserId == resp.Id)).RoleInEvent);
    }

    [Fact]
    public async Task Existing_company_name_in_the_event_is_skipped()
    {
        var (db, ev, _) = await Setup();
        db.Companies.Add(new Company { EventId = ev, Name = "PRODTRACK" });
        await db.SaveChangesAsync();
        var rep = await new ExhibitorImportService(db).RunAsync(ev, "nome\nProdtrack\n", true, Guid.NewGuid(), default);
        Assert.Equal((0, 1), (rep.Created, rep.Skipped));
    }

    [Fact]
    public async Task File_errors_short_circuit()
    {
        var (db, ev, _) = await Setup();
        var rep = await new ExhibitorImportService(db).RunAsync(ev, "x,y\n1,2", true, Guid.NewGuid(), default);
        Assert.NotEmpty(rep.FileErrors);
        Assert.Equal(0, await db.Companies.CountAsync());
    }
}
