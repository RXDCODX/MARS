FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
# curl обязателен для healthcheck'ов docker-compose.
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MARS.Admin.Api/MARS.Admin.Api.csproj", "MARS.Admin.Api/"]
COPY ["src/MARS.Admin.Application/MARS.Admin.Application.csproj", "MARS.Admin.Application/"]
COPY ["src/MARS.Admin.Domain/MARS.Admin.Domain.csproj", "MARS.Admin.Domain/"]
COPY ["src/MARS.Admin.Infrastructure.Persistence.Postgres/MARS.Admin.Infrastructure.Persistence.Postgres.csproj", "MARS.Admin.Infrastructure.Persistence.Postgres/"]
COPY ["src/MARS.Admin.Infrastructure.Integrations.Shikimori/MARS.Admin.Infrastructure.Integrations.Shikimori.csproj", "MARS.Admin.Infrastructure.Integrations.Shikimori/"]
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
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
RUN dotnet restore "MARS.Admin.Api/MARS.Admin.Api.csproj"

COPY ["src/MARS.Admin.Api/", "MARS.Admin.Api/"]
COPY ["src/MARS.Admin.Application/", "MARS.Admin.Application/"]
COPY ["src/MARS.Admin.Domain/", "MARS.Admin.Domain/"]
COPY ["src/MARS.Admin.Infrastructure.Persistence.Postgres/", "MARS.Admin.Infrastructure.Persistence.Postgres/"]
COPY ["src/MARS.Admin.Infrastructure.Integrations.Shikimori/", "MARS.Admin.Infrastructure.Integrations.Shikimori/"]
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
WORKDIR "/src/MARS.Admin.Api"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*
WORKDIR /src

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MARS.Admin.Api.dll"]
