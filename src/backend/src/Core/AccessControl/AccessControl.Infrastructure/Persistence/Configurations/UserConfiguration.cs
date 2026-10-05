using AccessControl.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessControl.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.FullName)
            .HasMaxLength(User.FullNameMaxLength)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasConversion(v => v.Value, v => Email.Create(v))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.ActivatedAt);

        builder.ComplexProperty(u => u.ActivationToken, token =>
        {
            token.Property(t => t.TokenHash).HasColumnName("ActivationTokenHash").HasMaxLength(64);
            token.Property(t => t.IssuedAt).HasColumnName("ActivationTokenIssuedAt");
            token.Property(t => t.ExpiresAt).HasColumnName("ActivationTokenExpiresAt");
            token.Property(t => t.UsedAt).HasColumnName("ActivationTokenUsedAt");
        });

        builder.ComplexProperty(u => u.PasswordResetToken, token =>
        {
            token.Property(t => t.TokenHash).HasColumnName("PasswordResetTokenHash").HasMaxLength(64);
            token.Property(t => t.IssuedAt).HasColumnName("PasswordResetTokenIssuedAt");
            token.Property(t => t.ExpiresAt).HasColumnName("PasswordResetTokenExpiresAt");
            token.Property(t => t.UsedAt).HasColumnName("PasswordResetTokenUsedAt");
        });

        builder.Property(u => u.FailedLoginAttempts).IsRequired();
        builder.Property(u => u.LockedUntil);
        builder.Property(u => u.DisabledAt);

        builder.Ignore(u => u.IsActive);
        builder.Ignore(u => u.IsDisabled);
        builder.Ignore(u => u.DomainEvents);
    }
}