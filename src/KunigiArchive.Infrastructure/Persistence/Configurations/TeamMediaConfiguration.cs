using KunigiArchive.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Infrastructure.Persistence.Configurations;

internal sealed class TeamMediaConfiguration : IEntityTypeConfiguration<TeamMedia>
{
    public void Configure(EntityTypeBuilder<TeamMedia> builder)
    {
        builder.ToTable("TeamMedia");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Path)
            .IsRequired()
            .HasMaxLength(TeamMedia.PathMaxLength);

        builder.Property(m => m.FileName)
            .IsRequired()
            .HasMaxLength(TeamMedia.FileNameMaxLength);

        builder.Property(m => m.OriginalFileName)
            .IsRequired()
            .HasMaxLength(TeamMedia.FileNameMaxLength);
    }
}