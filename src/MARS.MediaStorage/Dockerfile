FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
# curl обязателен для healthcheck'ов docker-compose: в базовом образе mcr.microsoft.com/dotnet/aspnet
# его нет, поэтому curl -f http://localhost:8080/health завершается кодом 127
# и каждый сервис уходит в unhealthy.
# git обязателен для синхронизации wwwroot с внешним репозиторием: в образе
# dotnet/aspnet его тоже нет, и MediaGitService падал бы с "git не найден".
# ffmpeg обязателен для FfprobeMediaInspector и MediaTranscoder: оба зовут
# ffprobe/ffmpeg как внешние процессы, и без пакета в образе оба падали бы с
# «процесс не найден».
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl git ffmpeg && \
    rm -rf /var/lib/apt/lists/*


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MARS.MediaStorage/MARS.MediaStorage.csproj", "MARS.MediaStorage/"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/MARS.Shared/MARS.Shared.csproj", "MARS.Shared/"]
RUN dotnet restore "MARS.MediaStorage/MARS.MediaStorage.csproj"

COPY ["src/MARS.MediaStorage/", "MARS.MediaStorage/"]
COPY ["src/MARS.Shared/", "MARS.Shared/"]
WORKDIR "/src/MARS.MediaStorage"
RUN dotnet build -c Release -o /app/build

# Статика UI хранилища. Стадия отдельная, чтобы npm-зависимости кэшировались
# отдельно от .NET-сборки: иначе любая правка C# переустанавливала бы пакеты.
# node_modules наружу не копируется — в образ попадает только результат сборки.
FROM node:24-alpine AS ui
WORKDIR /ui
COPY ["src/MARS.MediaStorage/ClientApp/package.json", "ClientApp/"]
COPY ["src/MARS.MediaStorage/ClientApp/package-lock.json", "ClientApp/"]
WORKDIR /ui/ClientApp
RUN npm ci --no-audit --no-fund
# Путь назначения абсолютный: относительный «ClientApp/» после WORKDIR
# /ui/ClientApp дал бы /ui/ClientApp/ClientApp/, и tsc не нашёл бы tsconfig.json.
COPY ["src/MARS.MediaStorage/ClientApp/", "/ui/ClientApp/"]
RUN npm run build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
# Dev-образ для docker-compose.override.yml: нужен SDK для dotnet watch,
# curl для healthcheck и git для синхронизации хранилища.
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl git && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=ui /ui/ui-dist ./ui-dist
EXPOSE 8080
ENTRYPOINT ["dotnet", "MARS.MediaStorage.dll"]
