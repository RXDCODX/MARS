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

COPY ["src/MARS.Telegram/MARS.Telegram.Host/MARS.Telegram.Host.csproj", "MARS.Telegram/MARS.Telegram.Host/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Api.Rest/MARS.Telegram.Api.Rest.csproj", "MARS.Telegram/MARS.Telegram.Api.Rest/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Api.Grpc/MARS.Telegram.Api.Grpc.csproj", "MARS.Telegram/MARS.Telegram.Api.Grpc/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Domain/MARS.Telegram.Domain.csproj", "MARS.Telegram/MARS.Telegram.Domain/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Application/MARS.Telegram.Application.csproj", "MARS.Telegram/MARS.Telegram.Application/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Infrastructure.Postgres/MARS.Telegram.Infrastructure.Postgres.csproj", "MARS.Telegram/MARS.Telegram.Infrastructure.Postgres/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Infrastructure.MTProto/MARS.Telegram.Infrastructure.MTProto.csproj", "MARS.Telegram/MARS.Telegram.Infrastructure.MTProto/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Infrastructure.GooglePhotos/MARS.Telegram.Infrastructure.GooglePhotos.csproj", "MARS.Telegram/MARS.Telegram.Infrastructure.GooglePhotos/"]
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
RUN dotnet restore "MARS.Telegram/MARS.Telegram.Host/MARS.Telegram.Host.csproj"

COPY ["src/MARS.Telegram/MARS.Telegram.Host/", "MARS.Telegram/MARS.Telegram.Host/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Api.Rest/", "MARS.Telegram/MARS.Telegram.Api.Rest/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Api.Grpc/", "MARS.Telegram/MARS.Telegram.Api.Grpc/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Domain/", "MARS.Telegram/MARS.Telegram.Domain/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Application/", "MARS.Telegram/MARS.Telegram.Application/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Infrastructure.Postgres/", "MARS.Telegram/MARS.Telegram.Infrastructure.Postgres/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Infrastructure.MTProto/", "MARS.Telegram/MARS.Telegram.Infrastructure.MTProto/"]
COPY ["src/MARS.Telegram/MARS.Telegram.Infrastructure.GooglePhotos/", "MARS.Telegram/MARS.Telegram.Infrastructure.GooglePhotos/"]
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
WORKDIR "/src/MARS.Telegram.Host"
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
ENTRYPOINT ["dotnet", "MARS.Telegram.Host.dll"]
