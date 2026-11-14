using KunigiArchive.Domain.Teams;
using KunigiArchive.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Infrastructure.Persistence.Configurations;

internal sealed class TeamManagerConfiguration : IEntityTypeConfiguration<TeamManager>
{
    public void Configure(EntityTypeBuilder<TeamManager> builder)
    {
        builder.ToTable("TeamManagers");

        builder.HasKey(m => new { m.TeamId, m.UserId });

        // The domain only knows the user's id; the foreign key to the Identity table lives here.
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}