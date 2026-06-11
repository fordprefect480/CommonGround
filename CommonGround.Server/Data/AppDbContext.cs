using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogImage> BlogImages => Set<BlogImage>();
    public DbSet<InstagramPost> InstagramPosts => Set<InstagramPost>();
    public DbSet<CommunityEvent> CommunityEvents => Set<CommunityEvent>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<SentEmail> SentEmails => Set<SentEmail>();
    public DbSet<SentEmailRecipient> SentEmailRecipients => Set<SentEmailRecipient>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.JoinedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(u => u.IsSubscribedToMailingList).HasDefaultValue(true);
        });

        builder.Entity<BlogCategory>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(50).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(50).IsRequired();
            b.HasIndex(x => x.Slug).IsUnique();

            b.HasData(
                new BlogCategory { Id = 1, Name = "Newsletters", Slug = "newsletters" },
                new BlogCategory { Id = 2, Name = "Events", Slug = "events" },
                new BlogCategory { Id = 3, Name = "How-to", Slug = "how-to" },
                new BlogCategory { Id = 4, Name = "Announcements", Slug = "announcements" });
        });

        builder.Entity<BlogImage>(b =>
        {
            b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Bytes).IsRequired();
            b.Property(x => x.OriginalFileName).HasMaxLength(255);
            b.HasOne(x => x.UploadedBy)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BlogPost>(b =>
        {
            b.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Excerpt).HasMaxLength(500);
            b.Property(x => x.BodyHtml).IsRequired();
            b.Property(x => x.AuthorName).HasMaxLength(100).IsRequired();

            b.HasIndex(x => x.Slug).IsUnique();
            b.HasIndex(x => new { x.Status, x.PublishedAt }).HasDatabaseName("IX_BlogPost_Status_PublishedAt");

            b.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.FeaturedImage)
                .WithMany()
                .HasForeignKey(x => x.FeaturedImageId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<InstagramPost>(b =>
        {
            b.Property(x => x.EmbedHtml).IsRequired();

            b.HasIndex(x => x.DisplayOrder).HasDatabaseName("IX_InstagramPost_DisplayOrder");

            b.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CommunityEvent>(b =>
        {
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Body).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Url).HasMaxLength(500);
            b.Property(x => x.Tone).HasMaxLength(16).IsRequired();

            b.Property(x => x.StartUtc).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            b.Property(x => x.EndUtc).HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : default(DateTime?));

            b.HasIndex(x => x.StartUtc).HasDatabaseName("IX_CommunityEvent_StartUtc");
            b.HasIndex(x => x.DisplayOrder).HasDatabaseName("IX_CommunityEvent_DisplayOrder");

            b.HasOne(x => x.FeaturedImage)
                .WithMany()
                .HasForeignKey(x => x.FeaturedImageId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Activity>(b =>
        {
            b.Property(a => a.OccurredAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(a => a.ActivityType).HasMaxLength(64).IsRequired();
            b.Property(a => a.ActorUserId).HasMaxLength(450);
            b.Property(a => a.ActorEmailSnapshot).HasMaxLength(256);
            b.Property(a => a.Summary).HasMaxLength(400).IsRequired();
            b.Property(a => a.TargetType).HasMaxLength(64);
            b.Property(a => a.TargetId).HasMaxLength(64);

            b.HasOne(a => a.Actor)
                .WithMany()
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(a => a.OccurredAt)
                .IsDescending()
                .HasDatabaseName("IX_Activity_OccurredAt");

            b.HasIndex(a => new { a.ActorUserId, a.OccurredAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_Activity_Actor_OccurredAt");

            b.HasIndex(a => new { a.ActivityType, a.OccurredAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_Activity_Type_OccurredAt");
        });

        builder.Entity<SentEmail>(b =>
        {
            b.Property(e => e.SentAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(e => e.Subject).HasMaxLength(200).IsRequired();
            b.Property(e => e.HtmlBody).IsRequired();
            b.Property(e => e.TextBody).IsRequired();
            b.Property(e => e.SenderUserId).HasMaxLength(450);
            b.Property(e => e.SenderEmailSnapshot).HasMaxLength(256);

            b.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey(e => e.SenderUserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(e => e.SentAt)
                .IsDescending()
                .HasDatabaseName("IX_SentEmail_SentAt");
        });

        builder.Entity<SentEmailRecipient>(b =>
        {
            b.Property(r => r.Email).HasMaxLength(256).IsRequired();
            b.Property(r => r.UserId).HasMaxLength(450);
            b.Property(r => r.ErrorMessage).HasMaxLength(1000);

            b.HasOne(r => r.SentEmail)
                .WithMany(e => e.Recipients)
                .HasForeignKey(r => r.SentEmailId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(r => r.SentEmailId).HasDatabaseName("IX_SentEmailRecipient_SentEmailId");
        });
    }
}
