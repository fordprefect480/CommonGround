using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogImage> BlogImages => Set<BlogImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.JoinedAt).HasDefaultValueSql("SYSUTCDATETIME()");
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
    }
}
