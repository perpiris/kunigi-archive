using KunigiArchive.Application.Abstractions;
using KunigiArchive.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence;

public sealed class KunigiArchiveDbContext : DbContext, IUnitOfWork
{
    public KunigiArchiveDbContext(DbContextOptions<KunigiArchiveDbContext> options) : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KunigiArchiveDbContext).Assembly);
    }
}