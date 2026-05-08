using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ToolLib.Models;

namespace ToolLib.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tool> Tools { get; set; }
    public DbSet<UserRequest> UserRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Tool>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.HasOne(t => t.Owner)
                  .WithMany(u => u.Tools)
                  .HasForeignKey(t => t.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Email).IsRequired().HasMaxLength(256);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Address).IsRequired().HasMaxLength(500);
            entity.Property(r => r.Message).HasMaxLength(1000);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(200);
            entity.Property(u => u.Address).HasMaxLength(500);
        });
    }
}
