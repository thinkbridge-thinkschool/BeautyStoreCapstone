using BeautyStore.Api.Auth;
using BeautyStore.Api.Catalog;
using BeautyStore.Api.Orders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeautyStore.Api.Data;

public sealed class BeautyStoreDbContext(DbContextOptions<BeautyStoreDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Catalog.Product> Products => Set<Catalog.Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("dbo");

        // Move Identity tables into a dedicated schema to keep dbo clean.
        modelBuilder.Entity<ApplicationUser>()
            .ToTable("Users", "Identity");
        modelBuilder.Entity<ApplicationRole>()
            .ToTable("Roles", "Identity");
        modelBuilder.Entity<IdentityUserRole<string>>()
            .ToTable("UserRoles", "Identity");
        modelBuilder.Entity<IdentityUserClaim<string>>()
            .ToTable("UserClaims", "Identity");
        modelBuilder.Entity<IdentityUserLogin<string>>()
            .ToTable("UserLogins", "Identity");
        modelBuilder.Entity<IdentityUserToken<string>>()
            .ToTable("UserTokens", "Identity");
        modelBuilder.Entity<IdentityRoleClaim<string>>()
            .ToTable("RoleClaims", "Identity");

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories", "dbo");
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.Property(c => c.ImageUrl).HasMaxLength(500);
            e.HasIndex(c => c.Slug).IsUnique();
        });

        modelBuilder.Entity<Catalog.Product>(e =>
        {
            e.ToTable("Products", "dbo");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(256).IsRequired();
            e.Property(p => p.Brand).HasMaxLength(100).IsRequired();
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.ImageUrl).HasMaxLength(500);
            e.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => new { p.CategoryId, p.IsActive, p.IsFeatured });
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders", "dbo");
            e.HasKey(o => o.Id);
            e.Property(o => o.UserId).HasMaxLength(450).IsRequired();
            e.Property(o => o.ProductName).HasMaxLength(256).IsRequired();
            e.Property(o => o.Status).HasMaxLength(50).IsRequired();
            e.Property(o => o.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");
            e.HasIndex(o => o.UserId);
        });
    }
}
