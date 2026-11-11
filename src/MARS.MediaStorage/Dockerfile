FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
# curl обязателен для healthcheck'ов docker-compose: в базовом образе mcr.microsoft.com/dotnet/aspnet
# его нет, поэтому curl -f http://localhost:8080/health завершается кодом 127
# и каждый сервис уходит в unhealthy.
# Git-операции выполняются через LibGit2Sharp и не требуют установленного git CLI.
# ffmpeg обязателен для FfprobeMediaInspector и MediaTranscoder: оба зовут
# ffprobe/ffmpeg как внешние процессы, и без пакета в образе оба падали бы с
# «процесс не найден».
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl ffmpeg && \
    rm -rf /var/lib/apt/lists/*


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MARS.MediaStorage/MARS.MediaStorage.Api.Rest/MARS.MediaStorage.Api.Rest.csproj", "MARS.MediaStorage/MARS.MediaStorage.Api.Rest/"]
COPY ["src/MARS.MediaStorage/MARS.MediaStorage.Application/MARS.MediaStorage.Application.csproj", "MARS.MediaStorage/MARS.MediaStorage.Application/"]
COPY ["src/MARS.MediaStorage/MARS.MediaStorage.Domain/MARS.MediaStorage.Domain.csproj", "MARS.MediaStorage/MARS.MediaStorage.Domain/"]
COPY ["src/MARS.MediaStorage/MARS.MediaStorage.Host/MARS.MediaStorage.Host.csproj", "MARS.MediaStorage/MARS.MediaStorage.Host/"]
COPY ["src/MARS.MediaStorage/MARS.MediaStorage.Infrastructure.Git/MARS.MediaStorage.Infrastructure.Git.csproj", "MARS.MediaStorage/MARS.MediaStorage.Infrastructure.Git/"]
COPY ["src/MARS.MediaStorage/MARS.MediaStorage.Infrastructure.Media/MARS.MediaStorage.Infrastructure.Media.csproj", "MARS.MediaStorage/MARS.MediaStorage.Infrastructure.Media/"]
COPY ["src/MARS.MediaStorage/MARS.MediaStorage.Infrastructure.Postgres/MARS.MediaStorage.Infrastructure.Postgres.csproj", "MARS.MediaStorage/MARS.MediaStorage.Infrastructure.Postgres/"]
COPY ["src/MARS.MediaStorage/MARS.MediaStorage.Infrastructure.Telegram/MARS.MediaStorage.Infrastructure.Telegram.csproj", "MARS.MediaStorage/MARS.MediaStorage.Infrastructure.Telegram/"]
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/MARS.Shared/MARS.Shared.Kernel/MARS.Shared.Kernel.csproj", "MARS.Shared/MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts/MARS.Shared.Contracts.csproj", "MARS.Shared/MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Messaging.RabbitMq/MARS.Platform.Infrastructure.Messaging.RabbitMq.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Messaging.RabbitMq/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Persistence.Postgres/MARS.Platform.Infrastructure.Persistence.Postgres.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Persistence.Postgres/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Grpc/MARS.Platform.Infrastructure.Grpc.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Grpc/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/MARS.Platform.Infrastructure.Observability.Logging.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/MARS.Platform.Infrastructure.Observability.Telemetry.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/MARS.Platform.Infrastructure.Web.AspNetCore.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Development/MARS.Platform.Infrastructure.Development.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/MARS.Platform.Infrastructure.Hosting.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/MARS.Platform.Infrastructure.Clients.Http.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/"]
RUN dotnet restore "MARS.MediaStorage/MARS.MediaStorage.Host/MARS.MediaStorage.Host.csproj"

COPY ["src/MARS.MediaStorage/", "MARS.MediaStorage/"]
COPY ["src/MARS.Shared/MARS.Shared.Kernel/", "MARS.Shared/MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts/", "MARS.Shared/MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Messaging.RabbitMq/", "MARS.Platform/MARS.Platform.Infrastructure.Messaging.RabbitMq/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Persistence.Postgres/", "MARS.Platform/MARS.Platform.Infrastructure.Persistence.Postgres/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Grpc/", "MARS.Platform/MARS.Platform.Infrastructure.Grpc/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/", "MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Development/", "MARS.Platform/MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/", "MARS.Platform/MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/", "MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/"]
WORKDIR "/src/MARS.MediaStorage"
RUN dotnet build "MARS.MediaStorage.Host/MARS.MediaStorage.Host.csproj" -c Release -o /app/build

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
RUN dotnet publish "MARS.MediaStorage.Host/MARS.MediaStorage.Host.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
# Dev-образ для docker-compose.override.yml: нужен SDK для dotnet watch и curl для healthcheck.
# Git-операции выполняются через LibGit2Sharp, системный git CLI не требуется.
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=ui /ui/ui-dist ./ui-dist
EXPOSE 8080
ENTRYPOINT ["dotnet", "MARS.MediaStorage.Host.dll"]
