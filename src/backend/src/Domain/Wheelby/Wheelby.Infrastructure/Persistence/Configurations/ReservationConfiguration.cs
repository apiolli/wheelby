using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wheelby.Domain.Reservations;

namespace Wheelby.Infrastructure.Persistence.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        // Guids sin clave foránea hacia otros esquemas (RD-03):
        // la integridad entre módulos se garantizará en los casos de uso.
        builder.Property(r => r.VehicleId).IsRequired();
        builder.Property(r => r.RenterId).IsRequired();

        builder.Property(r => r.StartDate).IsRequired();
        builder.Property(r => r.EndDate).IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CancellationReason)
            .HasMaxLength(Reservation.CancellationReasonMaxLength);

        builder.Property(r => r.CreatedAt).IsRequired();

        builder.Ignore(r => r.DomainEvents);
    }
}
