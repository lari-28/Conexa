using System.Text;
using System.Text.Json.Serialization;
using Conexa.Api.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Overrides locais opcionais (não versionados): appsettings.Local.json
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ---------- Configuração ----------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.Configure<RegistrationOptions>(builder.Configuration.GetSection("Registration"));

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key ?? "") < 32)
    throw new InvalidOperationException(
        "Jwt:Key ausente ou curta (mínimo 32 bytes). Defina via user-secrets ou variável de ambiente Jwt__Key.");

// ---------- Banco e Identity ----------
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentityCore<ApplicationUser>(o =>
    {
        o.User.RequireUniqueEmail = true;                 // RN01
        o.Password.RequiredLength = 8;
        o.Password.RequireDigit = true;
        o.Password.RequireLowercase = true;
        o.Password.RequireUppercase = true;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ---------- Autenticação (JWT) ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwt.Issuer,
            ValidateAudience = true, ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
        o.Events = new JwtBearerEvents
        {
            // Contas suspensas perdem acesso imediatamente, mesmo com token ainda válido.
            OnTokenValidated = async ctx =>
            {
                var id = ctx.Principal?.GetUserId() ?? Guid.Empty;
                var db = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var active = await db.Users.AsNoTracking().AnyAsync(u => u.Id == id && u.IsActive);
                if (!active) ctx.Fail("Conta inexistente ou suspensa.");
            }
        };
    });

// ---------- Autorização (RNF05: validada no backend) ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IEventAccessService, EventAccessService>();
builder.Services.AddScoped<IAuthorizationHandler, EventAccessHandler>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.EventMember, p => p.RequireAuthenticatedUser().AddRequirements(new EventAccessRequirement(false)))
    .AddPolicy(Policies.EventOrganizer, p => p.RequireAuthenticatedUser().AddRequirements(new EventAccessRequirement(true)));

// ---------- CORS ----------
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    // Em desenvolvimento aceita qualquer origem (inclui acesso pela rede local, ex.: http://192.168.0.10:5173).
    if (builder.Environment.IsDevelopment()) p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));

// ---------- MVC / Swagger ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CONEXA API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Cole o token retornado por POST /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});

var app = builder.Build();

await DbInitializer.InitializeAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection(); // em desenvolvimento/LAN o acesso é HTTP
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();
