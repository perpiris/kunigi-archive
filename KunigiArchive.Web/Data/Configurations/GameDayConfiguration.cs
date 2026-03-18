using KunigiArchive.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Web.Data.Configurations;

public class GameDayConfiguration : IEntityTypeConfiguration<GameDay>
{
    public void Configure(EntityTypeBuilder<GameDay> builder)
    {
        builder.HasKey(x => x.GameDayId);

        builder.Property(x => x.Title)
            .HasMaxLength(255);

        builder.Property(x => x.IsEnabled).HasDefaultValue(false);

        builder.HasOne(x => x.GameDayType)
            .WithMany()
            .HasForeignKey(x => x.GameTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Game)
            .WithMany(x => x.GameDays)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Puzzles)
            .WithOne(x => x.GameDay)
            .HasForeignKey(x => x.GameDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Posts)
            .WithOne(x => x.GameDay)
            .HasForeignKey(x => x.GameDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}