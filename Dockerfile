# syntax=docker/dockerfile:1

# --- Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restauration des paquets dans une couche séparée (mise en cache tant que le .csproj ne change pas)
COPY global.json AgendaBot.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Répertoire persistant pour la base SQLite, appartenant à l'utilisateur non-root "app" de l'image
RUN mkdir -p /data && chown app:app /data
VOLUME /data

ENV DOTNET_ENVIRONMENT=Production \
    ConnectionStrings__Agenda="Data Source=/data/agenda.db"

COPY --from=build /app .

USER appa
ENTRYPOINT ["dotnet", "AgendaBot.dll"]
