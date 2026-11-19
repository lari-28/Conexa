using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Conexa.Api.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.IsActive);
    }
}

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> b)
    {
        b.HasKey(x => x.UserId);
        b.HasOne(x => x.User).WithOne(u => u.Profile).HasForeignKey<UserProfile>(x => x.UserId);
        b.Property(x => x.PhotoUrl).HasMaxLength(500);
        b.Property(x => x.CompanyName).HasMaxLength(150);
        b.Property(x => x.JobTitle).HasMaxLength(150);
        b.Property(x => x.Area).HasMaxLength(100);
        b.Property(x => x.Bio).HasMaxLength(1000);
        b.Property(x => x.LookingFor).HasMaxLength(500);
        b.Property(x => x.Offers).HasMaxLength(500);
        b.HasIndex(x => x.Area);
        b.HasIndex(x => x.Interests).HasMethod("gin");
    }
}

public class ContactPrivacyConfiguration : IEntityTypeConfiguration<ContactPrivacy>
{
    public void Configure(EntityTypeBuilder<ContactPrivacy> b)
    {
        b.HasKey(x => x.UserId);
        b.HasOne(x => x.User).WithOne(u => u.Privacy).HasForeignKey<ContactPrivacy>(x => x.UserId);
        b.Property(x => x.Phone).HasMaxLength(30);
    }
}

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Location).HasMaxLength(200);
        b.Property(x => x.MainThemes).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.Status);
    }
}

public class EventMembershipConfiguration : IEntityTypeConfiguration<EventMembership>
{
    public void Configure(EntityTypeBuilder<EventMembership> b)
    {
        b.HasKey(x => new { x.EventId, x.UserId });
        b.HasOne(x => x.Event).WithMany(e => e.Memberships).HasForeignKey(x => x.EventId);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.RoleInEvent).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.UserId);
    }
}

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Segment).HasMaxLength(100);
        b.Property(x => x.ProductsServices).HasMaxLength(2000);
        b.Property(x => x.BoothLocation).HasMaxLength(100);
        b.Property(x => x.Website).HasMaxLength(300);
        b.Property(x => x.IdealCustomer).HasMaxLength(1000);
        b.Property(x => x.CheckInCode).HasMaxLength(40);
        b.HasIndex(x => x.CheckInCode).IsUnique();
        b.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId);
        b.HasIndex(x => x.EventId);
    }
}

public class CompanyMembershipConfiguration : IEntityTypeConfiguration<CompanyMembership>
{
    public void Configure(EntityTypeBuilder<CompanyMembership> b)
    {
        b.HasKey(x => new { x.CompanyId, x.UserId });
        b.HasOne(x => x.Company).WithMany(c => c.Members).HasForeignKey(x => x.CompanyId);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Title).HasMaxLength(100);
    }
}

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Category).HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId);
        b.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.EventId, x.CreatedAt });
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> b)
    {
        b.Property(x => x.Text).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Post).WithMany(p => p.Comments).HasForeignKey(x => x.PostId);
        b.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PostId, x.CreatedAt });
    }
}

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> b)
    {
        b.HasKey(x => new { x.PostId, x.UserId });
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Post).WithMany(p => p.Reactions).HasForeignKey(x => x.PostId);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.EventId, x.Status });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(x => x.Action).HasMaxLength(60).IsRequired();
        b.Property(x => x.TargetType).HasMaxLength(30).IsRequired();
        b.Property(x => x.Details).HasMaxLength(500);
        b.HasIndex(x => x.CreatedAt);
    }
}

public class InterestTagConfiguration : IEntityTypeConfiguration<InterestTag>
{
    public void Configure(EntityTypeBuilder<InterestTag> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(300);
        b.Property(x => x.LibraryKey).HasMaxLength(100);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId);
        b.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.EventId, x.Status });
        b.HasIndex(x => new { x.EventId, x.ParentId, x.Name }).IsUnique();
    }
}

public class CompanyTagConfiguration : IEntityTypeConfiguration<CompanyTag>
{
    public void Configure(EntityTypeBuilder<CompanyTag> b)
    {
        b.HasKey(x => new { x.CompanyId, x.TagId });
        b.HasOne(x => x.Company).WithMany(c => c.Tags).HasForeignKey(x => x.CompanyId);
        b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ParticipantEventProfileConfiguration : IEntityTypeConfiguration<ParticipantEventProfile>
{
    public void Configure(EntityTypeBuilder<ParticipantEventProfile> b)
    {
        b.HasKey(x => new { x.EventId, x.UserId });
        b.Property(x => x.Goal).HasMaxLength(1000);
        b.Property(x => x.Timeframe).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ParticipantTagConfiguration : IEntityTypeConfiguration<ParticipantTag>
{
    public void Configure(EntityTypeBuilder<ParticipantTag> b)
    {
        b.HasKey(x => new { x.EventId, x.UserId, x.TagId });
        b.HasOne(x => x.Profile).WithMany(p => p.Tags).HasForeignKey(x => new { x.EventId, x.UserId });
        b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MatchScoreConfiguration : IEntityTypeConfiguration<MatchScore>
{
    public void Configure(EntityTypeBuilder<MatchScore> b)
    {
        b.HasKey(x => new { x.EventId, x.UserId, x.CompanyId });
        b.Property(x => x.Band).HasMaxLength(10);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.EventId, x.CompanyId, x.Score });
    }
}

public class EventInviteConfiguration : IEntityTypeConfiguration<EventInvite>
{
    public void Configure(EntityTypeBuilder<EventInvite> b)
    {
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.EventId);
    }
}

public class InteractionConfiguration : IEntityTypeConfiguration<Interaction>
{
    public void Configure(EntityTypeBuilder<Interaction> b)
    {
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.EventId, x.CompanyId, x.Type });
        b.HasIndex(x => new { x.EventId, x.UserId, x.CompanyId, x.Type });
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.Property(x => x.DedupeKey).HasMaxLength(200).IsRequired();
        b.Property(x => x.Title).HasMaxLength(150).IsRequired();
        b.Property(x => x.Body).HasMaxLength(500).IsRequired();
        b.Property(x => x.Link).HasMaxLength(200);
        b.HasIndex(x => new { x.UserId, x.DedupeKey }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.ReadAt });
    }
}
