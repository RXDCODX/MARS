#!/usr/bin/env bash
set -uo pipefail

workspace="$GITHUB_WORKSPACE"
if [[ -z "$workspace" ]]; then
  workspace="$(git rev-parse --show-toplevel)"
fi
output_root="$STRYKER_OUTPUT"
if [[ -z "$output_root" ]]; then
  output_root="$RUNNER_TEMP/stryker"
fi
mkdir -p "$output_root"

status_file="$output_root/status.tsv"
printf 'target\ttest_project\tresult\texit_code\n' > "$status_file"
overall_exit_code=0

# Service targets use their matching service test projects.
# Shared/platform targets reuse MARS.Shared.Tests, which references all 11 assemblies.
while IFS='|' read -r test_project target; do
  [[ -z "$test_project" ]] && continue
  project_output="$output_root/$target"
  mkdir -p "$project_output"
  log_file="$project_output/stryker.log"

  echo "::group::Stryker — $target (tests: $test_project)"
  args=(
    --project "$target.csproj"
    --configuration Release
    --break-on-initial-test-failure
    --output "$project_output"
  )
  if [[ -n "$STRYKER_SINCE" ]]; then
    args+=("--since:$STRYKER_SINCE")
    echo "Incremental run since $STRYKER_SINCE"
  else
    echo "Full mutation run"
  fi

  (
    cd "$workspace/tests/$test_project"
    dotnet stryker \
      --config-file "$workspace/.config/stryker-config.json" \
      "${args[@]}"
  ) 2>&1 | tee "$log_file"

  exit_code=$?
  if [[ "$exit_code" -eq 0 ]]; then
    result="success"
    echo "Stryker completed successfully for $target"
  else
    result="failure"
    overall_exit_code=1
    echo "::error::Stryker failed for $target (exit code $exit_code)"
  fi
  printf '%s\t%s\t%s\t%s\n' "$target" "$test_project" "$result" "$exit_code" >> "$status_file"
  echo "::endgroup::"
done <<'TARGETS'
MARS.Admin.Tests|MARS.Admin
MARS.Alerts.Tests|MARS.Alerts
MARS.AudioController.Tests|MARS.AudioController
MARS.CinemaQueue.Tests|MARS.CinemaQueue
MARS.Commands.Tests|MARS.Commands
MARS.Discord.Tests|MARS.Discord
MARS.Gateway.Tests|MARS.Gateway
MARS.MediaStorage.Tests|MARS.MediaStorage
MARS.OBS.Tests|MARS.OBS
MARS.Scoreboard.Tests|MARS.Scoreboard
MARS.Shikimori.Tests|MARS.Shikimori
MARS.SoundRequest.Tests|MARS.SoundRequest
MARS.Telegram.Tests|MARS.Telegram
MARS.TTS.Tests|MARS.TTS
MARS.TwitchCore.Tests|MARS.TwitchCore
MARS.Videos365.Tests|MARS.Videos365
MARS.WaifuGacha.Tests|MARS.WaifuGacha
MARS.Shared.Tests|MARS.Shared.Kernel
MARS.Shared.Tests|MARS.Shared.Contracts
MARS.Shared.Tests|MARS.Platform.Clients.Http.Infrastructure
MARS.Shared.Tests|MARS.Platform.Development.Infrastructure
MARS.Shared.Tests|MARS.Platform.Grpc.Infrastructure
MARS.Shared.Tests|MARS.Platform.Hosting.Infrastructure
MARS.Shared.Tests|MARS.Platform.Messaging.RabbitMq.Infrastructure
MARS.Shared.Tests|MARS.Platform.Observability.Logging.Infrastructure
MARS.Shared.Tests|MARS.Platform.Observability.Telemetry.Infrastructure
MARS.Shared.Tests|MARS.Platform.Persistence.Postgres.Infrastructure
MARS.Shared.Tests|MARS.Platform.Web.AspNetCore.Infrastructure
TARGETS

exit "$overall_exit_code"
