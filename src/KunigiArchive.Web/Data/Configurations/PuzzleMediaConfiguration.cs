using KunigiArchive.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Web.Data.Configurations;

public class PuzzleMediaConfiguration : IEntityTypeConfiguration<PuzzleMedia>
{
    public void Configure(EntityTypeBuilder<PuzzleMedia> builder)
    {
        builder.HasKey(x => x.PuzzleMediaId);

        builder.Property(x => x.MediaType)
            .HasMaxLength(50);

        builder.Property(x => x.Path)
            .HasMaxLength(500);

        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(255);

        builder.Property(x => x.FileName)
            .HasMaxLength(255);

        builder.Property(x => x.TimeStamp)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.Puzzle)
            .WithMany(x => x.MediaFiles)
            .HasForeignKey(x => x.PuzzleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}