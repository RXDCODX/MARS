FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MARS.CinemaQueue/MARS.CinemaQueue.Domain/MARS.CinemaQueue.Domain.csproj", "MARS.CinemaQueue/MARS.CinemaQueue.Domain/"]
COPY ["src/MARS.CinemaQueue/MARS.CinemaQueue.Application/MARS.CinemaQueue.Application.csproj", "MARS.CinemaQueue/MARS.CinemaQueue.Application/"]
COPY ["src/MARS.CinemaQueue/MARS.CinemaQueue.Infrastructure.Postgres/MARS.CinemaQueue.Infrastructure.Postgres.csproj", "MARS.CinemaQueue/MARS.CinemaQueue.Infrastructure.Postgres/"]
COPY ["src/MARS.CinemaQueue/MARS.CinemaQueue.Infrastructure.Kinopoisk/MARS.CinemaQueue.Infrastructure.Kinopoisk.csproj", "MARS.CinemaQueue/MARS.CinemaQueue.Infrastructure.Kinopoisk/"]
COPY ["src/MARS.CinemaQueue/MARS.CinemaQueue.Api/MARS.CinemaQueue.Api.csproj", "MARS.CinemaQueue/MARS.CinemaQueue.Api/"]
COPY ["src/MARS.CinemaQueue/MARS.CinemaQueue.Host/MARS.CinemaQueue.Host.csproj", "MARS.CinemaQueue/MARS.CinemaQueue.Host/"]
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
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Http/MARS.Platform.Infrastructure.Http.csproj", "MARS.Platform/MARS.Platform.Infrastructure.Http/"]
COPY ["src/MARS.Alerts/MARS.Alerts.Models.Overlay/MARS.Alerts.Models.Overlay.csproj", "MARS.Alerts/MARS.Alerts.Models.Overlay/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Alerts/MARS.Shared.Contracts.Alerts.csproj", "MARS.Shared/MARS.Shared.Contracts.Alerts/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Scoreboard/MARS.Shared.Contracts.Scoreboard.csproj", "MARS.Shared/MARS.Shared.Contracts.Scoreboard/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.SoundRequest/MARS.Shared.Contracts.SoundRequest.csproj", "MARS.Shared/MARS.Shared.Contracts.SoundRequest/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Twitch.Rest/MARS.Shared.Contracts.Twitch.Rest.csproj", "MARS.Shared/MARS.Shared.Contracts.Twitch.Rest/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.WaifuGacha.Rest/MARS.Shared.Contracts.WaifuGacha.Rest.csproj", "MARS.Shared/MARS.Shared.Contracts.WaifuGacha.Rest/"]
COPY ["src/MARS.Twitch/MARS.Twitch.Infrastructure.Http/MARS.Twitch.Infrastructure.Http.csproj", "MARS.Twitch/MARS.Twitch.Infrastructure.Http/"]
COPY ["src/MARS.WaifuGacha/MARS.WaifuGacha.Infrastructure.Http/MARS.WaifuGacha.Infrastructure.Http.csproj", "MARS.WaifuGacha/MARS.WaifuGacha.Infrastructure.Http/"]
COPY ["src/MARS.WaifuGacha/MARS.WaifuGacha.Domain/MARS.WaifuGacha.Domain.csproj", "MARS.WaifuGacha/MARS.WaifuGacha.Domain/"]
RUN dotnet restore "MARS.CinemaQueue/MARS.CinemaQueue.Host/MARS.CinemaQueue.Host.csproj"

COPY ["src/MARS.CinemaQueue/", "MARS.CinemaQueue/"]
COPY ["src/MARS.Shared/MARS.Shared.Kernel/", "MARS.Shared/MARS.Shared.Kernel/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts/", "MARS.Shared/MARS.Shared.Contracts/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/", "MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Postgres/", "MARS.Platform/MARS.Platform.Infrastructure.Postgres/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/", "MARS.Platform/MARS.Platform.Infrastructure.Observability.Telemetry/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/", "MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Development/", "MARS.Platform/MARS.Platform.Infrastructure.Development/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/", "MARS.Platform/MARS.Platform.Infrastructure.Hosting/"]
COPY ["src/MARS.Platform/MARS.Platform.Infrastructure.Http/", "MARS.Platform/MARS.Platform.Infrastructure.Http/"]
COPY ["src/MARS.Alerts/MARS.Alerts.Models.Overlay/", "MARS.Alerts/MARS.Alerts.Models.Overlay/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Alerts/", "MARS.Shared/MARS.Shared.Contracts.Alerts/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Scoreboard/", "MARS.Shared/MARS.Shared.Contracts.Scoreboard/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.SoundRequest/", "MARS.Shared/MARS.Shared.Contracts.SoundRequest/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.Twitch.Rest/", "MARS.Shared/MARS.Shared.Contracts.Twitch.Rest/"]
COPY ["src/MARS.Shared/MARS.Shared.Contracts.WaifuGacha.Rest/", "MARS.Shared/MARS.Shared.Contracts.WaifuGacha.Rest/"]
COPY ["src/MARS.Twitch/MARS.Twitch.Infrastructure.Http/", "MARS.Twitch/MARS.Twitch.Infrastructure.Http/"]
COPY ["src/MARS.WaifuGacha/MARS.WaifuGacha.Infrastructure.Http/", "MARS.WaifuGacha/MARS.WaifuGacha.Infrastructure.Http/"]
COPY ["src/MARS.WaifuGacha/MARS.WaifuGacha.Domain/", "MARS.WaifuGacha/MARS.WaifuGacha.Domain/"]
WORKDIR "/src/MARS.CinemaQueue"
RUN dotnet build "MARS.CinemaQueue.Host/MARS.CinemaQueue.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MARS.CinemaQueue.Host/MARS.CinemaQueue.Host.csproj" -c Release -o /app/publish

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
ENTRYPOINT ["dotnet", "MARS.CinemaQueue.Host.dll"]
