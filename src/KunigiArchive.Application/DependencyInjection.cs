using KunigiArchive.Application.Teams.CreateTeam;
using KunigiArchive.Application.Teams.GetTeamById;
using KunigiArchive.Application.Teams.GetTeams;
using KunigiArchive.Application.Teams.GetTeamBySlug;
using KunigiArchive.Application.Teams.UpdateTeam;
using Microsoft.Extensions.DependencyInjection;

namespace KunigiArchive.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateTeamHandler>();
        services.AddScoped<GetTeamByIdHandler>();
        services.AddScoped<GetTeamBySlugHandler>();
        services.AddScoped<GetTeamsHandler>();
        services.AddScoped<UpdateTeamHandler>();

        return services;
    }
}