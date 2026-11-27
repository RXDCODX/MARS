---
name: mars-grpc-service-clients
description: Add or migrate an inter-service MARS call to typed gRPC. Covers Protobuf contracts, provider mapping, IMarsGrpcClient facades, registration, authentication, status codes, tests, ports and Docker/compose updates.
---

# MARS gRPC service clients

## Rule

Every inter-service call uses typed Protobuf over gRPC. Do not add an internal REST endpoint, a JSON-in-bytes payload, a dual-run fallback, or an `OperationResult` error envelope. Browser/Gateway REST, SignalR, health, Swagger, OAuth, media URLs, external providers, the Alertmanager webhook and AudioController (Windows host, outside Docker) remain HTTP.

## Contract

- Add the `.proto` under `src/MARS.Shared/MARS.Shared.Contracts/Protos/<service>/`.
- Use a service-specific `package` and `csharp_namespace`; generated clients and messages stay in `MARS.Shared.Contracts`.
- Model complex payloads as explicit Protobuf messages. Use standard gRPC status codes for failures.
- Preserve optionality with proto3 `optional` or a presence field such as `has_*`; do not encode nullable values as JSON.

## Provider (owning service)

- Implement the generated `*ServiceBase` next to the owning API/host, commonly in `Grpc/`.
- Map transport messages to the existing application/domain models at that boundary.
- Throw `RpcException(new Status(...))` for invalid arguments, not-found results, failed preconditions and unavailable dependencies.
- Register/map the service and keep `.RequireAuthorization(ServiceAuthExtensions.PolicyName)`. `AddMarsGrpcHosting()` also installs the global API-key interceptor.
- Do not perform network, filesystem or broker work in constructors.

## Client facade

- Add a service-named interface and implementation under `MARS.Platform.Infrastructure.Grpc/<Service>/`.
- Expose it through `IMarsGrpcClient`; its property getter must reject self-calls, including logical aliases such as Twitch/TwitchCore.
- Register the generated client in `AddMarsGrpcClient` with the logical endpoint key from `Grpc:Endpoints`, and add `ServiceApiKeyGrpcClientInterceptor`.
- Consumers inject `IMarsGrpcClient`, not the generated client or `HttpClient`.
- Convert `NotFound` to `null` only when the existing caller contract requires a missing-value fallback; do not swallow cancellation or temporary failures.

## Verification

- Add provider tests for mapping and status codes, and consumer tests with mocked `IMarsGrpcClient` facades.
- Extend `MarsGrpcClientTests` so every generated client is registered and every self-call alias is covered.
- Run the affected service tests and `dotnet build MARS.slnx -c Release`.
- If hosting/ports change, update Dockerfiles (`EXPOSE 8081` and Grpc project COPY), compose `expose`/dev publications, `Grpc:Endpoints`, env port keys, `PortAllocationTests`, and generated local launch profiles.
