---
name: mars-rest-service-clients
description: Historical REST-client guidance. Inter-service REST clients were removed; use mars-grpc-service-clients for service-to-service calls. REST remains for browser/Gateway and external integrations.
---

# MARS REST service clients (deprecated)

The platform no longer supports typed inter-service REST clients, except `IAudioControllerClient` for MARS.AudioController, which runs on the Windows host outside Docker. `ServiceHttpClientBase`, `AddMarsServiceClient(s)`, and the other old `I*Client` contracts were removed during the gRPC cutover.

For a call from one MARS service to another, use [`mars-grpc-service-clients`](../mars-grpc-service-clients/SKILL.md).

REST is still appropriate for:

- browser and Gateway APIs;
- SignalR, health, Swagger and OAuth endpoints;
- media URLs and other browser-facing resources;
- external providers and webhooks such as Alertmanager.

Do not add a new internal REST route as a fallback for gRPC.
