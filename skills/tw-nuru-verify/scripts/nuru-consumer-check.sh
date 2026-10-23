#!/usr/bin/env bash
# nuru-consumer-check.sh: install a shipped TimeWarp.Nuru the way a consumer does, drive it,
# and write evidence. Read ../SKILL.md first. The source (code + Purpose/Design regions) is the
# authority; docs are not. Exit 0 = launch, cache, probes and cli all ran and passed (docs too
# with --docs-strict), 1 = one of those failed, 2 = doctor refused to run. Doc snippet failures
# are DOC-DEFECT rows in their own summary section and do not change the exit code by default.
set -uo pipefail

NUGET_ORG="https://api.nuget.org/v3/index.json"
FLAT="https://api.nuget.org/v3-flatcontainer"

VERSION="latest"
SOURCE="$NUGET_ORG"
STEPS="cache,probes,cli,docs"
DOCS="all"
DOCS_REF=""
REPO=""
OUT=""
GLOBAL_CACHE="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
WORK_ROOT="${NURU_VERIFY_WORK:-${XDG_CACHE_HOME:-$HOME/.cache}/nuru-verify}"
KEEP=0
DOCS_STRICT=0

usage() {
  cat <<'EOF'
Usage: nuru-consumer-check.sh [options]

  --version V       Package version to check (default: latest on nuget.org)
  --source S        NuGet source URL or local feed folder (default: nuget.org)
  --steps LIST      Comma list of: cache,probes,cli,docs (default: all four)
  --docs MODE       readme | all (default: all)
  --docs-ref REF    Git ref the docs are read from (default: tag v<version>)
  --repo PATH       timewarp-nuru checkout holding the docs (default: git root of cwd)
  --out DIR         Evidence folder (default: <repo>/artifacts/nuru-verify/<version>)
  --global-cache D  Global NuGet cache to audit (default: $NUGET_PACKAGES or ~/.nuget/packages)
  --docs-strict     Docs failures (DOC-DEFECT) fail the run; default is report-only
  --keep            Keep the scratch work folder
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version) VERSION="$2"; shift 2 ;;
    --source) SOURCE="$2"; shift 2 ;;
    --steps) STEPS="$2"; shift 2 ;;
    --docs) DOCS="$2"; shift 2 ;;
    --docs-ref) DOCS_REF="$2"; shift 2 ;;
    --repo) REPO="$2"; shift 2 ;;
    --out) OUT="$2"; shift 2 ;;
    --global-cache) GLOBAL_CACHE="$2"; shift 2 ;;
    --docs-strict) DOCS_STRICT=1; shift ;;
    --keep) KEEP=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "unknown option: $1" >&2; usage >&2; exit 2 ;;
  esac
done

SKILL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROBES_DIR="$SKILL_DIR/probes"
has_step() { [[ ",$STEPS," == *",$1,"* ]]; }

# ---------------------------------------------------------------------------------------------
# Results
# ---------------------------------------------------------------------------------------------
RESULTS=()   # "step|check|status|evidence|detail"
FAILED=0
record() {   # record <step> <check> <PASS|FAIL> <evidence> [detail]
  # Exit 0 must mean every selected check ran and passed, so anything but PASS fails the run.
  # Docs are not the authority: a docs failure is a DOC-DEFECT and fails the run only with
  # --docs-strict. Pass FAIL to record; the docs step is downgraded here.
  if [[ "$1" == "docs" && "$3" == "FAIL" ]]; then
    set -- "$1" "$2" "DOC-DEFECT" "$4" "${5:-}"
    [[ $DOCS_STRICT -eq 1 ]] && FAILED=1
  elif [[ "$3" != "PASS" ]]; then
    FAILED=1
  fi
  RESULTS+=("$1|$2|$3|$4|${5:-}")
  printf '%-7s %-12s %s %s\n' "$3" "$1" "$2" "${5:+($5)}"
}

# run_logged <log> <cmd...>: run a command, write "$ cmd", stdout, stderr and the exit code to
# <log>. Sets RUN_RC, RUN_OUT (stdout + stderr) for the caller.
RUN_RC=0
RUN_OUT=""
run_logged() {
  local log="$1"; shift
  local tmp_out tmp_err
  tmp_out="$(mktemp)"; tmp_err="$(mktemp)"
  "$@" >"$tmp_out" 2>"$tmp_err"
  RUN_RC=$?
  {
    printf '$ %s\n' "$*"
    printf -- '--- stdout\n'; cat "$tmp_out"
    printf -- '--- stderr\n'; cat "$tmp_err"
    printf -- '--- exit: %s\n' "$RUN_RC"
  } >>"$log"
  RUN_OUT="$(cat "$tmp_out" "$tmp_err")"
  rm -f "$tmp_out" "$tmp_err"
}

