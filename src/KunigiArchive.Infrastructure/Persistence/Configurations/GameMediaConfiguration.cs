using KunigiArchive.Domain.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Infrastructure.Persistence.Configurations;

internal sealed class GameMediaConfiguration : IEntityTypeConfiguration<GameMedia>
{
    public void Configure(EntityTypeBuilder<GameMedia> builder)
    {
        builder.ToTable("GameMedia");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Path)
            .IsRequired()
            .HasMaxLength(GameMedia.PathMaxLength);

        builder.Property(m => m.FileName)
            .IsRequired()
            .HasMaxLength(GameMedia.FileNameMaxLength);

        builder.Property(m => m.OriginalFileName)
            .IsRequired()
            .HasMaxLength(GameMedia.FileNameMaxLength);
    }
}