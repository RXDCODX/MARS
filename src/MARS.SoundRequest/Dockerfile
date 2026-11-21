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

COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
# Структура src/MARS.SoundRequest/<project>/ сохраняется: csproj ссылаются на
# ../../MARS.Shared и ../../MARS.Platform, и при «сплющивании» каталогов эти
# пути указывали бы на /MARS.Shared и сборка падала с CS0234/CS0246.
COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Host/MARS.SoundRequest.Host.csproj", "MARS.SoundRequest/MARS.SoundRequest.Host/"]
COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Api/MARS.SoundRequest.Api.csproj", "MARS.SoundRequest/MARS.SoundRequest.Api/"]
COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Application/MARS.SoundRequest.Application.csproj", "MARS.SoundRequest/MARS.SoundRequest.Application/"]
COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Domain/MARS.SoundRequest.Domain.csproj", "MARS.SoundRequest/MARS.SoundRequest.Domain/"]
COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Infrastructure/MARS.SoundRequest.Infrastructure.csproj", "MARS.SoundRequest/MARS.SoundRequest.Infrastructure/"]
COPY ["src/MARS.Shared/MARS.Shared.Kernel/MARS.Shared.Kernel.csproj", "MARS.Shared/MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts/MARS.Shared.Contracts.csproj", "MARS.Shared/MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/MARS.Platform.Infrastructure.RabbitMq.csproj", "MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Events/MARS.Platform.Infrastructure.Events.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Events/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Postgres/MARS.Platform.Infrastructure.Postgres.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Postgres/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/MARS.Platform.Infrastructure.Observability.Logging.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/MARS.Platform.Infrastructure.Observability.Telemetry.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/MARS.Platform.Infrastructure.Web.AspNetCore.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Development/MARS.Platform.Infrastructure.Development.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/MARS.Platform.Infrastructure.Hosting.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Http/MARS.Platform.Infrastructure.Http.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Http/"]
RUN dotnet restore "MARS.SoundRequest/MARS.SoundRequest.Host/MARS.SoundRequest.Host.csproj"

COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Host/", "MARS.SoundRequest/MARS.SoundRequest.Host/"]
COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Api/", "MARS.SoundRequest/MARS.SoundRequest.Api/"]
COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Application/", "MARS.SoundRequest/MARS.SoundRequest.Application/"]
COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Domain/", "MARS.SoundRequest/MARS.SoundRequest.Domain/"]
COPY ["src/MARS.SoundRequest/MARS.SoundRequest.Infrastructure/", "MARS.SoundRequest/MARS.SoundRequest.Infrastructure/"]
COPY ["src/MARS.Shared/MARS.Shared.Kernel/", "MARS.Shared/MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts/", "MARS.Shared/MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/", "MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Events/", "MARS.Platform/MARS.Platform.Infrastructure.Events/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Postgres/", "MARS.Platform/MARS.Platform.Infrastructure.Postgres/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/", "MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Development/", "MARS.Platform/MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/", "MARS.Platform/MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Http/", "MARS.Platform/MARS.Platform.Infrastructure.Http/"]
WORKDIR "/src/MARS.SoundRequest/MARS.SoundRequest.Host"
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
ENTRYPOINT ["dotnet", "MARS.SoundRequest.Host.dll"]
