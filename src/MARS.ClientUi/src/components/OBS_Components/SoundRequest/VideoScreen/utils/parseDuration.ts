export function parseDurationToSeconds(duration?: string): number {
  if (duration) {
    const parts = duration.split(":");

    if (parts.length === 3) {
      const [hours, minutes, seconds] = parts.map(Number);
      if (
        Number.isFinite(hours) &&
        Number.isFinite(minutes) &&
        Number.isFinite(seconds)
      ) {
        return hours * 3600 + minutes * 60 + seconds;
      }
    }

    if (parts.length === 2) {
      const [minutes, seconds] = parts.map(Number);
      if (Number.isFinite(minutes) && Number.isFinite(seconds)) {
        return minutes * 60 + seconds;
      }
    }

    const match = duration.match(
      /PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/
    );
    if (match) {
      const hours = Number(match[1] ?? "0");
      const minutes = Number(match[2] ?? "0");
      const seconds = Number(match[3] ?? "0");
      if (
        Number.isFinite(hours) &&
        Number.isFinite(minutes) &&
        Number.isFinite(seconds)
      ) {
        return hours * 3600 + minutes * 60 + seconds;
      }
    }
  }

  return 0;
}
