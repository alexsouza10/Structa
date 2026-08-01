using Microsoft.EntityFrameworkCore;
using Structa.Core.Preferences;

namespace Structa.Persistence;

public sealed class StructaDbContext(DbContextOptions<StructaDbContext> options) : DbContext(options)
{
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.ToTable("UserPreferences");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Theme)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
