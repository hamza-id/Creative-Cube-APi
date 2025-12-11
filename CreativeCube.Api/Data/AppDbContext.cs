using CreativeCube.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CreativeCube.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Blueprint> Blueprints => Set<Blueprint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        
        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(p => p.UserId);
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Blueprint configuration
        modelBuilder.Entity<Blueprint>(entity =>
        {
            entity.HasIndex(b => b.ProjectId);
            entity.HasOne(b => b.Project)
                .WithMany(p => p.Blueprints)
                .HasForeignKey(b => b.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Auto-update UpdatedAt on save
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.GetProperty("UpdatedAt") != null)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property("UpdatedAt")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is AppUser || e.Entity is Project || e.Entity is Blueprint);
        
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is AppUser user)
                {
                    user.CreatedAt = now;
                    user.UpdatedAt = now;
                }
                else if (entry.Entity is Project project)
                {
                    project.CreatedAt = now;
                    project.UpdatedAt = now;
                }
                else if (entry.Entity is Blueprint blueprint)
                {
                    blueprint.CreatedAt = now;
                    blueprint.UpdatedAt = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is AppUser user)
                {
                    user.UpdatedAt = now;
                }
                else if (entry.Entity is Project project)
                {
                    project.UpdatedAt = now;
                }
                else if (entry.Entity is Blueprint blueprint)
                {
                    blueprint.UpdatedAt = now;
                }
            }
        }
    }
}

