FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /repo

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/KunigiArchive.Domain/KunigiArchive.Domain.csproj", "src/KunigiArchive.Domain/"]
COPY ["src/KunigiArchive.Application/KunigiArchive.Application.csproj", "src/KunigiArchive.Application/"]
COPY ["src/KunigiArchive.Infrastructure/KunigiArchive.Infrastructure.csproj", "src/KunigiArchive.Infrastructure/"]
COPY ["src/KunigiArchive.Blazor/KunigiArchive.Blazor.csproj", "src/KunigiArchive.Blazor/"]
RUN dotnet restore "src/KunigiArchive.Blazor/KunigiArchive.Blazor.csproj"

COPY ["src/KunigiArchive.Domain/", "src/KunigiArchive.Domain/"]
COPY ["src/KunigiArchive.Application/", "src/KunigiArchive.Application/"]
COPY ["src/KunigiArchive.Infrastructure/", "src/KunigiArchive.Infrastructure/"]
COPY ["src/KunigiArchive.Blazor/", "src/KunigiArchive.Blazor/"]
RUN dotnet publish "src/KunigiArchive.Blazor/KunigiArchive.Blazor.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KunigiArchive.Blazor.dll"]
