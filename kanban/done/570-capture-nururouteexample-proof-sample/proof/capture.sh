#!/usr/bin/env bash
# Regenerates every file under capture/ by running calc.cs (TimeWarp.Nuru 4.0.0-beta.16).
# Each *.ansi.txt is the raw stdout+stderr bytes. Each *.txt is the same output with ANSI
# escape sequences removed by sed (the published package does not honor NO_COLOR).
# capture/json-args.txt records every --json-args command, its output, exit code, and verdict.
set -u
cd "$(dirname "$0")"
mkdir -p capture
dotnet build calc.cs > /dev/null

strip_ansi() { sed -E 's/\x1b\[[0-9;]*m//g'; }

capture() {
  local name="$1"; shift
  "$@" > "capture/$name.ansi.txt" 2>&1
  strip_ansi < "capture/$name.ansi.txt" > "capture/$name.txt"
}

capture help-add ./calc.cs add --help
capture help-root ./calc.cs --help
./calc.cs --capabilities > capture/capabilities.json 2>&1

log=capture/json-args.txt
: > "$log"

# check <label> <expected-exit> <expected-stdout or -> <shell command>
check() {
  local label="$1" want_exit="$2" want_out="$3" cmd="$4" out code verdict
  out=$(bash -c "$cmd" 2>&1)
  code=$?
  verdict=PASS
  [ "$code" = "$want_exit" ] || verdict=FAIL
  if [ "$want_out" != "-" ] && [ "$out" != "$want_out" ]; then verdict=FAIL; fi
  {
    echo "## $label"
    echo "\$ $cmd"
    echo "$out" | strip_ansi
    echo "exit=$code expected_exit=$want_exit expected_stdout=$want_out -> $verdict"
    echo
  } >> "$log"
}

check "baseline argv" 0 5 "./calc.cs add 2 3"
check "inline object" 0 5 "./calc.cs add --json-args '{\"x\":2,\"y\":3}'"
check "stdin '-' from a pipe" 0 5 "printf '{\"x\":2,\"y\":3}' | ./calc.cs add --json-args -"
check "stdin '-' from a heredoc" 0 5 "./calc.cs add --json-args - <<'JSON'
{\"x\": 2, \"y\": 3}
JSON"
check "@path file" 0 5 "./calc.cs add --json-args @add-2-3.json"
check "argv overrides JSON for the same name" 0 13 "./calc.cs add 10 --json-args '{\"x\":2,\"y\":3}'"
check "unknown key is an error" 1 - "./calc.cs add --json-args '{\"x\":2,\"y\":3,\"z\":4}'"
check "attached form is rejected" 1 - "./calc.cs add '--json-args={\"x\":2,\"y\":3}'"

grep -c -- '-> PASS' "$log" | xargs -I{} echo "{} PASS"
grep -c -- '-> FAIL' "$log" | xargs -I{} echo "{} FAIL"
