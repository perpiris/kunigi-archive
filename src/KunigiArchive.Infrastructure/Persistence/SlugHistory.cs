using KunigiArchive.Application.Common;
using KunigiArchive.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Infrastructure.Persistence;

// A slug a team or game used to have. Not part of the domain: only addresses depend on it.
internal sealed class SlugRedirect
{
    public Guid Id { get; set; }
    public SlugOwner Owner { get; set; }
    public string Slug { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class SlugRedirectConfiguration : IEntityTypeConfiguration<SlugRedirect>
{
    public void Configure(EntityTypeBuilder<SlugRedirect> builder)
    {
        builder.ToTable("SlugRedirects");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Owner).HasConversion<byte>();
        builder.Property(r => r.Slug).IsRequired().HasMaxLength(Slug.MaxLength);

        // An old address leads to one place: the latest owner of it.
        builder.HasIndex(r => new { r.Owner, r.Slug }).IsUnique();

        // No foreign key: a redirect to a deleted team or game is just not followed (FindCurrentSlugAsync finds nothing).
    }
}

internal sealed class SlugHistory : ISlugHistory
{
    private readonly KunigiArchiveDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public SlugHistory(KunigiArchiveDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task RecordAsync(SlugOwner owner, string previous, Guid ownerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(previous) || previous.Length > Slug.MaxLength)
        {
            return;
        }

        // Looked up in the tracked rows and the database alike; the save is the caller's.
        SlugRedirect? existing = _dbContext.SlugRedirects.Local.FirstOrDefault(r => r.Owner == owner && r.Slug == previous)
            ?? await _dbContext.SlugRedirects.FirstOrDefaultAsync(r => r.Owner == owner && r.Slug == previous, cancellationToken);

        if (existing is not null)
        {
            existing.OwnerId = ownerId;
            existing.CreatedAt = _timeProvider.GetUtcNow();
            return;
        }

        _dbContext.SlugRedirects.Add(new SlugRedirect
        {
            Id = Guid.CreateVersion7(),
            Owner = owner,
            Slug = previous,
            OwnerId = ownerId,
            CreatedAt = _timeProvider.GetUtcNow()
        });
    }

    public async Task<string?> FindCurrentSlugAsync(SlugOwner owner, string previous, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(previous) || previous.Length > Slug.MaxLength)
        {
            return null;
        }

        Guid? ownerId = await _dbContext.SlugRedirects.AsNoTracking()
            .Where(r => r.Owner == owner && r.Slug == previous)
            .Select(r => (Guid?)r.OwnerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerId is null)
        {
            return null;
        }

        string? current = owner switch
        {
            SlugOwner.Team => await _dbContext.Teams.AsNoTracking()
                .Where(t => t.Id == ownerId).Select(t => t.Slug.Value).FirstOrDefaultAsync(cancellationToken),
            SlugOwner.Game => await _dbContext.Games.AsNoTracking()
                .Where(g => g.Id == ownerId).Select(g => g.Slug.Value).FirstOrDefaultAsync(cancellationToken),
            _ => null
        };

        // Never redirect an address to itself.
        return current == previous ? null : current;
    }
}
