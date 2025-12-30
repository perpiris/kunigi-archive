using KunigiArchive.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Web.Data.Configurations;

public class PuzzleConfiguration : IEntityTypeConfiguration<Puzzle>
{
    public void Configure(EntityTypeBuilder<Puzzle> builder)
    {
        builder.HasKey(x => x.PuzzleId);

        builder.HasOne(x => x.GameDay)
            .WithMany(x => x.Puzzles)
            .HasForeignKey(x => x.GameDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MediaFiles)
            .WithOne(x => x.Puzzle)
            .HasForeignKey(x => x.PuzzleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}