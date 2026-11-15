using KunigiArchive.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Infrastructure.Persistence.Configurations;

internal sealed class PostMediaConfiguration : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.ToTable("PostMedia");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Path)
            .IsRequired()
            .HasMaxLength(PostMedia.PathMaxLength);

        builder.Property(m => m.FileName)
            .IsRequired()
            .HasMaxLength(PostMedia.FileNameMaxLength);

        builder.Property(m => m.OriginalFileName)
            .IsRequired()
            .HasMaxLength(PostMedia.FileNameMaxLength);
    }
}