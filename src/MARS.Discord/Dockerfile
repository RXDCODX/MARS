FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080 8081
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
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/MARS.Platform.Infrastructure.RabbitMq.csproj", "MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Postgres/MARS.Platform.Infrastructure.Postgres.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Postgres/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/MARS.Platform.Infrastructure.Observability.Logging.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/MARS.Platform.Infrastructure.Observability.Telemetry.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/MARS.Platform.Infrastructure.Web.AspNetCore.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Development/MARS.Platform.Infrastructure.Development.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/MARS.Platform.Infrastructure.Hosting.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Grpc/MARS.Platform.Infrastructure.Grpc.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Grpc/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Http/MARS.Platform.Infrastructure.Http.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Http/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.YouTube/MARS.Platform.Infrastructure.YouTube.csproj", "MARS.Platform/MARS.Platform.Infrastructure.YouTube/"]
COPY ["src/MARS.Alerts/MARS.Alerts.Models.Overlay/MARS.Alerts.Models.Overlay.csproj", "MARS.Alerts/MARS.Alerts.Models.Overlay/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Alerts/MARS.Shared.Contracts.Alerts.csproj", "MARS.Shared/MARS.Shared.Contracts.Alerts/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Scoreboard/MARS.Shared.Contracts.Scoreboard.csproj", "MARS.Shared/MARS.Shared.Contracts.Scoreboard/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.SoundRequest/MARS.Shared.Contracts.SoundRequest.csproj", "MARS.Shared/MARS.Shared.Contracts.SoundRequest/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Twitch.Rest/MARS.Shared.Contracts.Twitch.Rest.csproj", "MARS.Shared/MARS.Shared.Contracts.Twitch.Rest/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.WaifuGacha.Rest/MARS.Shared.Contracts.WaifuGacha.Rest.csproj", "MARS.Shared/MARS.Shared.Contracts.WaifuGacha.Rest/"]
COPY ["src/MARS.WaifuGacha/MARS.WaifuGacha.Domain/MARS.WaifuGacha.Domain.csproj", "MARS.WaifuGacha/MARS.WaifuGacha.Domain/"]
RUN dotnet restore "MARS.Discord/MARS.Discord.Host/MARS.Discord.Host.csproj"

COPY ["src/MARS.Discord/", "MARS.Discord/"]
COPY ["src/MARS.Shared/MARS.Shared.Kernel/", "MARS.Shared/MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts/", "MARS.Shared/MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/", "MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Postgres/", "MARS.Platform/MARS.Platform.Infrastructure.Postgres/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/", "MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Development/", "MARS.Platform/MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/", "MARS.Platform/MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Grpc/", "MARS.Platform/MARS.Platform.Infrastructure.Grpc/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Http/", "MARS.Platform/MARS.Platform.Infrastructure.Http/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.YouTube/", "MARS.Platform/MARS.Platform.Infrastructure.YouTube/"]
COPY ["src/MARS.Alerts/MARS.Alerts.Models.Overlay/", "MARS.Alerts/MARS.Alerts.Models.Overlay/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Alerts/", "MARS.Shared/MARS.Shared.Contracts.Alerts/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Scoreboard/", "MARS.Shared/MARS.Shared.Contracts.Scoreboard/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.SoundRequest/", "MARS.Shared/MARS.Shared.Contracts.SoundRequest/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Twitch.Rest/", "MARS.Shared/MARS.Shared.Contracts.Twitch.Rest/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.WaifuGacha.Rest/", "MARS.Shared/MARS.Shared.Contracts.WaifuGacha.Rest/"]
COPY ["src/MARS.WaifuGacha/MARS.WaifuGacha.Domain/", "MARS.WaifuGacha/MARS.WaifuGacha.Domain/"]
WORKDIR "/src/MARS.Discord"
RUN dotnet build "MARS.Discord.Host/MARS.Discord.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MARS.Discord.Host/MARS.Discord.Host.csproj" -c Release -o /app/publish

# SDK 11: фикс dotnet watch (dotnet/sdk#54309), отсутствующий в 10.0.4xx; prod-стейджи остаются на LTS 10.0.
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS dev
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*
WORKDIR /src

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MARS.Discord.Host.dll"]
