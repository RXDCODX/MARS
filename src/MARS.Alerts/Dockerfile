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

COPY ["src/MARS.Alerts/MARS.Alerts.csproj", "MARS.Alerts/"]
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/MARS.Shared.Kernel/MARS.Shared.Kernel.csproj", "MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared.Contracts/MARS.Shared.Contracts.csproj", "MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform.Messaging.RabbitMq.Infrastructure/MARS.Platform.Messaging.RabbitMq.Infrastructure.csproj", "MARS.Platform.Messaging.RabbitMq.Infrastructure/"]
COPY ["src/MARS.Platform.Persistence.Postgres.Infrastructure/MARS.Platform.Persistence.Postgres.Infrastructure.csproj", "MARS.Platform.Persistence.Postgres.Infrastructure/"]
COPY ["src/MARS.Platform.Grpc.Infrastructure/MARS.Platform.Grpc.Infrastructure.csproj", "MARS.Platform.Grpc.Infrastructure/"]
COPY ["src/MARS.Platform.Observability.Logging.Infrastructure/MARS.Platform.Observability.Logging.Infrastructure.csproj", "MARS.Platform.Observability.Logging.Infrastructure/"]
COPY ["src/MARS.Platform.Observability.Telemetry.Infrastructure/MARS.Platform.Observability.Telemetry.Infrastructure.csproj", "MARS.Platform.Observability.Telemetry.Infrastructure/"]
COPY ["src/MARS.Platform.Web.AspNetCore.Infrastructure/MARS.Platform.Web.AspNetCore.Infrastructure.csproj", "MARS.Platform.Web.AspNetCore.Infrastructure/"]
COPY ["src/MARS.Platform.Development.Infrastructure/MARS.Platform.Development.Infrastructure.csproj", "MARS.Platform.Development.Infrastructure/"]
COPY ["src/MARS.Platform.Hosting.Infrastructure/MARS.Platform.Hosting.Infrastructure.csproj", "MARS.Platform.Hosting.Infrastructure/"]
COPY ["src/MARS.Platform.Clients.Http.Infrastructure/MARS.Platform.Clients.Http.Infrastructure.csproj", "MARS.Platform.Clients.Http.Infrastructure/"]
RUN dotnet restore "MARS.Alerts/MARS.Alerts.csproj"

COPY ["src/MARS.Alerts/", "MARS.Alerts/"]
COPY ["src/MARS.Shared.Kernel/", "MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared.Contracts/", "MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform.Messaging.RabbitMq.Infrastructure/", "MARS.Platform.Messaging.RabbitMq.Infrastructure/"]
COPY ["src/MARS.Platform.Persistence.Postgres.Infrastructure/", "MARS.Platform.Persistence.Postgres.Infrastructure/"]
COPY ["src/MARS.Platform.Grpc.Infrastructure/", "MARS.Platform.Grpc.Infrastructure/"]
COPY ["src/MARS.Platform.Observability.Logging.Infrastructure/", "MARS.Platform.Observability.Logging.Infrastructure/"]
COPY ["src/MARS.Platform.Observability.Telemetry.Infrastructure/", "MARS.Platform.Observability.Telemetry.Infrastructure/"]
COPY ["src/MARS.Platform.Web.AspNetCore.Infrastructure/", "MARS.Platform.Web.AspNetCore.Infrastructure/"]
COPY ["src/MARS.Platform.Development.Infrastructure/", "MARS.Platform.Development.Infrastructure/"]
COPY ["src/MARS.Platform.Hosting.Infrastructure/", "MARS.Platform.Hosting.Infrastructure/"]
COPY ["src/MARS.Platform.Clients.Http.Infrastructure/", "MARS.Platform.Clients.Http.Infrastructure/"]
WORKDIR "/src/MARS.Alerts"
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
ENTRYPOINT ["dotnet", "MARS.Alerts.dll"]
