FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MARS.Videos365/MARS.Videos365.Domain/MARS.Videos365.Domain.csproj", "MARS.Videos365/MARS.Videos365.Domain/"]
COPY ["src/MARS.Videos365/MARS.Videos365.Application/MARS.Videos365.Application.csproj", "MARS.Videos365/MARS.Videos365.Application/"]
COPY ["src/MARS.Videos365/MARS.Videos365.Infrastructure/MARS.Videos365.Infrastructure.csproj", "MARS.Videos365/MARS.Videos365.Infrastructure/"]
COPY ["src/MARS.Videos365/MARS.Videos365.Host/MARS.Videos365.Host.csproj", "MARS.Videos365/MARS.Videos365.Host/"]
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
RUN dotnet restore "MARS.Videos365/MARS.Videos365.Host/MARS.Videos365.Host.csproj"

COPY ["src/MARS.Videos365/", "MARS.Videos365/"]
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
WORKDIR "/src/MARS.Videos365"
RUN dotnet build "MARS.Videos365.Host/MARS.Videos365.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MARS.Videos365.Host/MARS.Videos365.Host.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*
WORKDIR /src

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MARS.Videos365.Host.dll"]