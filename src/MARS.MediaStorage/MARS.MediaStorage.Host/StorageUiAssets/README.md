# Storage UI assets

Static assets for the Razor-based MediaStorage browser. This directory is deliberately
outside the media `wwwroot` bind mount so that UI files are never indexed or committed
to the user's media repository.

The host serves it from `/storage-ui/assets` and copies it to output/publish via the
Host project file. The legacy Vite UI remains served from `/storage-ui` until the
functional-parity and cutover stages are complete.
