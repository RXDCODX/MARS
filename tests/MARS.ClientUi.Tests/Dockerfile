# Образ для навигационных тестов клиента (Playwright + .NET SDK 10).
#
# Отдельный образ, а не шаг в ClientApp.Dockerfile: тесты гоняются в CI на
# живом стенде, который поднимает сам раннер, и клиент к этому моменту уже
# собран внутри образа client-ui. Собирать его здесь второй раз незачем, а
# Playwright с браузерами — это сотни мегабайт, которые не должны попадать в
# боевое изображение.
#
# Версия образа обязана совпадать с Microsoft.Playwright в Directory.Packages.props:
# Playwright сверяет свою версию с версией браузера и падает при расхождении.
# Браузеры лежат в /ms-playwright, и переменная окружения говорит им, где
# искать, — иначе Playwright скачает их повторно.
FROM mcr.microsoft.com/playwright:v1.63.0-noble

# SDK ставится apt'ом: в образе Playwright его нет, а тест-проект — обычный
# .NET-проект и собирается он так же, как все остальные.
#
# --no-install-recommends обязателен: без него тянется половина графического
# стека, который тестам не нужен.
RUN apt-get update \
    && apt-get install --no-install-recommends -y \
        dotnet-sdk-10.0 \
        ca-certificates \
    && rm -rf /var/lib/apt/lists/*

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

WORKDIR /src

# Слой восстановления переиспользуется, пока не меняются .csproj и
# Directory.Packages.props, поэтому исходники копируются после него.
#
# MARS.slnx нужен из-за global.json: без него SDK не видит настройку
# `"test": { "runner": "Microsoft.Testing.Platform" }` и прогон пошёл бы
# мимо xunit v3.
COPY MARS.slnx global.json Directory.Packages.props ./
COPY tests/MARS.ClientUi.Tests/MARS.ClientUi.Tests.csproj tests/MARS.ClientUi.Tests/
RUN dotnet restore tests/MARS.ClientUi.Tests/MARS.ClientUi.Tests.csproj

# Тесты и маршруты. routes.generated.json нужен и по пути в csproj (Link +
# CopyToOutputDirectory), и как входные данные: тесты читают его из
# каталога сборки, чтобы список маршрутов не расходился с SPA.
COPY tests/MARS.ClientUi.Tests/ tests/MARS.ClientUi.Tests/
COPY src/MARS.Gateway/ClientApp/routes.generated.json src/MARS.Gateway/ClientApp/routes.generated.json

RUN dotnet build tests/MARS.ClientUi.Tests/MARS.ClientUi.Tests.csproj \
    -c Release --no-restore

# Адрес стенда приходит переменной окружения, а порт внутри образа не
# подставляется: CI запускает контейнер с --network host.
ENTRYPOINT ["dotnet", "tests/MARS.ClientUi.Tests/bin/Release/net10.0-windows/MARS.ClientUi.Tests.dll"]