using KunigiArchive.Application.Common;
using KunigiArchive.Application.GameDays;
using KunigiArchive.Application.GameDays.GetGameDays;
using KunigiArchive.Application.GameDays.ManageGameDay;
using KunigiArchive.Application.GameDays.ManagePuzzles;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Games.CreateGame;
using KunigiArchive.Application.Games.DeleteGame;
using KunigiArchive.Application.Games.DeleteGameMedia;
using KunigiArchive.Application.Games.GetGameBySlug;
using KunigiArchive.Application.Games.GetGameForEdit;
using KunigiArchive.Application.Games.GetGameRecord;
using KunigiArchive.Application.Games.GetGames;
using KunigiArchive.Application.Games.GetMyGames;
using KunigiArchive.Application.Games.SetGameImage;
using KunigiArchive.Application.Games.UpdateGameDetails;
using KunigiArchive.Application.Games.UpdateGameRecord;
using KunigiArchive.Application.Games.UploadGameMedia;
using KunigiArchive.Application.Media.GetMediaStatus;
using KunigiArchive.Application.Posts;
using KunigiArchive.Application.Posts.CreatePost;
using KunigiArchive.Application.Posts.DeletePost;
using KunigiArchive.Application.Posts.DeletePostMedia;
using KunigiArchive.Application.Posts.GetGamePosts;
using KunigiArchive.Application.Posts.GetPostForEdit;
using KunigiArchive.Application.Posts.UpdatePost;
using KunigiArchive.Application.Posts.UploadPostMedia;
using KunigiArchive.Application.Sitemap;
using KunigiArchive.Application.Users.DeleteUser;
using KunigiArchive.Application.Users.SetUserDisabled;
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
using KunigiArchive.Application.Users.MyAccount;
using KunigiArchive.Application.Users.ResetPassword;
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
        services.AddScoped<GetSitemapHandler>();
        services.AddScoped<SetUserDisabledHandler>();
        services.AddScoped<DeleteUserHandler>();
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
        services.AddScoped<GetGameBySlugHandler>();
        services.AddScoped<GetGameForEditHandler>();
        services.AddScoped<GetMyGamesHandler>();
        services.AddScoped<UpdateGameDetailsHandler>();
        services.AddScoped<SetGameImageHandler>();
        services.AddScoped<UploadGameMediaHandler>();
        services.AddScoped<DeleteGameMediaHandler>();
        services.AddScoped<DeleteGameHandler>();

        services.AddScoped<GetMediaStatusHandler>();
        services.AddScoped<FindSlugRedirectHandler>();

        services.AddScoped<GameDayEditor>();
        services.AddScoped<GetGameDaysHandler>();
        services.AddScoped<GetGameDayOptionsHandler>();
        services.AddScoped<GetGameDayHandler>();
        services.AddScoped<GetGameDayForEditHandler>();
        services.AddScoped<SetGameDayEnabledHandler>();
        services.AddScoped<UpdateGameDayHandler>();
        services.AddScoped<UploadGameDayMediaHandler>();
        services.AddScoped<DeleteGameDayMediaHandler>();
        services.AddScoped<CreatePuzzleHandler>();
        services.AddScoped<UpdatePuzzleHandler>();
        services.AddScoped<DeletePuzzleHandler>();
        services.AddScoped<MovePuzzleHandler>();
        services.AddScoped<UploadPuzzleMediaHandler>();
        services.AddScoped<DeletePuzzleMediaHandler>();

        services.AddScoped<GameAccess>();
        services.AddScoped<GetGamePostsHandler>();
        services.AddScoped<GetPostForEditHandler>();
        services.AddScoped<CreatePostHandler>();
        services.AddScoped<UpdatePostHandler>();
        services.AddScoped<DeletePostHandler>();
        services.AddScoped<UploadPostMediaHandler>();
        services.AddScoped<DeletePostMediaHandler>();

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
        services.AddScoped<GetMyAccountHandler>();
        services.AddScoped<ChangeMyEmailHandler>();
        services.AddScoped<ChangeMyPasswordHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<ResetPasswordHandler>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}