# ---------------------------------------------------------------------------------------------
# Resolve inputs
# ---------------------------------------------------------------------------------------------
is_local_source() { [[ "$SOURCE" != http://* && "$SOURCE" != https://* ]]; }

nuget_org_versions() {  # nuget_org_versions <lowercase id>
  curl -fsS "$FLAT/$1/index.json" 2>/dev/null | tr -d ' \n' | grep -o '"[^"]*"' | tr -d '"' | grep -v '^versions$'
}

if [[ "$VERSION" == "latest" ]]; then
  if is_local_source; then
    echo "doctor: --version is required with a local feed" >&2; exit 2
  fi
  VERSION="$(nuget_org_versions timewarp.nuru | tail -1)"
  [[ -n "$VERSION" ]] || { echo "doctor: could not read the latest version from nuget.org" >&2; exit 2; }
fi

if [[ -z "$REPO" ]]; then
  REPO="$(git rev-parse --show-toplevel 2>/dev/null || true)"
  [[ -n "$REPO" && -d "$REPO/source/timewarp-nuru" ]] || REPO=""
fi
[[ -n "$DOCS_REF" ]] || DOCS_REF="v$VERSION"
if [[ -z "$OUT" ]]; then
  if [[ -n "$REPO" ]]; then OUT="$REPO/artifacts/nuru-verify/$VERSION"; else OUT="$PWD/nuru-verify/$VERSION"; fi
fi

# ---------------------------------------------------------------------------------------------
# Doctor: refuse when the environment would make a pass meaningless
# ---------------------------------------------------------------------------------------------
doctor_fail() { echo "doctor: $*" >&2; exit 2; }

for tool in dotnet curl; do command -v "$tool" >/dev/null || doctor_fail "$tool is not on PATH"; done
has_step docs && { command -v git >/dev/null || doctor_fail "git is not on PATH (needed for --steps docs)"; }
# A case that cannot run is not a pass. Refuse up front rather than skip it and exit 0.
if has_step probes; then
  cut -f3 "$PROBES_DIR/cases.tsv" | grep -qx pty && { command -v script >/dev/null || doctor_fail "script(1) is not on PATH (needed for the pty cases: AddRepl)"; }
  cut -f3 "$PROBES_DIR/cases.tsv" | grep -qx otlp && { command -v nc >/dev/null || doctor_fail "nc is not on PATH (needed for the otlp cases: UseTelemetry)"; }
fi
has_step cli && ! has_step probes && doctor_fail "--steps cli needs probes (nuru search indexes the core-routing probe app)"

if [[ -e "$OUT" && ! -e "$OUT/.nuru-verify" && -n "$(ls -A "$OUT" 2>/dev/null)" ]]; then
  doctor_fail "$OUT exists and was not written by this check; pass --out"
fi

STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
WORK="$WORK_ROOT/$VERSION-$STAMP-$$"
mkdir -p "$WORK_ROOT" || doctor_fail "cannot create $WORK_ROOT"

# MSBuild walks up from the project folder and imports the first Directory.Build.* and
# Directory.Packages.props it meets. A stray one turns this into a repo build, not a consumer one.
dir="$WORK_ROOT"
while [[ "$dir" != "/" ]]; do
  for f in Directory.Build.props Directory.Build.targets Directory.Packages.props Directory.Build.rsp; do
    [[ -e "$dir/$f" ]] && doctor_fail "$dir/$f would be imported by the consumer projects; move it or set NURU_VERIFY_WORK"
  done
  dir="$(dirname "$dir")"
done

if is_local_source; then
  [[ -d "$SOURCE" ]] || doctor_fail "local feed $SOURCE does not exist"
  SOURCE="$(cd "$SOURCE" && pwd)"
  # state#647: a local pack under a version nuget.org already serves is the poisoning pattern.
  if nuget_org_versions timewarp.nuru | grep -qx "$VERSION"; then
    doctor_fail "$VERSION is a released version on nuget.org; pack local candidates under a unique prerelease version"
  fi
else
  if [[ "$SOURCE" == "$NUGET_ORG" ]]; then
    nuget_org_versions timewarp.nuru | grep -qx "$VERSION" || doctor_fail "TimeWarp.Nuru $VERSION is not on nuget.org"
  fi
fi

if has_step docs; then
  [[ -n "$REPO" ]] || doctor_fail "--steps docs needs a timewarp-nuru checkout; pass --repo or drop docs from --steps"
  git -C "$REPO" rev-parse --verify --quiet "$DOCS_REF^{commit}" >/dev/null \
    || doctor_fail "docs ref $DOCS_REF not found in $REPO (git fetch --tags, or pass --docs-ref)"
fi

# The apps and the CLI run with HOME=$WORK/home. It must exist: .NET resolves a missing home
# folder to "", and Nuru then writes .nuru/ (REPL history, search index) into the cwd.
mkdir -p "$WORK/home" || doctor_fail "cannot create $WORK"
rm -rf "$OUT"
mkdir -p "$OUT"
touch "$OUT/.nuru-verify"

# Isolation: a private package cache, HTTP cache and NuGet config. Nothing below reads or writes
# the global cache; the cache step only reads it to compare.
export NUGET_PACKAGES="$WORK/nuget-packages"
export NUGET_HTTP_CACHE_PATH="$WORK/http-cache"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0
cat >"$WORK/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="consumer-source" value="$SOURCE" />
  </packageSources>
</configuration>
EOF

{
  echo "version=$VERSION"
  echo "source=$SOURCE"
  echo "docs_ref=$DOCS_REF"
  echo "repo=$REPO"
  echo "work=$WORK"
  echo "nuget_packages=$NUGET_PACKAGES"
  echo "global_cache_audited=$GLOBAL_CACHE"
  echo "dotnet=$(dotnet --version)"
  echo "started=$STAMP"
} >"$OUT/environment.txt"

echo "nuru-consumer-check: TimeWarp.Nuru $VERSION from $SOURCE"
echo "  work: $WORK"
echo "  evidence: $OUT"

# consumer_csproj <dir> <name> [extra package ids...]: the project a consumer gets from
# dotnet new console + dotnet add package. Extra ids are sibling TimeWarp.Nuru packages at $VERSION.
consumer_csproj() {
  local dir="$1" name="$2"; shift 2
  local refs="    <PackageReference Include=\"TimeWarp.Nuru\" Version=\"$VERSION\" />" id
  for id in "$@"; do refs+=$'\n'"    <PackageReference Include=\"$id\" Version=\"$VERSION\" />"; done
  cat >"$dir/$name.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
$refs
  </ItemGroup>
</Project>
EOF
}

# package_available <id>: whether <id> $VERSION exists at the source. A missing sibling package
# would fail the restore of every snippet in the solution build, so only real ones are referenced.
declare -A AVAILABLE=()
package_available() {
  local id="$1" lower
  lower="$(tr '[:upper:]' '[:lower:]' <<<"$id")"
  if [[ -z "${AVAILABLE[$lower]:-}" ]]; then
    if is_local_source; then
      ls "$SOURCE" 2>/dev/null | tr '[:upper:]' '[:lower:]' | grep -qx "$lower.$VERSION.nupkg" && AVAILABLE[$lower]=1 || AVAILABLE[$lower]=0
    else
      nuget_org_versions "$lower" | grep -qx "$VERSION" && AVAILABLE[$lower]=1 || AVAILABLE[$lower]=0
    fi
  fi
  [[ "${AVAILABLE[$lower]}" == 1 ]]
}

# build_solution <dir> <log>: build every */*.csproj under <dir> in one parallel solution build.
build_solution() {
  local dir="$1" log="$2" proj
  {
    echo "<Solution>"
    for proj in "$dir"/*/*.csproj; do echo "  <Project Path=\"${proj#"$dir"/}\" />"; done
    echo "</Solution>"
  } >"$dir/all.slnx"
  run_logged "$log" dotnet build "$dir/all.slnx" -c Release -nr:false -m
}

# project_errors <log> <csproj>: "CODE: message" for each error the build reported for one
# project. The file path, project suffix and generator "Location:" tail are dropped, so a message
# never matches on the scratch path.
project_errors() {
  grep -F "[$2" "$1" | grep -E ': error [A-Z]+[0-9_A-Z]*:' \
    | sed 's/^.*: error //; s/ \[[^]]*\]$//; s/ Location: .*$//' | sort -u
}

# nuru_errors <errors>: the errors that blame a TimeWarp.Nuru API rather than the snippet's own
# missing names. A snippet that only lacks its handlers, services or types is a fragment.
nuru_errors() {
  local errors="$1" undefined=0
  # CS0103/CS0246/CS0234 on a non-Nuru name: the snippet leaves something for the reader to write.
  if grep -E '^CS(0103|0246|0234):' <<<"$errors" | grep -qviE 'nuru'; then undefined=1; fi
  {
    # The generator rejects a builder method, or a route pattern, the docs teach.
    grep -E '^NURU_S999: Unrecognized DSL method|^NURU_R[0-9]+:' <<<"$errors"
    # Other generator errors cascade from undefined names; without any, they blame the API.
    [[ $undefined -eq 0 ]] && grep -E '^NURU_' <<<"$errors"
    # A symbol exists in a referenced assembly but the snippet may not use it.
    grep -E '^CS0122:' <<<"$errors"
    # The compiler names a Nuru type or member.
    grep -E '^CS[0-9]+:' <<<"$errors" | grep -iE 'nuru|CreateBuilder'
    # A call shape or member that does not exist, even when the message names no Nuru type
    # (nuru#296: the pre-3.0 two-argument Map(pattern, handler) is CS1501 "No overload for method
    # 'Map' takes 2 arguments"). A snippet's own undefined names show up as CS0103/CS0246 instead.
    grep -E '^CS1501:' <<<"$errors"
    # Missing members, except on host extension points: docs call their own or another package's
    # extensions there (services.AddDatabase(), logging.AddOpenTelemetry()) and leave the using
    # directive to the reader. A missing Nuru extension still names Nuru and is caught above.
    grep -E '^CS(1061|0117):' <<<"$errors" \
      | grep -vE "^CS[0-9]+: '(IServiceCollection|ILoggingBuilder|IConfigurationBuilder|IHostBuilder|IHostApplicationBuilder|WebApplicationBuilder|IMcpServerBuilder)'"
  } | sort -u
}

# ---------------------------------------------------------------------------------------------
# Launch: restore the shipped package into the private cache, as a consumer would
# ---------------------------------------------------------------------------------------------
mkdir -p "$OUT/launch"
run_logged "$OUT/launch/new-console.log" dotnet new console -o "$WORK/consumer" --framework net10.0
run_logged "$OUT/launch/add-package.log" dotnet add "$WORK/consumer/consumer.csproj" package TimeWarp.Nuru --version "$VERSION"
if [[ $RUN_RC -ne 0 ]]; then
  record launch "dotnet add package TimeWarp.Nuru $VERSION" FAIL launch/add-package.log "restore failed"
else
  meta="$NUGET_PACKAGES/timewarp.nuru/$VERSION/.nupkg.metadata"
  resolved="$(grep -o '"source": *"[^"]*"' "$meta" 2>/dev/null | sed 's/.*: *"//; s/"$//')"
  cp "$meta" "$OUT/launch/timewarp.nuru.nupkg.metadata" 2>/dev/null
  if [[ "${resolved%/}" == "${SOURCE%/}" ]]; then
    record launch "restore resolves TimeWarp.Nuru $VERSION from the intended source" PASS launch/timewarp.nuru.nupkg.metadata "$resolved"
  else
    record launch "restore resolves TimeWarp.Nuru $VERSION from the intended source" FAIL launch/timewarp.nuru.nupkg.metadata "resolved from '${resolved:-unknown}'"
  fi
fi

# ---------------------------------------------------------------------------------------------
# Cache (state#647): every package the consumer restored must match the global cache entry
# ---------------------------------------------------------------------------------------------
if has_step cache; then
  mkdir -p "$OUT/cache"
  report="$OUT/cache/global-cache-audit.tsv"
  printf 'package\tversion\tstatus\tglobal_source\tfresh_hash\tglobal_hash\n' >"$report"
  mismatches=0; checked=0; absent=0
  for fresh_meta in "$NUGET_PACKAGES"/*/*/.nupkg.metadata; do
    [[ -e "$fresh_meta" ]] || continue
    ver_dir="$(dirname "$fresh_meta")"; ver="$(basename "$ver_dir")"; id="$(basename "$(dirname "$ver_dir")")"
    fresh_hash="$(grep -o '"contentHash": *"[^"]*"' "$fresh_meta" | sed 's/.*: *"//; s/"$//')"
    global_meta="$GLOBAL_CACHE/$id/$ver/.nupkg.metadata"
    if [[ ! -e "$global_meta" ]]; then
      absent=$((absent + 1))
      printf '%s\t%s\tnot-cached\t-\t%s\t-\n' "$id" "$ver" "$fresh_hash" >>"$report"
      continue
    fi
    checked=$((checked + 1))
    global_hash="$(grep -o '"contentHash": *"[^"]*"' "$global_meta" | sed 's/.*: *"//; s/"$//')"
    global_source="$(grep -o '"source": *"[^"]*"' "$global_meta" | sed 's/.*: *"//; s/"$//')"
    if [[ "$fresh_hash" == "$global_hash" ]]; then
      printf '%s\t%s\tmatch\t%s\t%s\t%s\n' "$id" "$ver" "$global_source" "$fresh_hash" "$global_hash" >>"$report"
    else
      mismatches=$((mismatches + 1))
      printf '%s\t%s\tPOISONED\t%s\t%s\t%s\n' "$id" "$ver" "$global_source" "$fresh_hash" "$global_hash" >>"$report"
      record cache "global cache $id $ver matches the published package" FAIL cache/global-cache-audit.tsv "global entry came from $global_source; delete $GLOBAL_CACHE/$id/$ver"
    fi
  done
  if [[ $mismatches -eq 0 ]]; then
    record cache "global cache entries match the published packages" PASS cache/global-cache-audit.tsv "$checked matched, $absent not cached"
  fi
fi

# ---------------------------------------------------------------------------------------------
# Probes (nuru#242): one small app per advertised builder API, each run with an expected effect
# ---------------------------------------------------------------------------------------------
if has_step probes; then
  mkdir -p "$OUT/probes" "$WORK/probes"
  cp "$WORK/nuget.config" "$WORK/probes/nuget.config"
  for src in "$PROBES_DIR"/*.cs; do
    name="$(basename "$src" .cs)"
    mkdir -p "$WORK/probes/$name"
    consumer_csproj "$WORK/probes/$name" "$name"
    cp "$src" "$WORK/probes/$name/Program.cs"
  done
  build_solution "$WORK/probes" "$OUT/probes/build.log"

  declare -A PROBE_OK=()
  for proj in "$WORK/probes"/*/*.csproj; do
    name="$(basename "$proj" .csproj)"
    errors="$(project_errors "$OUT/probes/build.log" "$proj")"
    if [[ -z "$errors" && -e "$WORK/probes/$name/bin/Release/net10.0/$name.dll" ]]; then
      PROBE_OK[$name]=1
    else
      PROBE_OK[$name]=0
      printf '%s\n' "$errors" >"$OUT/probes/$name.build-errors.txt"
      record probes "$name compiles against the package" FAIL "probes/$name.build-errors.txt" "$(printf '%s' "$errors" | head -1)"
    fi
  done

  case_no=0
  while IFS=$'\t' read -r probe api mode env args stdin exit_code contains not_contains; do
    [[ "$probe" == "probe" || -z "$probe" ]] && continue
    case_no=$((case_no + 1))
    label="$probe: $api"
    log="probes/$(printf '%02d' "$case_no")-$probe.log"
    if [[ "${PROBE_OK[$probe]:-0}" != 1 ]]; then
      record probes "$label" FAIL "probes/$probe.build-errors.txt" "probe did not compile"
      continue
    fi
    dll="$WORK/probes/$probe/bin/Release/net10.0/$probe.dll"
    read -r -a argv <<<"$([[ "$args" == "-" ]] && echo "" || echo "$args")"
    envs=(); [[ "$env" != "-" ]] && envs=("$env")
    haystack=""
    case "$mode" in
      run)
        run_logged "$OUT/$log" env HOME="$WORK/home" "${envs[@]}" timeout 120 dotnet "$dll" "${argv[@]}"
        haystack="$RUN_OUT"
        ;;
      pty)
        cmd="$(printf '%q ' dotnet "$dll" "${argv[@]}")"
        # script(1) gives the REPL a real terminal. Lines are typed the way a person types them:
        # wait for startup, then one line and Enter (CR) at a time. Type-ahead sent before the REPL
        # starts loses its Enter key, so a pipe of all input at once hangs.
        feed="sleep 3"
        IFS=$'\x1f' read -r -a lines <<<"${stdin//\\n/$'\x1f'}"
        for line in "${lines[@]}"; do
          [[ -n "$line" ]] && feed+="; printf '%s\\r' $(printf '%q' "$line"); sleep 2"
        done
        run_logged "$OUT/$log" bash -c "($feed) | HOME=$(printf '%q' "$WORK/home") timeout 60 script -qec $(printf '%q' "$cmd") /dev/null"
        haystack="$RUN_OUT"
        # Keep the transcript readable: drop the ANSI redraw codes the REPL writes on every key.
        sed -i 's/\x1b\[[0-9;?]*[A-Za-z]//g; s/\x1b[=>]//g' "$OUT/$log"
        ;;
      otlp)
        # The probe sets OtlpEndpoint as a literal (the generator only reads literals); listen there.
        port="$(grep -o '127\.0\.0\.1:[0-9]*' "$PROBES_DIR/$probe.cs" | head -1 | cut -d: -f2)"
        capture="$OUT/probes/$probe.otlp-capture.bin"
        nc -l 127.0.0.1 "$port" >"$capture" 2>/dev/null &
        nc_pid=$!
        sleep 0.5
        run_logged "$OUT/$log" env HOME="$WORK/home" timeout 60 dotnet "$dll" "${argv[@]}"
        kill "$nc_pid" 2>/dev/null; wait "$nc_pid" 2>/dev/null
        haystack="$(LC_ALL=C tr -c '[:print:]' '.' <"$capture")"
        printf -- '--- otlp listener capture (printable bytes)\n%s\n' "$haystack" >>"$OUT/$log"
        ;;
    esac
    problems=()
    [[ "$RUN_RC" == "$exit_code" ]] || problems+=("exit $RUN_RC, expected $exit_code")
    [[ "$contains" == "-" || "$haystack" == *"$contains"* ]] || problems+=("missing '$contains'")
    [[ "$not_contains" == "-" || "$haystack" != *"$not_contains"* ]] || problems+=("unexpected '$not_contains'")
    if [[ ${#problems[@]} -eq 0 ]]; then
      record probes "$label" PASS "$log"
    else
      record probes "$label" FAIL "$log" "$(IFS=';'; echo "${problems[*]}")"
    fi
  done <"$PROBES_DIR/cases.tsv"
fi

# ---------------------------------------------------------------------------------------------
# CLI: install the shipped nuru search tool and point it at a consumer app built from the package
# ---------------------------------------------------------------------------------------------
if has_step cli; then
  mkdir -p "$OUT/cli"
  tool_dir="$WORK/tools"
  run_logged "$OUT/cli/install.log" dotnet tool install TimeWarp.Nuru.Search --version "$VERSION" --tool-path "$tool_dir" --configfile "$WORK/nuget.config"
  if [[ $RUN_RC -ne 0 ]]; then
    record cli "dotnet tool install TimeWarp.Nuru.Search $VERSION" FAIL cli/install.log "install failed"
  else
    record cli "dotnet tool install TimeWarp.Nuru.Search $VERSION" PASS cli/install.log
    nuru=("$tool_dir/nuru")
    run_logged "$OUT/cli/version.log" env HOME="$WORK/home" "${nuru[@]}" --version
    if [[ $RUN_RC -eq 0 && "$RUN_OUT" == *"${VERSION%%-*}"* ]]; then
      record cli "nuru --version reports $VERSION" PASS cli/version.log
    else
      record cli "nuru --version reports $VERSION" FAIL cli/version.log "exit $RUN_RC"
    fi
    target="$WORK/probes/core-routing/bin/Release/net10.0/core-routing"
    if [[ -x "$target" ]]; then
      run_logged "$OUT/cli/index.log" env HOME="$WORK/home" "${nuru[@]}" index rebuild --cli "$target"
      run_logged "$OUT/cli/search.log" env HOME="$WORK/home" "${nuru[@]}" search --query "integers"
      if [[ $RUN_RC -eq 0 && "$RUN_OUT" == *"add {x:int} {y:int}"* ]]; then
        record cli "nuru indexes and finds a route in an app built from the package" PASS cli/search.log
      else
        record cli "nuru indexes and finds a route in an app built from the package" FAIL cli/search.log "exit $RUN_RC; route not found"
      fi
    else
      record cli "nuru indexes and finds a route in an app built from the package" FAIL probes/core-routing.build-errors.txt "core-routing probe did not build"
    fi
  fi
fi

# ---------------------------------------------------------------------------------------------
# Docs (nuru#296): compile every C# snippet that builds an app, from the docs of this release
# ---------------------------------------------------------------------------------------------
if has_step docs; then
  mkdir -p "$OUT/docs" "$WORK/docs"
  cp "$WORK/nuget.config" "$WORK/docs/nuget.config"
  if [[ "$DOCS" == "readme" ]]; then
    doc_files=(readme.md)
  else
    mapfile -t doc_files < <(git -C "$REPO" ls-tree -r --name-only "$DOCS_REF" -- readme.md documentation/user | grep '\.md$')
  fi
  index="$WORK/docs/index.tsv"
  : >"$index"
  for doc in "${doc_files[@]}"; do
    git -C "$REPO" show "$DOCS_REF:$doc" 2>/dev/null | awk -v doc="$doc" -v root="$WORK/docs" -v idx_file="$index" '
      /^```/ {
        if (!in_block) {
          in_block = 1; lang = substr($0, 4); gsub(/[ \t].*/, "", lang); body = ""; start = NR + 1; next
        }
        in_block = 0
        csharp = (lang == "csharp" || lang == "cs" || lang == "c#")
        # A fence that calls builder.Map(pattern, handler) without building the app (the pre-3.0
        # two-argument Map, nuru#296) is compiled after a "var builder = NuruApp.CreateBuilder();"
        # line, so the call meets a real receiver. Its using directives move above that line and
        # its column-0 type declarations below the statements (top-level statements come first).
        wrap = csharp && body !~ /CreateBuilder/ && body ~ /builder[[:space:]]*\.[[:space:]]*Map[[:space:]]*\([[:space:]]*"[^"]*"[[:space:]]*,/
        if (csharp && (body ~ /CreateBuilder/ || wrap)) {
          n = sprintf("%s#L%d", doc, start)
          slug = doc; gsub(/[^A-Za-z0-9]+/, "-", slug)
          name = sprintf("s-%s-%d", slug, start)
          dir = root "/" name
          system("mkdir -p \"" dir "\"")
          program = body
          if (wrap) {
            usings = ""; rest = ""; types = ""; in_type = 0
            m = split(body, lines, "\n")
            for (i = 1; i < m; i++) {
              if (!in_type && lines[i] ~ /^using [A-Za-z0-9_. =]+;/) { usings = usings lines[i] "\n"; continue }
              if (!in_type && lines[i] ~ /^((public|internal|sealed|static|abstract|partial|file|readonly) )*(record|class|struct|interface|enum) /) in_type = 1
              if (in_type) {
                types = types lines[i] "\n"
                if (lines[i] ~ /^(}|\);)/ || lines[i] ~ /^[^ ].*;[ \t]*$/) in_type = 0
              } else {
                rest = rest lines[i] "\n"
              }
            }
            program = usings "var builder = NuruApp.CreateBuilder();\n" rest types
          }
          printf "%s", program > (dir "/Program.cs"); close(dir "/Program.cs")
          printf "%s\t%s\t%s\n", name, n, (wrap ? "builder" : "program") >> idx_file; close(idx_file)
          last = name
        } else if ((lang == "bash" || lang == "sh" || lang == "shell") && last != "") {
          m = split(body, lines, "\n")
          for (i = 1; i < m; i++) {
            if (lines[i] ~ /^dotnet run -- / && lines[i + 1] ~ /^# Output: /) {
              a = lines[i]; sub(/^dotnet run -- /, "", a)
              o = lines[i + 1]; sub(/^# Output: /, "", o)
              printf "%s\t%s\n", a, o >> (root "/" last "/expect.tsv"); close(root "/" last "/expect.tsv")
            }
          }
          last = ""
        } else {
          last = ""
        }
        next
      }
      in_block { body = body $0 "\n" }
    '
  done
  snippet_count="$(wc -l <"$index")"
  for dir in "$WORK/docs"/s-*/; do
    name="$(basename "$dir")"
    # A snippet that names a sibling package (McpHost, Daemon) gets it, as its page tells the reader
    # to add it. File-based app directives (#!, #:package, #:project) do not compile in a project.
    extra=()
    for id in TimeWarp.Nuru.McpHost TimeWarp.Nuru.Daemon; do
      grep -q "$id" "$dir/Program.cs" && package_available "$id" && extra+=("$id")
    done
    sed -i '/^#[!:]/d' "$dir/Program.cs"
    consumer_csproj "$dir" "$name" "${extra[@]}"
    printf 'global using TimeWarp.Nuru;\n' >"$dir/usings.cs"
  done
  build_solution "$WORK/docs" "$OUT/docs/build.log"

  table="$OUT/docs/snippets.tsv"
  printf 'snippet\tsource\tcontext\tresult\tfirst_error\n' >"$table"
  broken=0; ok=0; fragment=0
  while IFS=$'\t' read -r name where context; do
    proj="$WORK/docs/$name/$name.csproj"
    errors="$(project_errors "$OUT/docs/build.log" "$proj")"
    # Broken: the build fails on a TimeWarp.Nuru API or on the package itself (stale docs, a stub
    # the generator rejects). Fragment: the snippet only fails on names it never defined
    # (handlers, services, user types), so it proves nothing either way.
    blame="$(nuru_errors "$errors")"
    if [[ -z "$errors" ]]; then
      result="compiles"; ok=$((ok + 1)); first=""
    elif [[ -n "$blame" ]]; then
      result="broken"; broken=$((broken + 1)); first="$(printf '%s' "$blame" | head -1)"
    else
      result="fragment"; fragment=$((fragment + 1)); first="$(printf '%s' "$errors" | head -1)"
    fi
    printf '%s\t%s\t%s\t%s\t%s\n' "$name" "$where" "$context" "$result" "$first" >>"$table"
    if [[ "$result" == "broken" ]]; then
      record docs "snippet ${where%%#L*}:${where##*#L} compiles against the package" FAIL docs/snippets.tsv "$first"
    fi
    if [[ "$result" == "compiles" && -s "$WORK/docs/$name/expect.tsv" ]]; then
      dll="$WORK/docs/$name/bin/Release/net10.0/$name.dll"
      while IFS=$'\t' read -r a o; do
        read -r -a argv <<<"$a"
        log="docs/$name.run.log"
        run_logged "$OUT/$log" env HOME="$WORK/home" timeout 120 dotnet "$dll" "${argv[@]}"
        if [[ $RUN_RC -eq 0 && "$RUN_OUT" == *"$o"* ]]; then
          record docs "${where%%#L*}:${where##*#L}: dotnet run -- $a prints '$o'" PASS "$log"
        else
          record docs "${where%%#L*}:${where##*#L}: dotnet run -- $a prints '$o'" FAIL "$log" "exit $RUN_RC"
        fi
      done <"$WORK/docs/$name/expect.tsv"
    fi
  done <"$index"
  if [[ $broken -eq 0 ]]; then
    record docs "doc snippets ($DOCS_REF) use only APIs the package has" PASS docs/snippets.tsv "$ok compile, $fragment fragments, $snippet_count total"
  else
    record docs "doc snippets ($DOCS_REF) use only APIs the package has" FAIL docs/snippets.tsv "$broken broken, $ok compile, $fragment fragments, $snippet_count total"
  fi
fi

# ---------------------------------------------------------------------------------------------
# Evidence summary
# ---------------------------------------------------------------------------------------------
pass=0; fail=0; docdefects=0
for r in "${RESULTS[@]}"; do
  case "$(cut -d'|' -f1,3 <<<"$r")" in
    docs\|*) [[ "$(cut -d'|' -f3 <<<"$r")" == "DOC-DEFECT" ]] && docdefects=$((docdefects + 1)) ;;
    *\|PASS) pass=$((pass + 1)) ;;
    *) fail=$((fail + 1)) ;;
  esac
done
{
  echo "# Nuru consumer check: TimeWarp.Nuru $VERSION"
  echo
  echo "The source (code and its Purpose/Design regions) is the authority. Docs are checked separately."
  echo
  echo "- Source: \`$SOURCE\` (private cache, \`<clear/>\` nuget.config)"
  echo "- Docs: \`$DOCS_REF\` ($DOCS)$([[ $DOCS_STRICT -eq 1 ]] && echo ", --docs-strict")"
  echo "- Run: $STAMP, dotnet $(dotnet --version)"
  echo "- Release verdict (launch, cache, probes, cli): **$([[ $fail -eq 0 ]] && echo PASS || echo FAIL)**: $pass passed, $fail failed"
  has_step docs && echo "- Docs out of date with source: **$docdefects** doc defects ($([[ $DOCS_STRICT -eq 1 ]] && echo "failing the run, --docs-strict" || echo "report only, not part of the verdict"))"
  echo
  echo "## Release verdict"
  echo
  echo "| Step | Check | Result | Evidence | Detail |"
  echo "|------|-------|--------|----------|--------|"
  for r in "${RESULTS[@]}"; do
    IFS='|' read -r s c st ev de <<<"$r"
    [[ "$s" == "docs" ]] && continue
    printf '| %s | %s | %s | `%s` | %s |\n' "$s" "${c//|/\\|}" "$st" "$ev" "${de//|/\\|}"
  done
  if has_step docs; then
    echo
    echo "## Docs out of date with source (DOC-DEFECT, file:line)"
    echo
    echo "A failing snippet is a doc defect unless the source public API says it should work. Then it is a code defect and the probe for that API shows it."
    echo
    echo "| Check | Result | Evidence | Detail |"
    echo "|-------|--------|----------|--------|"
    for r in "${RESULTS[@]}"; do
      IFS='|' read -r s c st ev de <<<"$r"
      [[ "$s" == "docs" ]] || continue
      printf '| %s | %s | `%s` | %s |\n' "${c//|/\\|}" "$st" "$ev" "${de//|/\\|}"
    done
  fi
} >"$OUT/summary.md"

echo
echo "summary: $OUT/summary.md ($pass passed, $fail failed, $docdefects doc defects)"
if [[ $KEEP -eq 0 ]]; then rm -rf "$WORK"; else echo "work kept: $WORK"; fi
exit "$FAILED"
