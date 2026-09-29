---
name: dotnet-test-run
description: Run .NET backend tests with filtering, build verification, and result parsing. Use when the user asks to run tests, verify tests pass, or check test coverage for MARS.Server/MARS.Tests.
---

# .NET Test Run

Execute MARS backend tests with proper build-check-test sequencing and result parsing.

## Workflow

### 1. Build the test project first (always)
```bash
dotnet build MARS.Projects/MARS.Tests/MARS.Tests.csproj --configuration Release --no-restore 2>&1 | Select-String -Pattern "error CS|Build succeeded|Build FAILED"
```
If build fails, fix CS errors before running tests.

### 2. Run tests (choose one)

**All tests:**
```bash
dotnet test MARS.Projects/MARS.Tests/MARS.Tests.csproj --configuration Release 2>&1 | Select-String "Пройден|Не пройден|Всего"
```

**Filtered by class/namespace:**
```bash
dotnet test MARS.Projects/MARS.Tests/MARS.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ClassName" 2>&1
```

**With verbose output (for debugging failures):**
```bash
dotnet test MARS.Projects/MARS.Tests/MARS.Tests.csproj --filter "FullyQualifiedName~ClassName" --configuration Release --verbosity normal 2>&1
```

### 3. Parse results
- Look for `Пройден: X` / `Passed: X` for pass count
- Look for `Не пройден` / `Failed` for failures
- Look for `Всего: X` / `Total: X` for total count
- Exit code 0 = all pass, non-zero = failures

### 4. Format modified files with CSharpier (if tests exposed formatting issues)
```bash
dotnet csharpier MARS.Projects/MARS.Server/path/to/file.cs 2>&1
dotnet csharpier MARS.Projects/MARS.Tests/path/to/file.cs 2>&1
```

## Common filter patterns

| What to test | Filter |
|---|---|
| Twitch rewards | `FullyQualifiedName~FumoRoll` |
| SoundRequest | `FullyQualifiedName~SoundRequest` |
| Validation service | `FullyQualifiedName~TwitchEventValidationService` |
| Redemption validation | `FullyQualifiedName~RedemptionValidation` |
| Base commands | `FullyQualifiedName~BaseCommandTests` |
| WaifuRoll | `FullyQualifiedName~WaifuRoll` |
| AutoHello | `FullyQualifiedName~AutoHello` |

## Notes
- Always use `--no-restore` after initial restore to speed up builds
- `--configuration Release` is required (matches CI)
- Tests use InMemory EF provider — no real database needed
- `TreatWarningsAsErrors=true` means build warnings fail the build
