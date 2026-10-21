using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Permissions.Domain.PermissionRequests;

namespace Permissions.Infrastructure.Persistence;

internal sealed class PermissionRequestConfiguration : IEntityTypeConfiguration<PermissionRequest>
{
    public void Configure(EntityTypeBuilder<PermissionRequest> builder)
    {
        builder.ToTable("PermissionRequests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        // Guids sin clave foránea hacia otros esquemas (RD-03):
        // la integridad entre módulos se garantizará en los casos de uso.
        builder.Property(r => r.RequesterId).IsRequired();
        builder.Property(r => r.ApproverId);

        builder.Property(r => r.PermissionCode)
            .HasMaxLength(PermissionCodeRules.MaxLength)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.ResolvedAt);
        builder.Property(r => r.RejectionReason);

        // Solo puede existir una solicitud pendiente por usuario y código.
        builder.HasIndex(r => new { r.RequesterId, r.PermissionCode })
            .IsUnique()
            .HasFilter("\"Status\" = 'Pending'");

        // Token de concurrencia: la columna de sistema xmin de PostgreSQL.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Ignore(r => r.DomainEvents);
    }
}
