using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KunigiArchive.Infrastructure.Persistence.Configurations;

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Title).HasMaxLength(Post.TitleMaxLength);
        builder.Property(p => p.Content).IsRequired();

        // A game's posts go with it.
        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(p => p.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.GameId, p.CreatedAt });

        builder.HasMany(p => p.Media)
            .WithOne()
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}