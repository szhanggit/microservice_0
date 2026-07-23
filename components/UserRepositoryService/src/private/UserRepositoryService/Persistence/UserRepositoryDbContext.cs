using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Constants;
using UserRepositoryService.Entities;

namespace UserRepositoryService.Persistence;

public sealed class UserRepositoryDbContext(DbContextOptions<UserRepositoryDbContext> options) : DbContext(options)
{
    public DbSet<UserInfo> Users => Set<UserInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserInfo>(entity =>
        {
            entity.ToTable("UserInfo");
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.UserId).ValueGeneratedNever();
            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(UserValidationConstants.FirstNameMaxLength);
            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(UserValidationConstants.LastNameMaxLength);
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(UserValidationConstants.EmailMaxLength);
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}
