using KunigiArchive.Application.Games.CreateGame;
using KunigiArchive.Application.Games.GetGameRecord;
using KunigiArchive.Application.Games.GetGames;
using KunigiArchive.Application.Games.UpdateGameRecord;
using KunigiArchive.Application.Teams.AssignTeamManager;
using KunigiArchive.Application.Teams.CreateTeam;
using KunigiArchive.Application.Teams.DeleteTeam;
using KunigiArchive.Application.Teams.DeleteTeamMedia;
using KunigiArchive.Application.Teams.GetMyTeams;
using KunigiArchive.Application.Teams.GetTeamById;
using KunigiArchive.Application.Teams.GetTeamBySlug;
using KunigiArchive.Application.Teams.GetTeamForEdit;
using KunigiArchive.Application.Teams.GetTeamManagers;
using KunigiArchive.Application.Teams.GetTeamOptions;
using KunigiArchive.Application.Teams.GetTeamProfile;
using KunigiArchive.Application.Teams.GetTeams;
using KunigiArchive.Application.Teams.RemoveTeamManager;
using KunigiArchive.Application.Teams.SetTeamImage;
using KunigiArchive.Application.Teams.UpdateTeam;
using KunigiArchive.Application.Teams.UploadTeamMedia;
using KunigiArchive.Application.Users.ChangeUserRole;
using KunigiArchive.Application.Users.CreateUser;
using KunigiArchive.Application.Users.GetUserDetails;
using KunigiArchive.Application.Users.GetUsers;
using KunigiArchive.Application.Users.SendTemporaryPassword;
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
        services.AddScoped<GetTeamProfileHandler>();
        services.AddScoped<UpdateTeamHandler>();
        services.AddScoped<SetTeamImageHandler>();
        services.AddScoped<DeleteTeamHandler>();
        services.AddScoped<GetTeamOptionsHandler>();

        services.AddScoped<GetGamesHandler>();
        services.AddScoped<CreateGameHandler>();
        services.AddScoped<GetGameRecordHandler>();
        services.AddScoped<UpdateGameRecordHandler>();

        services.AddScoped<GetTeamManagersHandler>();
        services.AddScoped<AssignTeamManagerHandler>();
        services.AddScoped<RemoveTeamManagerHandler>();

        services.AddScoped<UploadTeamMediaHandler>();
        services.AddScoped<DeleteTeamMediaHandler>();

        services.AddScoped<GetUsersHandler>();
        services.AddScoped<GetUserDetailsHandler>();
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<ChangeUserRoleHandler>();
        services.AddScoped<SendTemporaryPasswordHandler>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}