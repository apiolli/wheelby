using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Permissions.Domain.PermissionGrants;
using Permissions.Domain.PermissionRequests;

namespace Permissions.Infrastructure.Persistence;

internal sealed class PermissionGrantConfiguration : IEntityTypeConfiguration<PermissionGrant>
{
    public void Configure(EntityTypeBuilder<PermissionGrant> builder)
    {
        builder.ToTable("PermissionGrants");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        // Referencia lógica al usuario: sin clave foránea hacia otros esquemas (RD-03).
        builder.Property(g => g.UserId).IsRequired();
        builder.Property(g => g.RequestId).IsRequired();

        builder.Property(g => g.PermissionCode)
            .HasMaxLength(PermissionCodeRules.MaxLength)
            .IsRequired();

        builder.Property(g => g.GrantedAt).IsRequired();

        // Un usuario no puede tener dos veces el mismo permiso.
        builder.HasIndex(g => new { g.UserId, g.PermissionCode }).IsUnique();
    }
}
