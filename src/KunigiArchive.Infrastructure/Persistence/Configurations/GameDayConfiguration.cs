using KunigiArchive.Domain.GameDays;
using KunigiArchive.Domain.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Infrastructure.Persistence.Configurations;

internal sealed class GameDayConfiguration : IEntityTypeConfiguration<GameDay>
{
    public void Configure(EntityTypeBuilder<GameDay> builder)
    {
        builder.ToTable("GameDays");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.Type)
            .HasConversion<byte>();

        builder.Property(d => d.Title).HasMaxLength(GameDay.TitleMaxLength);

        // A game's days go with it.
        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(d => d.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // One day of each kind per game.
        builder.HasIndex(d => new { d.GameId, d.Type })
            .IsUnique();

        builder.HasMany(d => d.Media)
            .WithOne()
            .HasForeignKey(m => m.GameDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Puzzles)
            .WithOne()
            .HasForeignKey(p => p.GameDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class GameDayMediaConfiguration : IEntityTypeConfiguration<GameDayMedia>
{
    public void Configure(EntityTypeBuilder<GameDayMedia> builder)
    {
        builder.ToTable("GameDayMedia");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Path)
            .IsRequired()
            .HasMaxLength(GameDayMedia.PathMaxLength);

        builder.Property(m => m.FileName)
            .IsRequired()
            .HasMaxLength(GameDayMedia.FileNameMaxLength);

        builder.Property(m => m.OriginalFileName)
            .IsRequired()
            .HasMaxLength(GameDayMedia.FileNameMaxLength);
    }
}

internal sealed class PuzzleConfiguration : IEntityTypeConfiguration<Puzzle>
{
    public void Configure(EntityTypeBuilder<Puzzle> builder)
    {
        builder.ToTable("Puzzles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        // Not unique: moving and compacting puzzles renumbers several rows in one save, and SQL Server checks a
        // unique index per statement. The GameDay aggregate keeps the numbers 1..n.
        builder.HasIndex(p => new { p.GameDayId, p.Order });

        builder.HasMany(p => p.Media)
            .WithOne()
            .HasForeignKey(m => m.PuzzleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PuzzleMediaConfiguration : IEntityTypeConfiguration<PuzzleMedia>
{
    public void Configure(EntityTypeBuilder<PuzzleMedia> builder)
    {
        builder.ToTable("PuzzleMedia");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Kind)
            .HasConversion<byte>();

        builder.Property(m => m.Path)
            .IsRequired()
            .HasMaxLength(PuzzleMedia.PathMaxLength);

        builder.Property(m => m.FileName)
            .IsRequired()
            .HasMaxLength(PuzzleMedia.FileNameMaxLength);

        builder.Property(m => m.OriginalFileName)
            .IsRequired()
            .HasMaxLength(PuzzleMedia.FileNameMaxLength);
    }
}
