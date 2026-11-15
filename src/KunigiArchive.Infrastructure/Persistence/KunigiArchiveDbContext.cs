using KunigiArchive.Application.Abstractions;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Posts;
using KunigiArchive.Domain.Teams;
using KunigiArchive.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KunigiArchive.Infrastructure.Persistence;

public sealed class KunigiArchiveDbContext : IdentityDbContext<AppUser, AppRole, Guid>, IUnitOfWork
{
    public KunigiArchiveDbContext(DbContextOptions<KunigiArchiveDbContext> options) : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Post> Posts => Set<Post>();

    // Identity's stores use this same scoped context, so their saves join the transaction too.
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await Database.BeginTransactionAsync(cancellationToken);
        await work(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KunigiArchiveDbContext).Assembly);
    }
}