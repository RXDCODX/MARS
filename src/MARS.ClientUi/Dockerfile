# Клиент MARS: production SPA image and Vite development server.
#
# Dependencies are installed once in the shared stage. The dev target keeps
# node_modules in the image so docker-compose can mount the source tree without
# hiding installed packages. Production still ships only nginx + built assets.

FROM node:24-alpine AS dependencies
WORKDIR /ui/ClientApp

COPY ["src/MARS.ClientUi/package.json", "./"]
COPY ["src/MARS.ClientUi/yarn.lock", "./"]
COPY ["src/MARS.ClientUi/.yarnrc.yml", "./"]

RUN corepack enable && yarn install --immutable

# Dev-only target. docker-compose.dev.yml bind-mounts the source directory and
# mounts a named volume over node_modules to keep Linux dependencies isolated
# from the host OS (especially Windows).
FROM dependencies AS dev
EXPOSE 80
CMD ["yarn", "dev", "--host", "0.0.0.0", "--port", "80", "--strictPort"]

FROM dependencies AS build
COPY ["src/MARS.ClientUi/", "./"]
RUN yarn build

FROM nginx:alpine AS final

COPY ["infrastructure/nginx/client-ui.conf", "/etc/nginx/conf.d/default.conf"]
COPY --from=build /ui/ClientApp/dist /usr/share/nginx/html

EXPOSE 80

# busybox-wget is available in nginx:alpine; curl is not.
HEALTHCHECK --interval=10s --timeout=5s --retries=12 --start-period=10s \
    CMD wget --spider -q http://127.0.0.1/healthz || exit 1

STOPSIGNAL SIGQUIT
CMD ["nginx", "-g", "daemon off;"]
