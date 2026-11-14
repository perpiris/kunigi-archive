using KunigiArchive.Web.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Web.Data;

public class DataContext : IdentityDbContext<AppUser, AppRole, long>
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamManager> TeamManagers { get; set; }
    public DbSet<TeamMedia> TeamMedia { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameMedia> GameMedia { get; set; }
    public DbSet<GameDay> GameDays { get; set; }
    public DbSet<GameDayType> GameDayTypes { get; set; }
    public DbSet<GameDayMedia> GameDayMedia { get; set; }
    public DbSet<Puzzle> Puzzles { get; set; }
    public DbSet<PuzzleMedia> PuzzleMedia { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostMedia> PostMedia { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}