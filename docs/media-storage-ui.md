# MARS.MediaStorage UI

## Public URL and hosting

The primary UI is an ASP.NET Core Razor Page at `/storage-ui/index.html`.
`/storage-ui` redirects to the canonical page. The Gateway continues to proxy
`/storage-ui/{**remainder}` to the `media-storage` service; API and SignalR
paths remain `/api/MediaStorage/*` and `/hubs/media-storage`.

Razor markup lives in `MARS.MediaStorage.Host/Pages/Storage`. CSS and JavaScript
live in `MARS.MediaStorage.Host/StorageUiAssets` and are served under
`/storage-ui/assets/*` by a dedicated `PhysicalFileProvider`. The Host project
copies these assets to build and publish output.

**Do not put UI assets in the media `wwwroot`.** That directory is a required
bind mount containing the user's media files and its Git working copy. UI
assets there would be indexed as media and could appear in the media repository.

## Browser data model

- The filesystem under `WebRootPath` is the source of truth for folders,
  physical files, file size and last-write time. Empty directories are visible.
- PostgreSQL `MediaStorageEntry` supplies IDs and storage metadata for
  indexed files. A physical file without an entry remains visible as
  `Не индексирован`; ID-based bulk actions are disabled for it.
- Git state is provided by `IMediaGitService`.
- The browser endpoint returns only direct children. The left tree loads
  child directories lazily; the current directory is represented in the
  query string and breadcrumb navigation.
- `.git`, `_trash`, dot-prefixed path segments and symlink/reparse points
  are excluded from normal browsing and indexing. The trash is a separate
  view backed by the existing deleted-entry API.

## API

| Endpoint | Purpose |
|---|---|
| `GET /api/MediaStorage/browse?directory=` | Direct child folders/files, including unindexed files |
| `GET /api/MediaStorage/search?q=&page=&pageSize=` | Global paginated search over indexed entries |
| `GET /api/MediaStorage/preview?path=` | Safe allowlisted inline image/audio/video/PDF response with Range |
| `GET /api/MediaStorage/text-preview?path=` | Text response capped at 512 KiB, with truncation metadata |
| `GET /api/MediaStorage/file?path=` | Attachment download; existing URL preserved |

HTML, JavaScript, SVG and unknown formats are not served as active inline content.
Text is returned as data and displayed using DOM text APIs, never `innerHTML`.
All file paths are relative to `WebRootPath`; the resolver checks path
containment and each path segment, rejecting hidden/internal paths and
symlinks/reparse points. Never expose absolute host/container paths in responses.

Existing upload, move, soft-delete, restore, index and selective Git-sync API
contracts remain in place. The UI supports multi-file upload with progress and
drag-and-drop, folder selection for move/upload, default dry-run for bulk
operations, restore from the separate trash view and operation result details.

## Filesystem change notifications

`StorageChanged` represents indexed metadata changes. `StorageStructureChanged`
represents physical directory changes. The watcher observes Created, Changed,
Deleted and Renamed; the periodic directory snapshot is a fallback for Docker
bind mounts that do not reliably propagate filesystem notifications.

## Build and deployment

The MediaStorage Host has no separate React/Vite/Node build stage. Razor Pages
and `StorageUiAssets` are included by the .NET Host build/publish. The dev
Compose service reads the assets from the mounted source tree. The media
`wwwroot` bind mount remains exclusively for user media.

## Manual smoke checks

1. Open `/storage-ui/index.html` through Gateway; check root, nested and empty
   directories, breadcrumbs, direct links, and browser Back/Forward.
2. Verify `.git`, `_trash`, hidden names and symlinks cannot be browsed or previewed.
3. Preview PNG/JPEG/GIF/WebP/AVIF, video, audio, PDF and text; verify text truncation
   at 512 KiB and that HTML/JS/SVG are never active documents.
4. Check video/audio seeking with Range requests.
5. Check multi-upload/progress/drag-drop, move, dry-run, soft delete, trash,
   restore, indexing and selective Git sync.
6. Create/rename/delete an empty folder outside the application and verify
   the tree refreshes through SignalR or the periodic reconciliation.
7. Confirm `StorageUiAssets` is outside the media volume and absent from media
   Git status and `MediaStorageEntry` indexing.
