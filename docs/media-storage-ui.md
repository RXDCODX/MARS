# MARS.MediaStorage UI

## Public URL and hosting

The MediaStorage browser is a server-rendered ASP.NET Core Razor Page at
`/storage-ui/index.html`. The compatibility route `/storage-ui` redirects
to the page. The Gateway continues to proxy `/storage-ui/{**remainder}` to
the `media-storage` service.

Razor markup, its PageModel and CSS are isolated in the
`MARS.MediaStorage.Presentation.Storage` Razor Class Library. The Host
references that presentation project and discovers its pages through an MVC
application part. Static assets are served from the Razor Class Library under
`/_content/MARS.MediaStorage.Presentation.Storage/*`; they are deliberately
outside the media `wwwroot` bind mount so they are not indexed or committed
as user media.

The Razor page follows the standard Page / PageModel convention:
`Pages/Storage/Index.cshtml` contains `@page`, `@model` and the page title;
`Pages/Shared/_Layout.cshtml` owns the document shell, and
`Pages/_ViewStart.cshtml` selects that layout. Reusable UI is split into
`Pages/Storage/Components` partials: header, search, directory tree,
breadcrumbs, file panel, bulk actions, the trash/search/directory tables,
upload form, preview pane and its media-specific content. Components use the
Razor `<partial>` tag helper with a strongly typed `IndexModel`.

The PageModel is also divided by responsibility. Its primary page file owns
the query-bound view state and GET initialization. Separate partial class
files under `Handlers`, `Browsing`, `Navigation` and `Infrastructure`
hold POST actions, directory/preview loading, URL generation, and safe
filesystem/path helpers. This avoids a single oversized .cshtml or code-behind
file while keeping one cohesive Razor Page model.

REST controllers remain in `MARS.MediaStorage.Api.Rest`. Razor handlers call
the Application service contracts directly instead of making HTTP requests
back into the same process. The public API routes remain available at
`/api/MediaStorage/*`.

## Rendering and interaction

The page uses server-side Razor rendering and ordinary GET/POST forms. It
does not load client JavaScript, make fetch/XHR requests, or connect to the
SignalR hub. The following interactions are supported without a client
runtime:

- Navigate physical folders through the folder tree and breadcrumbs.
- Browse indexed and unindexed physical files, and search indexed files with
  server-side pagination.
- Open safe inline previews for images, audio, video and PDF; text previews
  are rendered as encoded text and capped at 512 KiB. HTML, JavaScript and
  SVG remain download-only.
- Upload multiple files, replace a file while retaining its original path,
  download files, and move individual files to the recoverable trash.
- Select indexed files for bulk move, soft-delete, restore and selective
  Git synchronization. Bulk move/delete/restore supports dry-run.
- Adjust the file table's name/type/size column widths through the page form.
- Refresh and re-index manually. The directory hierarchy is scanned and
  preloaded into a server-side in-memory cache when the Host starts.
- Expand and collapse folders using native HTML `<details>` elements. All
  descendants are already included in the rendered response, so expanding a
  node does not issue a request and does not require JavaScript.

The complete tree is cached for 20 seconds. Operations initiated by the page
invalidate it immediately; externally changed folders are picked up after the
cache expires or after the user selects **Обновить**. Folder contents and file
metadata continue to use their own 20-second cache.

## Safety and storage model

- The filesystem under `WebRootPath` is the source of truth for folders,
  physical files, file size and last-write time. Empty directories are visible.
- PostgreSQL `MediaStorageEntry` supplies IDs and metadata for indexed files.
  A physical file without an entry remains visible but cannot participate in
  ID-based bulk operations until indexing.
- Git state is provided by `IMediaGitService`.
- `.git`, `_trash`, dot-prefixed path segments and symlinks/reparse points
  are excluded from browsing. Preview and path-based actions validate relative
  paths and reject traversal outside the media root.
- Text is emitted through Razor's normal HTML encoding, never inserted as raw
  HTML. Inline binary preview is delegated to the API's allowlisted endpoint.

## Development and containers

The Host uses the supported .NET Hot Reload workflow through `dotnet watch`.
Razor files in the presentation project are compiled by the Razor SDK; the CSS
is a Razor Class Library static web asset. Do not add
`Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` or call
`AddRazorRuntimeCompilation`: runtime compilation is obsolete in .NET 10 and
disables the supported Hot Reload workflow.

The MediaStorage Dockerfile must copy the Presentation project file before
`dotnet restore`, then copy the service source before `dotnet build` and
`dotnet publish`. Do not place presentation assets in the media `wwwroot`.

## API compatibility

The existing API contracts stay in `MARS.MediaStorage.Api.Rest`:

| Endpoint | Purpose |
|---|---|
| `GET /api/MediaStorage/browse?directory=` | Direct child folders/files, including unindexed files |
| `GET /api/MediaStorage/search?q=&page=&pageSize=` | Global paginated search over indexed entries |
| `GET /api/MediaStorage/preview?path=` | Safe allowlisted inline image/audio/video/PDF response with Range |
| `GET /api/MediaStorage/text-preview?path=` | Text response capped at 512 KiB |
| `GET /api/MediaStorage/file?path=` | Attachment download |
| `DELETE /api/MediaStorage/file?path=` | Move one file to the recoverable trash |
| `POST /api/MediaStorage/replace` | Replace a file while retaining its original name/path |

Upload, move, soft-delete, restore, index and selective Git-sync API contracts
remain unchanged. The Presentation project does not reference the API project.
