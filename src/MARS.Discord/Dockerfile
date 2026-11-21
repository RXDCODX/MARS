FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MARS.Discord/MARS.Discord.Application/MARS.Discord.Application.csproj", "MARS.Discord/MARS.Discord.Application/"]
COPY ["src/MARS.Discord/MARS.Discord.Infrastructure/MARS.Discord.Infrastructure.csproj", "MARS.Discord/MARS.Discord.Infrastructure/"]
COPY ["src/MARS.Discord/MARS.Discord.Api/MARS.Discord.Api.csproj", "MARS.Discord/MARS.Discord.Api/"]
COPY ["src/MARS.Discord/MARS.Discord.Host/MARS.Discord.Host.csproj", "MARS.Discord/MARS.Discord.Host/"]
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/MARS.Shared/MARS.Shared.Kernel/MARS.Shared.Kernel.csproj", "MARS.Shared/MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts/MARS.Shared.Contracts.csproj", "MARS.Shared/MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Messaging.RabbitMq/MARS.Platform.Infrastructure.Messaging.RabbitMq.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Messaging.RabbitMq/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Persistence.Postgres/MARS.Platform.Infrastructure.Persistence.Postgres.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Persistence.Postgres/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/MARS.Platform.Infrastructure.Observability.Logging.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/MARS.Platform.Infrastructure.Observability.Telemetry.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/MARS.Platform.Infrastructure.Web.AspNetCore.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Development/MARS.Platform.Infrastructure.Development.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/MARS.Platform.Infrastructure.Hosting.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/MARS.Platform.Infrastructure.Clients.Http.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.YouTube/MARS.Platform.Infrastructure.YouTube.csproj", "MARS.Platform/MARS.Platform.Infrastructure.YouTube/"]
RUN dotnet restore "MARS.Discord/MARS.Discord.Host/MARS.Discord.Host.csproj"

COPY ["src/MARS.Discord/", "MARS.Discord/"]
COPY ["src/MARS.Shared/MARS.Shared.Kernel/", "MARS.Shared/MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts/", "MARS.Shared/MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Messaging.RabbitMq/", "MARS.Platform/MARS.Platform.Infrastructure.Messaging.RabbitMq/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Persistence.Postgres/", "MARS.Platform/MARS.Platform.Infrastructure.Persistence.Postgres/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/", "MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Development/", "MARS.Platform/MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/", "MARS.Platform/MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/", "MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.YouTube/", "MARS.Platform/MARS.Platform.Infrastructure.YouTube/"]
WORKDIR "/src/MARS.Discord"
RUN dotnet build "MARS.Discord.Host/MARS.Discord.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MARS.Discord.Host/MARS.Discord.Host.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*
WORKDIR /src

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MARS.Discord.Host.dll"]