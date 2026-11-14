using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Teams;
using KunigiArchive.Infrastructure.Persistence;
using KunigiArchive.Infrastructure.Persistence.Queries;
using KunigiArchive.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KunigiArchive.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "KunigiArchive";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.AddDbContext<KunigiArchiveDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<KunigiArchiveDbContext>());

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamQueries, TeamQueries>();

        return services;
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        KunigiArchiveDbContext dbContext = scope.ServiceProvider.GetRequiredService<KunigiArchiveDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}