using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Conexa.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<ContactPrivacy> ContactPrivacies => Set<ContactPrivacy>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventMembership> EventMemberships => Set<EventMembership>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyMembership> CompanyMemberships => Set<CompanyMembership>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<InterestTag> InterestTags => Set<InterestTag>();
    public DbSet<CompanyTag> CompanyTags => Set<CompanyTag>();
    public DbSet<ParticipantEventProfile> ParticipantEventProfiles => Set<ParticipantEventProfile>();
    public DbSet<ParticipantTag> ParticipantTags => Set<ParticipantTag>();
    public DbSet<MatchScore> MatchScores => Set<MatchScore>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
