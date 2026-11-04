# syntax=docker/dockerfile:1

# --- Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restauration des paquets dans une couche séparée (mise en cache tant que les .csproj/.props ne changent pas)
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/AgendaBot.Host/AgendaBot.Host.csproj src/AgendaBot.Host/
RUN dotnet restore src/AgendaBot.Host/AgendaBot.Host.csproj

COPY src/ src/
RUN dotnet publish src/AgendaBot.Host/AgendaBot.Host.csproj -c Release -o /app --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Répertoire persistant pour la base SQLite, appartenant à l'utilisateur non-root "app" de l'image
RUN mkdir -p /data && chown app:app /data
VOLUME /data

ENV DOTNET_ENVIRONMENT=Production \
    ConnectionStrings__Agenda="Data Source=/data/agenda.db"

COPY --from=build /app .

USER app
ENTRYPOINT ["dotnet", "AgendaBot.Host.dll"]
