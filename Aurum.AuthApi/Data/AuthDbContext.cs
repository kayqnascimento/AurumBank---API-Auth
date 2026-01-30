using Microsoft.EntityFrameworkCore;
using Aurum.AuthApi.Models;

namespace Aurum.AuthApi.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<IdentityUser> Users => Set<IdentityUser>();

    public DbSet<CoreCustomer> Customers => Set<CoreCustomer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUser>(entity =>
        {
            entity.ToTable("users", "identity");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Cpf).HasColumnName("cpf");
            entity.Property(e => e.CpfHash).HasColumnName("cpf_hash");
            entity.Property(e => e.CpfLast4).HasColumnName("cpf_last4");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<CoreCustomer>(entity =>
        {
            entity.ToTable("customers", "core");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
    }

}
