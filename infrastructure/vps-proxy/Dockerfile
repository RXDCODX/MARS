FROM metacubex/mihomo:latest AS mihomo
FROM alpine:3.20
RUN apk add --no-cache gettext
COPY --from=mihomo /mihomo /usr/bin/mihomo
WORKDIR /opt/vps-proxy
COPY config.template.yaml /opt/vps-proxy/config.template.yaml
COPY entrypoint.sh /opt/vps-proxy/entrypoint.sh
RUN chmod +x /opt/vps-proxy/entrypoint.sh
ENTRYPOINT ["/opt/vps-proxy/entrypoint.sh"]