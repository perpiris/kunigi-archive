using KunigiArchive.Application.Abstractions;
using KunigiArchive.Domain.Teams;
using KunigiArchive.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence;

public sealed class KunigiArchiveDbContext : IdentityDbContext<AppUser, AppRole, Guid>, IUnitOfWork
{
    public KunigiArchiveDbContext(DbContextOptions<KunigiArchiveDbContext> options) : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KunigiArchiveDbContext).Assembly);
    }
}