#!/usr/bin/env bash
# Telemetry overhead benchmark (task 520).
# Publishes the off/on apps (AOT and JIT), starts the local OTLP receiver, runs hyperfine
# over cases 1 to 5 plus the JIT comparison, collects the in-process timeline, and writes
# results/<date>-telemetry-overhead.md. Needs hyperfine, curl, and the .NET SDK.
# Case 5 (blackholed endpoint, task 590) checks the flush budget.
#
# Knobs (environment):
#   FAST_RUNS / FAST_WARMUP   runs for the cases without an endpoint (default 500 / 20)
#   SLOW_RUNS / SLOW_WARMUP   runs for the cases with an endpoint (default 30 / 3)
#   TIMELINE_RUNS             runs per case for the timeline breakdown (default 30)
#   GRPC_PORT / HTTP_PORT     receiver ports (default 24317 / 24318)
#   CLOSED_PORT               port with nothing listening, for case 4 (default 24319)
#   BLACKHOLE_ENDPOINT        endpoint that drops packets, for case 5 (default http://192.0.2.1:4317,
#                             TEST-NET-1); empty skips case 5
#   BLACKHOLE_RUNS            runs for case 5 (default 5)
#   SKIP_PUBLISH=1            reuse publish/ from an earlier run
set -euo pipefail
cd "$(dirname "$0")"

