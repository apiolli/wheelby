using Documents.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Documents.Infrastructure.Persistence;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        // Guids sin clave foránea hacia access_control (RD-03).
        builder.Property(d => d.OwnerId).IsRequired();
        builder.Property(d => d.DeletedBy);

        builder.Property(d => d.FileName)
            .HasMaxLength(Document.FileNameMaxLength)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(Document.ContentTypeMaxLength)
            .IsRequired();

        builder.Property(d => d.StoredFileName)
            .HasMaxLength(Document.StoredFileNameLength)
            .IsRequired();

        builder.Property(d => d.SizeBytes).IsRequired();
        builder.Property(d => d.UploadedAt).IsRequired();
        builder.Property(d => d.DeletedAt);

        builder.Ignore(d => d.IsDeleted);
        builder.Ignore(d => d.DomainEvents);

        // Sin filtro global de consulta: el filtro DeletedAt == null está en DocumentReadStore.
        builder.HasIndex(d => new { d.OwnerId, d.DeletedAt });
    }
}
