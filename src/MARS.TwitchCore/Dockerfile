FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
# curl обязателен для healthcheck'ов docker-compose: в базовом образе mcr.microsoft.com/dotnet/aspnet
# его нет, поэтому curl -f http://localhost:8080/health завершается кодом 127
# и каждый сервис уходит в unhealthy.
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MARS.TwitchCore/MARS.TwitchCore.csproj", "MARS.TwitchCore/"]
COPY ["src/MARS.Shared/MARS.Shared.csproj", "MARS.Shared/"]
RUN dotnet restore "MARS.TwitchCore/MARS.TwitchCore.csproj"

COPY ["src/MARS.TwitchCore/", "MARS.TwitchCore/"]
COPY ["src/MARS.Shared/", "MARS.Shared/"]
WORKDIR "/src/MARS.TwitchCore"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
# Dev-образ для docker-compose.override.yml: нужен SDK для dotnet watch и curl для healthcheck.
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*
WORKDIR /src

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MARS.TwitchCore.dll"]
