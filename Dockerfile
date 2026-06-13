FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["KunigiArchive.Web/KunigiArchive.Web.csproj", "KunigiArchive.Web/"]
RUN dotnet restore "KunigiArchive.Web/KunigiArchive.Web.csproj"
COPY . .
WORKDIR "/src/KunigiArchive.Web"
RUN dotnet build "KunigiArchive.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "KunigiArchive.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KunigiArchive.Web.dll"]