FAST_RUNS=${FAST_RUNS:-500}
FAST_WARMUP=${FAST_WARMUP:-20}
SLOW_RUNS=${SLOW_RUNS:-30}
SLOW_WARMUP=${SLOW_WARMUP:-3}
TIMELINE_RUNS=${TIMELINE_RUNS:-30}
GRPC_PORT=${GRPC_PORT:-24317}
HTTP_PORT=${HTTP_PORT:-24318}
CLOSED_PORT=${CLOSED_PORT:-24319}
BLACKHOLE_ENDPOINT=${BLACKHOLE_ENDPOINT-http://192.0.2.1:4317}
BLACKHOLE_RUNS=${BLACKHOLE_RUNS:-5}

for tool in hyperfine curl dotnet; do
  command -v "$tool" > /dev/null || { echo "error: $tool not found on PATH" >&2; exit 1; }
done

# Only the variables each case sets may reach the apps.
while IFS= read -r name; do unset "$name"; done < <(env | sed -n 's/^\(OTEL_[A-Z0-9_]*\)=.*/\1/p')

port_open() { (exec 3<> "/dev/tcp/127.0.0.1/$1") 2> /dev/null; }
if port_open "$CLOSED_PORT"; then
  echo "error: CLOSED_PORT $CLOSED_PORT has a listener; pick a closed port" >&2
  exit 1
fi
for port in "$GRPC_PORT" "$HTTP_PORT"; do
  if port_open "$port"; then
    echo "error: port $port is in use; stop that listener or set GRPC_PORT / HTTP_PORT" >&2
    exit 1
  fi
done

if [[ "${SKIP_PUBLISH:-0}" != 1 ]]; then
  for app in bench-telemetry-off bench-telemetry-on; do
    echo "== publish $app (AOT)"
    dotnet publish "$app" -c Release -o "publish/aot/$app" -v quiet -nologo
    echo "== publish $app (JIT)"
    dotnet publish "$app" -c Release -p:PublishAot=false -o "publish/jit/$app" -v quiet -nologo
  done
fi

AOT_OFF=publish/aot/bench-telemetry-off/bench-telemetry-off
AOT_ON=publish/aot/bench-telemetry-on/bench-telemetry-on
JIT_OFF=publish/jit/bench-telemetry-off/bench-telemetry-off.dll
JIT_ON=publish/jit/bench-telemetry-on/bench-telemetry-on.dll
DOTNET=$(command -v dotnet)
ARGS='greet --name x'

GRPC_ENDPOINT="http://127.0.0.1:$GRPC_PORT"
HTTP_ENDPOINT="http://127.0.0.1:$HTTP_PORT"
CLOSED_ENDPOINT="http://127.0.0.1:$CLOSED_PORT"

# Every command runs through env so each case pays the same exec.
NO_ENDPOINT='env -u OTEL_EXPORTER_OTLP_ENDPOINT'
GRPC_ENV="env OTEL_EXPORTER_OTLP_PROTOCOL=grpc OTEL_EXPORTER_OTLP_ENDPOINT=$GRPC_ENDPOINT"
HTTP_ENV="env OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf OTEL_EXPORTER_OTLP_ENDPOINT=$HTTP_ENDPOINT"
CLOSED_ENV="env OTEL_EXPORTER_OTLP_PROTOCOL=grpc OTEL_EXPORTER_OTLP_ENDPOINT=$CLOSED_ENDPOINT"
BLACKHOLE_ENV="env OTEL_EXPORTER_OTLP_PROTOCOL=grpc OTEL_EXPORTER_OTLP_ENDPOINT=$BLACKHOLE_ENDPOINT"

echo "== start otlp-receiver"
RECEIVER_LOG=$(mktemp)
dotnet run otlp-receiver/otlp-receiver.cs -- "$GRPC_PORT" "$HTTP_PORT" > "$RECEIVER_LOG" 2>&1 &
RECEIVER_PID=$!
cleanup() {
  pkill -P "$RECEIVER_PID" 2> /dev/null || true
  kill "$RECEIVER_PID" 2> /dev/null || true
  rm -f "$RECEIVER_LOG"
}
trap cleanup EXIT
for _ in $(seq 1 120); do
  curl -sf "$HTTP_ENDPOINT/stats" > /dev/null && break
  kill -0 "$RECEIVER_PID" 2> /dev/null || { cat "$RECEIVER_LOG" >&2; exit 1; }
  sleep 1
done
curl -sf "$HTTP_ENDPOINT/stats" > /dev/null || { echo "error: receiver did not start" >&2; exit 1; }

DATE=$(date +%Y-%m-%d)
RAW="results/raw/$DATE"
mkdir -p "$RAW"

echo "== hyperfine: cases without an endpoint"
hyperfine -N --warmup "$FAST_WARMUP" --runs "$FAST_RUNS" \
  --export-markdown "$RAW/fast.md" --export-json "$RAW/fast.json" \
  -n '1 off (AOT)' "$NO_ENDPOINT $AOT_OFF $ARGS" \
  -n '2 on, no endpoint (AOT)' "$NO_ENDPOINT $AOT_ON $ARGS" \
  -n '1 off (JIT)' "$NO_ENDPOINT $DOTNET $JIT_OFF $ARGS" \
  -n '2 on, no endpoint (JIT)' "$NO_ENDPOINT $DOTNET $JIT_ON $ARGS"

echo "== hyperfine: cases with an endpoint"
hyperfine -N --warmup "$SLOW_WARMUP" --runs "$SLOW_RUNS" \
  --export-markdown "$RAW/slow.md" --export-json "$RAW/slow.json" \
  -n '3 on, reachable, grpc (AOT)' "$GRPC_ENV $AOT_ON $ARGS" \
  -n '3 on, reachable, http/protobuf (AOT)' "$HTTP_ENV $AOT_ON $ARGS" \
  -n '4 on, unreachable, grpc (AOT)' "$CLOSED_ENV $AOT_ON $ARGS" \
  -n '3 on, reachable, grpc (JIT)' "$GRPC_ENV $DOTNET $JIT_ON $ARGS"

if [[ -n "$BLACKHOLE_ENDPOINT" ]]; then
  echo "== hyperfine: blackholed endpoint"
  hyperfine -N --warmup 1 --runs "$BLACKHOLE_RUNS" \
    --export-markdown "$RAW/blackhole.md" --export-json "$RAW/blackhole.json" \
    -n '5 on, blackholed, grpc (AOT)' "$BLACKHOLE_ENV $AOT_ON $ARGS" \
    -n '5 on, blackholed, OTEL_EXPORTER_OTLP_TIMEOUT=500 (AOT)' "$BLACKHOLE_ENV OTEL_EXPORTER_OTLP_TIMEOUT=500 $AOT_ON $ARGS"
fi

curl -s "$HTTP_ENDPOINT/stats" > "$RAW/receiver-stats.txt"

# Timeline: mean ms from Main start to handler entry (provider setup lands here) and from
# handler entry to RunAsync returning (handler, output, provider shutdown and dispose).
timeline() {
  local label=$1
  shift
  local i
  for i in $(seq 1 "$TIMELINE_RUNS"); do
    NURU_BENCH_TIMELINE=1 "$@" 2>&1 > /dev/null | grep '^timeline '
  done | awk -v label="$label" '
    { split($2, h, "="); split($3, e, "="); hs += h[2]; ts += e[2] - h[2]; n++ }
    END { printf "| %s | %.2f | %.2f |\n", label, hs / n, ts / n }'
}

echo "== timeline"
{
  echo "| Case | Main to handler [ms] | Handler to exit [ms] |"
  echo "|:---|---:|---:|"
  timeline '1 off (AOT)' $NO_ENDPOINT $AOT_OFF $ARGS
  timeline '2 on, no endpoint (AOT)' $NO_ENDPOINT $AOT_ON $ARGS
  timeline '3 on, reachable, grpc (AOT)' $GRPC_ENV $AOT_ON $ARGS
  timeline '3 on, reachable, http/protobuf (AOT)' $HTTP_ENV $AOT_ON $ARGS
  timeline '4 on, unreachable, grpc (AOT)' $CLOSED_ENV $AOT_ON $ARGS
  timeline '1 off (JIT)' $NO_ENDPOINT "$DOTNET" $JIT_OFF $ARGS
  timeline '2 on, no endpoint (JIT)' $NO_ENDPOINT "$DOTNET" $JIT_ON $ARGS
  timeline '3 on, reachable, grpc (JIT)' $GRPC_ENV "$DOTNET" $JIT_ON $ARGS
} > "$RAW/timeline.md"

NURU_VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' ../../source/Directory.Build.props | head -1)
OTEL_VERSION=$(sed -n 's:.*Include="OpenTelemetry" Version="\([^"]*\)".*:\1:p' ../../Directory.Packages.props | head -1)
OUT="results/$DATE-telemetry-overhead.md"
{
  echo "# Telemetry overhead benchmark ($DATE)"
  echo
  echo "Generated by \`run-benchmark.sh\`. Command in every case: \`greet --name x\`."
  echo
  echo "## Environment"
  echo
  echo "| Item | Value |"
  echo "|:---|:---|"
  echo "| Machine | $(lscpu | sed -n 's/^Model name: *//p' | head -1), $(nproc) logical CPUs |"
  echo "| OS | $(. /etc/os-release && echo "$PRETTY_NAME"), kernel $(uname -r) |"
  echo "| .NET SDK | $(dotnet --version) |"
  echo "| Nuru | $NURU_VERSION (this checkout, $(git rev-parse --short HEAD)) |"
  echo "| OpenTelemetry | $OTEL_VERSION |"
  echo "| hyperfine | $(hyperfine --version | cut -d' ' -f2), \`-N\` |"
  echo "| Receiver | \`otlp-receiver/otlp-receiver.cs\` on 127.0.0.1:$GRPC_PORT (gRPC) and :$HTTP_PORT (HTTP) |"
  echo "| Closed port (case 4) | 127.0.0.1:$CLOSED_PORT |"
  echo "| Blackholed endpoint (case 5) | ${BLACKHOLE_ENDPOINT:-skipped} |"
  echo
  echo "## Binary size"
  echo
  echo "| App | AOT binary |"
  echo "|:---|---:|"
  echo "| bench-telemetry-off | $(du -h "$AOT_OFF" | cut -f1) |"
  echo "| bench-telemetry-on | $(du -h "$AOT_ON" | cut -f1) |"
  echo
  echo "## Cases without an endpoint ($FAST_RUNS runs, $FAST_WARMUP warmup)"
  echo
  cat "$RAW/fast.md"
  echo
  echo "## Cases with an endpoint ($SLOW_RUNS runs, $SLOW_WARMUP warmup)"
  echo
  cat "$RAW/slow.md"
  echo
  if [[ -n "$BLACKHOLE_ENDPOINT" ]]; then
    echo "## Blackholed endpoint ($BLACKHOLE_RUNS runs, 1 warmup)"
    echo
    cat "$RAW/blackhole.md"
    echo
  fi
  echo "## Timeline (mean of $TIMELINE_RUNS runs, ms since Main started)"
  echo
  cat "$RAW/timeline.md"
  echo
  echo "## Receiver export counts"
  echo
  echo '```text'
  cat "$RAW/receiver-stats.txt"
  echo '```'
} > "$OUT"

echo "== wrote $OUT"
