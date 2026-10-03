# Клиент MARS.Gateway: оверлеи, админка и сайт.
#
# Собирается в два образа, как MediaStorage: стадия сборки на Node и финальная на
# nginx, который только раздаёт готовые файлы. В финальный образ не попадает ни
# Node, ни исходники, ни node_modules.
#
# Наружу контейнер не публикуется. Единственный опубликованный порт — Gateway
# (9155), и он проксирует корень сюда.

FROM node:24-alpine AS build
WORKDIR /ui/ClientApp

# Манифесты копируются отдельно от исходников: правка package.json не должна
# пересобирать установку. Тот же приём, что Directory.Packages.props перед
# dotnet restore в сервисных образах.
COPY ["src/MARS.Gateway/ClientApp/package.json", "./"]
COPY ["src/MARS.Gateway/ClientApp/yarn.lock", "./"]
# .yarnrc.yml обязателен до установки: там enableScripts, а часть пакетов
# ставит себя postinstall-скриптом.
COPY ["src/MARS.Gateway/ClientApp/.yarnrc.yml", "./"]

# В образе глобальный Yarn — 1.22, а package.json объявляет
# packageManager: yarn@4.18.0. Yarn 1 на это отвечает отказом и просит включить
# corepack, то есть установка падала бы на самом первом шаге.
RUN corepack enable

RUN yarn install --mode=skip-build

COPY ["src/MARS.Gateway/ClientApp/", "./"]

# Сборка идёт в production-режиме: .env.production задаёт относительный базис,
# из-за которого хаб и API достаются через Gateway тем же origin, что и страница.
# SKIP_STORYBOOK_VITEST здесь не нужен — vitest в сборке образа не запускается,
# проверка контракта живёт в CI-шаге фронтенда, где доступен весь репозиторий.
RUN yarn build

FROM nginx:alpine AS final

COPY ["infrastructure/nginx/client-ui.conf", "/etc/nginx/conf.d/default.conf"]
COPY --from=build /ui/ClientApp/dist /usr/share/nginx/html

EXPOSE 80

# healthcheck compose отдаёт curl по правилам репозитория, но в nginx:alpine его
# нет, и apt-пакета на Alpine тоже нет. Зато есть busybox-wget, поэтому проверка
# идёт через него и по отдельному /healthz, а не по корню.
HEALTHCHECK --interval=10s --timeout=5s --retries=12 --start-period=10s \
    CMD wget --spider -q http://localhost/healthz || exit 1

STOPSIGNAL SIGQUIT
CMD ["nginx", "-g", "daemon off;"]