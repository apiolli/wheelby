using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Reviews;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Infrastructure.Persistence.Configurations;

// Tablas de apoyo del módulo. DocumentId es un Guid sin clave foránea hacia el
// esquema de Documents (RD-03).

internal sealed class ReservationStatusChangeConfiguration : IEntityTypeConfiguration<ReservationStatusChange>
{
    public void Configure(EntityTypeBuilder<ReservationStatusChange> builder)
    {
        builder.ToTable("ReservationStatusHistory");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.From).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.To).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(h => h.Actor).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(h => h.Reason).HasMaxLength(Reservation.CancellationReasonMaxLength);
        builder.Property(h => h.At).IsRequired();
        builder.HasIndex(h => new { h.ReservationId, h.At });
    }
}

internal sealed class ReservationDocumentConfiguration : IEntityTypeConfiguration<ReservationDocument>
{
    public void Configure(EntityTypeBuilder<ReservationDocument> builder)
    {
        builder.ToTable("ReservationDocuments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(d => new { d.ReservationId, d.DocumentId }).IsUnique();
        builder.HasOne<Reservation>().WithMany().HasForeignKey(d => d.ReservationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class VehiclePhotoConfiguration : IEntityTypeConfiguration<VehiclePhoto>
{
    public void Configure(EntityTypeBuilder<VehiclePhoto> builder)
    {
        builder.ToTable("VehiclePhotos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.HasIndex(p => new { p.VehicleId, p.DocumentId }).IsUnique();
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(p => p.VehicleId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.AuthorRole).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(Review.CommentMaxLength);
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        // Una reseña por actor por reserva; la base lo garantiza aunque dos peticiones lleguen juntas.
        builder.HasIndex(r => new { r.ReservationId, r.AuthorId }).IsUnique();
        builder.HasIndex(r => new { r.VehicleId, r.CreatedAt });
        builder.HasOne<Reservation>().WithMany().HasForeignKey(r => r.ReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", $"\"Rating\" BETWEEN {Review.RatingMin} AND {Review.RatingMax}"));
        builder.Ignore(r => r.DomainEvents);
    }
}
