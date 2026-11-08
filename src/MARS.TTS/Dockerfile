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

COPY ["src/MARS.TTS/MARS.TTS.csproj", "MARS.TTS/"]
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/MARS.Shared.Kernel/MARS.Shared.Kernel.csproj", "MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared.Contracts/MARS.Shared.Contracts.csproj", "MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform.Infrastructure.Messaging.RabbitMq/MARS.Platform.Infrastructure.Messaging.RabbitMq.csproj", "MARS.Platform.Infrastructure.Messaging.RabbitMq/"]
COPY ["src/MARS.Platform.Infrastructure.Persistence.Postgres/MARS.Platform.Infrastructure.Persistence.Postgres.csproj", "MARS.Platform.Infrastructure.Persistence.Postgres/"]
COPY ["src/MARS.Platform.Infrastructure.Grpc/MARS.Platform.Infrastructure.Grpc.csproj", "MARS.Platform.Infrastructure.Grpc/"]
COPY ["src/MARS.Platform.Infrastructure.Observability.Logging/MARS.Platform.Infrastructure.Observability.Logging.csproj", "MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform.Infrastructure.Observability.Telemetry/MARS.Platform.Infrastructure.Observability.Telemetry.csproj", "MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform.Infrastructure.Web.AspNetCore/MARS.Platform.Infrastructure.Web.AspNetCore.csproj", "MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform.Infrastructure.Development/MARS.Platform.Infrastructure.Development.csproj", "MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform.Infrastructure.Hosting/MARS.Platform.Infrastructure.Hosting.csproj", "MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform.Infrastructure.Clients.Http/MARS.Platform.Infrastructure.Clients.Http.csproj", "MARS.Platform.Infrastructure.Clients.Http/"]
RUN dotnet restore "MARS.TTS/MARS.TTS.csproj"

COPY ["src/MARS.TTS/", "MARS.TTS/"]
COPY ["src/MARS.Shared.Kernel/", "MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared.Contracts/", "MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform.Infrastructure.Messaging.RabbitMq/", "MARS.Platform.Infrastructure.Messaging.RabbitMq/"]
COPY ["src/MARS.Platform.Infrastructure.Persistence.Postgres/", "MARS.Platform.Infrastructure.Persistence.Postgres/"]
COPY ["src/MARS.Platform.Infrastructure.Grpc/", "MARS.Platform.Infrastructure.Grpc/"]
COPY ["src/MARS.Platform.Infrastructure.Observability.Logging/", "MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform.Infrastructure.Observability.Telemetry/", "MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform.Infrastructure.Web.AspNetCore/", "MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform.Infrastructure.Development/", "MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform.Infrastructure.Hosting/", "MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform.Infrastructure.Clients.Http/", "MARS.Platform.Infrastructure.Clients.Http/"]
WORKDIR "/src/MARS.TTS"
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
ENTRYPOINT ["dotnet", "MARS.TTS.dll"]
