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
    public DbSet<SecondaryMember> SecondaryMembers => Set<SecondaryMember>();
    public DbSet<MembershipPayment> MembershipPayments => Set<MembershipPayment>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<SentEmail> SentEmails => Set<SentEmail>();
    public DbSet<SentEmailRecipient> SentEmailRecipients => Set<SentEmailRecipient>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<BedLease> BedLeases => Set<BedLease>();
    public DbSet<BedRequest> BedRequests => Set<BedRequest>();
    public DbSet<BedLeasePayment> BedLeasePayments => Set<BedLeasePayment>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.JoinedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(u => u.IsSubscribedToMailingList).HasDefaultValue(true);

            b.HasMany(u => u.SecondaryMembers)
                .WithOne(s => s.User!)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SecondaryMember>(b =>
        {
            b.Property(s => s.FullName).HasMaxLength(200).IsRequired();
            b.HasIndex(s => s.UserId).HasDatabaseName("IX_SecondaryMember_UserId");
        });

        builder.Entity<MembershipPayment>(b =>
        {
            b.Property(p => p.StripeCheckoutSessionId).HasMaxLength(255).IsRequired();
            b.Property(p => p.StripePaymentIntentId).HasMaxLength(255);
            b.Property(p => p.Currency).HasMaxLength(3).IsRequired();
            b.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Filtered so offline (Manual) payments with an empty session id don't collide,
            // while Stripe sessions stay unique for idempotent webhook processing.
            b.HasIndex(p => p.StripeCheckoutSessionId)
                .IsUnique()
                .HasFilter("[StripeCheckoutSessionId] <> ''")
                .HasDatabaseName("IX_MembershipPayment_StripeCheckoutSessionId");
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
            b.Property(x => x.Location).HasMaxLength(200);
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

        builder.Entity<SiteSettings>(b =>
        {
            b.HasData(new SiteSettings { Id = 1, MembershipPriceCents = 2500, LeasedBedPriceCents = 8000 });
        });

        builder.Entity<Bed>(b =>
        {
            b.Property(x => x.Label).HasMaxLength(50).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(500);

            // Labels are unique among live beds; a soft-deleted bed's label can be reused.
            b.HasIndex(x => x.Label)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("IX_Bed_Label");

            // Soft-deleted beds drop out of every query automatically.
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasData(SeedBeds());
        });

        builder.Entity<BedLease>(b =>
        {
            b.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasOne(x => x.Bed)
                .WithMany(x => x.Leases)
                .HasForeignKey(x => x.BedId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.BedId).HasDatabaseName("IX_BedLease_BedId");
            b.HasIndex(x => x.UserId).HasDatabaseName("IX_BedLease_UserId");
        });

        builder.Entity<BedRequest>(b =>
        {
            b.Property(r => r.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(r => r.UserId).HasDatabaseName("IX_BedRequest_UserId");
            b.HasIndex(r => new { r.Status, r.CreatedAtUtc }).HasDatabaseName("IX_BedRequest_Status_CreatedAtUtc");
        });

        builder.Entity<BedLeasePayment>(b =>
        {
            b.Property(p => p.StripeCheckoutSessionId).HasMaxLength(255).IsRequired();
            b.Property(p => p.StripePaymentIntentId).HasMaxLength(255);
            b.Property(p => p.Currency).HasMaxLength(3).IsRequired();
            b.Property(p => p.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.BedLease)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.BedLeaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Filtered so many Manual payments (empty session id) don't collide, while
            // Stripe sessions stay unique for idempotent webhook processing.
            b.HasIndex(p => p.StripeCheckoutSessionId)
                .IsUnique()
                .HasFilter("[StripeCheckoutSessionId] <> ''")
                .HasDatabaseName("IX_BedLeasePayment_StripeCheckoutSessionId");

            b.HasIndex(p => p.BedLeaseId).HasDatabaseName("IX_BedLeasePayment_BedLeaseId");
        });
    }

    private static List<Bed> SeedBeds()
    {
        var beds = new List<Bed>();
        var id = 1;
        foreach (var section in new[] { "N", "S" })
        {
            for (var number = 1; number <= 16; number++)
            {
                beds.Add(new Bed { Id = id++, Label = $"{section}{number}", IsActive = true });
            }
        }
        return beds;
    }
}
