using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Infrastructure.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(Team.NameMaxLength);

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(Slug.MaxLength)
            .HasConversion(slug => slug.Value, value => Slug.Parse(value));

        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.Property(t => t.Facebook).HasMaxLength(500);
        builder.Property(t => t.Instagram).HasMaxLength(500);
        builder.Property(t => t.Youtube).HasMaxLength(500);
        builder.Property(t => t.Website).HasMaxLength(500);
        builder.Property(t => t.Image).HasMaxLength(500);
    }
}