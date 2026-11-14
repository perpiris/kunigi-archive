using KunigiArchive.Application.Teams.AssignTeamManager;
using KunigiArchive.Application.Teams.CreateTeam;
using KunigiArchive.Application.Teams.DeleteTeamMedia;
using KunigiArchive.Application.Teams.GetMyTeams;
using KunigiArchive.Application.Teams.GetTeamById;
using KunigiArchive.Application.Teams.GetTeamBySlug;
using KunigiArchive.Application.Teams.GetTeamForEdit;
using KunigiArchive.Application.Teams.GetTeamManagers;
using KunigiArchive.Application.Teams.GetTeams;
using KunigiArchive.Application.Teams.RemoveTeamManager;
using KunigiArchive.Application.Teams.UpdateTeam;
using KunigiArchive.Application.Teams.UploadTeamMedia;
using Microsoft.Extensions.DependencyInjection;

namespace KunigiArchive.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateTeamHandler>();
        services.AddScoped<GetTeamByIdHandler>();
        services.AddScoped<GetTeamBySlugHandler>();
        services.AddScoped<GetTeamForEditHandler>();
        services.AddScoped<GetTeamsHandler>();
        services.AddScoped<GetMyTeamsHandler>();
        services.AddScoped<UpdateTeamHandler>();

        services.AddScoped<GetTeamManagersHandler>();
        services.AddScoped<AssignTeamManagerHandler>();
        services.AddScoped<RemoveTeamManagerHandler>();

        services.AddScoped<UploadTeamMediaHandler>();
        services.AddScoped<DeleteTeamMediaHandler>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}