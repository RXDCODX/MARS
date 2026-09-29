---
name: frontend-type-check
description: Run frontend type checking, linting, and build verification for mars.client. Use when the user asks to check types, lint, build, or verify the frontend compiles.
---

# Frontend Type Check & Build

Verify mars.client compiles correctly with type checking, linting, and build.

## Workflow

### 1. Type check (fastest feedback)
```bash
yarn type-check 2>&1
```
Run from `MARS.Projects/mars.client` directory.

**With last N lines only (for quick scan):**
```bash
yarn type-check 2>&1 | Select-Object -Last 10
```

### 2. Lint (with autofix)
```bash
yarn lint
```

### 3. Lint (no fix, CI mode)
```bash
yarn lint:noFix
```

### 4. Build (full production build)
```bash
yarn build 2>&1 | Select-String -Pattern "error|✓ built"
```

### 5. Regenerate API client (after backend API changes)
```bash
yarn build-api
```

## Command reference

| Command | Purpose | Speed |
|---|---|---|
| `yarn type-check` | TypeScript type verification | Fast |
| `yarn lint` | ESLint with autofix | Medium |
| `yarn lint:noFix` | ESLint without fix | Medium |
| `yarn build` | Production build | Slow |
| `yarn build-api` | Regenerate API client types | Medium |
| `yarn test` | Unit tests (vitest) | Medium |
| `yarn stylelint` | CSS/SCSS linting | Fast |

## Notes
- Port 44478 is used by Vite dev server (proxied by SpaYarp)
- Path aliases (`@/`, `@/components`, `@/shared`) must match between `vite.config.ts` and `vitest.config.ts`
- ESLint flat config with `simple-import-sort` — imports are auto-sorted
- All interactive elements must have `data-testid` attributes per AGENTS.md
- Use `simple-import-sort` for import ordering (enforced by ESLint)
