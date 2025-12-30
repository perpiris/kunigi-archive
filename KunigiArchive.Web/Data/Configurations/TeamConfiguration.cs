using KunigiArchive.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Web.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(x => x.TeamId);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Slug).HasMaxLength(255);

        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.Property(x => x.Facebook).HasMaxLength(255);
        builder.Property(x => x.Instagram).HasMaxLength(255);
        builder.Property(x => x.Youtube).HasMaxLength(255);
        builder.Property(x => x.Website).HasMaxLength(255);

        builder.Property(x => x.Image).HasMaxLength(255);

        builder.HasMany(x => x.Managers)
            .WithOne(x => x.Team)
            .HasForeignKey(x => x.TeamId);

        builder.HasMany(x => x.MediaFiles)
            .WithOne(x => x.Team)
            .HasForeignKey(x => x.TeamId);

        builder.HasMany(x => x.HostedGames)
            .WithOne(x => x.Host)
            .HasForeignKey(x => x.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.WonGames)
            .WithOne(x => x.Winner)
            .HasForeignKey(x => x.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}