---
name: frontend-type-check
description: Validate the MARS MediaStorage Razor UI and its hosting project; no separate Node/Vite build.
---

# MediaStorage Razor UI checks

UI хранилища реализован на ASP.NET Core Razor Pages и vanilla JavaScript.
Он входит в `MARS.MediaStorage.Host`; отдельной сборки npm/Vite нет.

## Build and test

```bash
dotnet build src/MARS.MediaStorage/MARS.MediaStorage.Host/MARS.MediaStorage.Host.csproj
dotnet test tests/MARS.MediaStorage.Tests/MARS.MediaStorage.Tests.csproj
```

## Runtime assets

- Razor markup: `MARS.MediaStorage.Host/Pages/Storage`.
- CSS/JavaScript: `MARS.MediaStorage.Host/StorageUiAssets`.
- Host serves assets under `/storage-ui/assets` from a provider outside media
  `wwwroot` and copies them to output/publish.
- Main page: `/storage-ui/index.html`; `/storage-ui` redirects to it.
- API: `/api/MediaStorage/*`; SignalR: `/hubs/media-storage`.

## Manual scenarios

- Open root, nested and empty directories; verify breadcrumbs and browser Back/Forward.
- Confirm `.git`, `_trash`, hidden segments and symlinks are not exposed.
- Preview image, video, audio, PDF and supported text; verify HTML/JS/SVG are not
  rendered as active content and text preview is capped at 512 KiB.
- Exercise upload/progress/drag-drop, move/delete/restore dry-run, trash and Git sync.
- Confirm UI assets do not appear under media `wwwroot` or in media Git status.
