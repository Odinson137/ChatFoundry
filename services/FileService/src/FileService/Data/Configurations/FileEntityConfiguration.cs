using FileService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Data.Configurations;

public class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.ToTable("files");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Key)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(f => f.OriginalFileName)
            .HasMaxLength(512);

        builder.Property(f => f.ContentType)
            .HasMaxLength(128);

        builder.HasIndex(f => f.CompanyId);
        builder.HasIndex(f => f.UploadedClientId);
        builder.HasIndex(f => f.UploadedByUserId);
        builder.HasIndex(f => f.Key).IsUnique();
    }
}
