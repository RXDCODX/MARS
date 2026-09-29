---
name: api-change-workflow
description: Follow the strict API change workflow: update contract, generate OpenAPI, regenerate client types. Use when modifying controllers, SignalR hubs, or API models in MARS.Server.
---

# API Change Workflow

When modifying API contracts (controllers, SignalR hubs, models), follow this strict order:

## Step 1: Update API contract
Modify controllers, models, hubs in `MARS.Projects/MARS.Server/`.

Examples:
- Add/edit controller endpoints in `MARS.Server/Controllers/`
- Add/edit DTOs in `MARS.Server/Controllers/*Dtos.cs`
- Add/edit SignalR hub methods in `MARS.Server/Hubs/`

## Step 2: Generate OpenAPI docs
```bash
dotnet run --project MARS.Projects/MARS.Server/MARS.Server.csproj --generate-openapi
```
This creates/updates:
- `MARS.Projects/mars.client/api/swagger_api.json`
- `MARS.Projects/mars.client/api/swagger_hubs.json`

## Step 3: Regenerate client types
```bash
cd MARS.Projects/mars.client && yarn build-api
```
This updates HTTP clients and types in `mars.client/src/shared/api/`.

## Step 4: Verify
```bash
# Backend builds
dotnet build MARS.Projects/MARS.Server/MARS.Server.csproj --configuration Release

# Frontend type checks
cd MARS.Projects/mars.client && yarn type-check
```

## ⚠️ Critical
- **Never skip step 2 or 3** — causes type mismatches between frontend and backend
- `build-api.js` auto-deduplicates `OperationResult` types — do not manually edit generated types in `src/shared/api/`
- After `build-api`, run `yarn type-check` to verify no type errors

## Notes
- OpenAPI generation requires the server project to compile successfully
- `build-api` reads `swagger_api.json` and `swagger_hubs.json` to generate TypeScript types
- Generated files are in `src/shared/api/` — these are auto-generated, do not edit manually
