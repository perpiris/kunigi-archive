using KunigiArchive.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Web.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(x => x.GameId);

        builder.Property(x => x.OrderTitle)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Slug).HasMaxLength(255);

        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Image).HasMaxLength(255);

        builder.HasOne(x => x.Host)
            .WithMany(x => x.HostedGames)
            .HasForeignKey(x => x.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Winner)
            .WithMany(x => x.WonGames)
            .HasForeignKey(x => x.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.GameDays)
            .WithOne(x => x.Game)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}