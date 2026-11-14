using KunigiArchive.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Web.Data.Configurations;

public class GameDayMediaConfiguration : IEntityTypeConfiguration<GameDayMedia>
{
    public void Configure(EntityTypeBuilder<GameDayMedia> builder)
    {
        builder.HasKey(x => x.GameDayMediaId);

        builder.Property(x => x.Path)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.TimeStamp)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.GameDay)
            .WithMany(x => x.GameDayMedia)
            .HasForeignKey(x => x.GameDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}