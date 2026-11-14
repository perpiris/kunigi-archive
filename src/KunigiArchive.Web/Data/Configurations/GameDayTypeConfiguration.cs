using KunigiArchive.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Web.Data.Configurations;

public class GameDayTypeConfiguration : IEntityTypeConfiguration<GameDayType>
{
    public void Configure(EntityTypeBuilder<GameDayType> builder)
    {
        builder.HasKey(x => x.GameDayTypeId);

        builder.Property(x => x.Label)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(100)
            .IsRequired();
    }
}