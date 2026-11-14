FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
# Fonts for PDFtoImage (PDFium) rendering
RUN apt-get update \
    && apt-get install -y --no-install-recommends fontconfig libfontconfig1 libfreetype6 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /repo

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/KunigiArchive.Web/KunigiArchive.Web.csproj", "src/KunigiArchive.Web/"]
RUN dotnet restore "src/KunigiArchive.Web/KunigiArchive.Web.csproj"

COPY ["src/", "src/"]
RUN dotnet publish "src/KunigiArchive.Web/KunigiArchive.Web.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KunigiArchive.Web.dll"]
