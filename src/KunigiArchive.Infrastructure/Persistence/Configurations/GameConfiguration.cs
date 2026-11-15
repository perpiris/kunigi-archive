using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Infrastructure.Persistence.Configurations;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .ValueGeneratedNever();

        // One game per year; the old app only checked this in code (#23).
        builder.HasIndex(g => g.Year)
            .IsUnique();

        // 31.5 needs one decimal. Numbers are unique, but several games may have none.
        builder.Property(g => g.Order)
            .HasPrecision(4, 1);

        builder.HasIndex(g => g.Order)
            .IsUnique()
            .HasFilter("[Order] IS NOT NULL");

        builder.Property(g => g.Slug)
            .IsRequired()
            .HasMaxLength(Slug.MaxLength)
            .HasConversion(slug => slug.Value, value => Slug.Parse(value));

        builder.HasIndex(g => g.Slug)
            .IsUnique();

        builder.Property(g => g.OrderTitle)
            .IsRequired()
            .HasMaxLength(Game.OrderTitleMaxLength);

        builder.Property(g => g.Title).HasMaxLength(Game.TitleMaxLength);
        builder.Property(g => g.Note).HasMaxLength(Game.NoteMaxLength);
        builder.Property(g => g.Image).HasMaxLength(Game.ImageMaxLength);

        // Restrict: a team that hosted or won a game can't be deleted out from under it.
        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(g => g.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(g => g.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Media)
            .WithOne()
            .HasForeignKey(m => m.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}