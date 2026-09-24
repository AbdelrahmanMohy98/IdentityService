using IdentityService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => new UserId(value))
            .ValueGeneratedNever();

        // Owned value objects: Email/Password are mapped as part of the Users
        // table (not separate tables) since they have no independent identity.
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(256).IsRequired();
            email.HasIndex(e => e.Value).IsUnique();
        });

        builder.OwnsOne(u => u.Password, password =>
        {
            password.Property(p => p.Value).HasColumnName("PasswordHash").IsRequired();
        });

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();

        // Roles stored via EF Core 8's native primitive-collection support.
        // SQL Server has no array type, so EF automatically serializes this as
        // a JSON string column — fine for this service's deliberately small,
        // closed role set (see Domain/Roles/Role.cs). The backing field is
        // private with no public settable CLR property, so it's configured
        // directly by field name via the generic Property<T> overload.
        builder.Property<List<string>>("_roles")
            .HasColumnName("Roles")
            .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.RefreshTokens)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_refreshTokens");

        builder.Ignore(u => u.DomainEvents);
    }
}
