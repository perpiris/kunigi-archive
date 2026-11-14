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

        builder.Property(t => t.Facebook).HasMaxLength(Team.UrlMaxLength);
        builder.Property(t => t.Instagram).HasMaxLength(Team.UrlMaxLength);
        builder.Property(t => t.Youtube).HasMaxLength(Team.UrlMaxLength);
        builder.Property(t => t.Website).HasMaxLength(Team.UrlMaxLength);
        builder.Property(t => t.Image).HasMaxLength(TeamMedia.PathMaxLength);

        builder.HasMany(t => t.Managers)
            .WithOne()
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Managers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Media)
            .WithOne()
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Media)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}