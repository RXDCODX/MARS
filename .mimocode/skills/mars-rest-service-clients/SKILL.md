---
name: mars-rest-service-clients
description: Conventions for adding a REST client to call another MARS microservice: contract and client live in the owning service (Models.Rest + Infrastructure.Http), consumer adds a ProjectReference and AddMarsServiceClient. Use when integrating one service with another over HTTP.
---

# MARS REST service clients

## Rule

A REST contract and its typed client **live in the service that owns the data**:

- `MARS.Shared.Contracts.<Service>.Rest` — DTOs and the client interface (`I<Service>Client`).
- `MARS.<Service>.Infrastructure.Http` — `ServiceHttpClientBase`-derived client.

The consuming service only adds:

1. `ProjectReference` to `Models.Rest` and `Infrastructure.Http`.
2. `builder.Services.AddMarsServiceClient<I<Service>Client, <Service>Client>(ServiceClientExtensions.<Name>HttpClientName)` in its `Program.cs`.

No new projects, no duplicated DTOs, no hand-rolled `HttpClient` in the consumer.

## Template

Owner service (`MARS.Twitch` example):

- `MARS.Shared.Contracts.Twitch.Rest/TwitchUserRestModel.cs` — sealed record with init properties.
- `MARS.Shared.Contracts.Twitch.Rest/ITwitchUsersClient.cs` — interface returning the rest model.
- `MARS.Twitch.Infrastructure.Http/TwitchUsersClient.cs` — `ServiceHttpClientBase` subclass, `ServiceEndpoint => endpoints.Value.<Owner>`.

Consumer (`MARS.Alerts`):

- csproj: two `ProjectReference`s.
- Program.cs: `using MARS.Twitch.Infrastructure.Http; using MARS.Twitch.Models.Rest;` + `AddMarsServiceClient<ITwitchUsersClient, TwitchUsersClient>(...)`.
- Handler: inject `ITwitchUsersClient`, call its method.

## Checklist

- [ ] Endpoint exists in owner's `Api` (controller) returning the rest model.
- [ ] Client method maps URL via `Uri.EscapeDataString`.
- [ ] `ServiceEndpoint` points at the owner service host (e.g. `endpoints.Value.TwitchCore`).
- [ ] Consumer uses the rest model directly or maps to its own domain model at the boundary.
- [ ] `MARS.slnx` updated with new projects.
- [ ] No secret/URL hardcoded; endpoint comes from `ServiceEndpoints`.
