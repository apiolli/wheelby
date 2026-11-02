using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Infrastructure.Persistence.Configurations;

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        // Sin claves foráneas hacia otros esquemas (RD-03): OwnerId es un Guid cuyo dueño vive en AccessControl.
        builder.Property(v => v.OwnerId).IsRequired();

        builder.Property(v => v.Brand)
            .IsRequired()
            .HasMaxLength(Vehicle.BrandMaxLength);

        builder.Property(v => v.Model)
            .IsRequired()
            .HasMaxLength(Vehicle.ModelMaxLength);

        builder.Property(v => v.Year).IsRequired();

        builder.Property(v => v.Plate)
            .IsRequired()
            .HasMaxLength(Vehicle.PlateMaxLength);

        builder.HasIndex(v => v.Plate).IsUnique();

        builder.Property(v => v.PricePerDay)
            .IsRequired()
            .HasColumnType("numeric(12,2)");

        builder.Property(v => v.CreatedAt).IsRequired();

        builder.HasIndex(v => v.OwnerId);

        builder.Ignore(v => v.DomainEvents);
    }
}
