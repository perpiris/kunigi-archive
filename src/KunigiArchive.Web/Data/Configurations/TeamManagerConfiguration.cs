using KunigiArchive.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Web.Data.Configurations;

public class TeamManagerConfiguration : IEntityTypeConfiguration<TeamManager>
{
    public void Configure(EntityTypeBuilder<TeamManager> builder)
    {
        builder.HasKey(x => new { x.TeamId, x.AppUserId });

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.AppUserId)
            .IsRequired();

        builder.HasOne(x => x.Team)
            .WithMany(x => x.Managers)
            .HasForeignKey(x => x.TeamId);

        builder.HasOne(x => x.AppUser)
            .WithMany(x => x.TeamManagers)
            .HasForeignKey(x => x.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}