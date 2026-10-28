# Round 1 — general
**Date:** 2026-10-28
**Scope reviewed:** branch diff vs merge-base `bd204a66` for task 552 (telemetry registration, command-class emission, telemetry-02/04/05, user telemetry docs).

## Summary

`UseTelemetry()` now registers `global::TimeWarp.Nuru.TelemetryBehavior` once, as the first behavior, unless the app already added that type or both tracing and metrics are off. The dead emitter span helpers are gone. The span shape (route pattern name, `command.name` / `command.type`, Ok, or Error with `error.type` and no message) matches `TelemetryBehavior` and the goal restatement. An app that also calls `AddBehavior(typeof(TelemetryBehavior))` keeps a single registration. Parentage is `Activity.Current`. The goal restatement matches the task: choosing the behavior as the one mechanism, and keeping 470-015 redaction, is the decision the task asked for.

The new registration sends every fluent route through `BehaviorEmitter.EmitCommandCreation`. For a short-only flag that initializer does not match the generated command class or the locals the route matcher already emitted, so `UseTelemetry()` fails to compile a documented route shape such as `build -v` or `docker run -i -t`. Parameterless routes and short options that carry a `{parameter}` still line up.

`ganda repo audit` reported one failure, `self-hosted-runner`, because `gh repo view` could not tell whether the repo is private in this sandbox. That is not a product defect. `dotnet run` of telemetry-05 could not start: the sandbox denied the runfile cache under `~/.local/share/dotnet/runfile`. The short-flag failure was checked by compiling a small `UseTelemetry()` program against `source/timewarp-nuru-analyzers/bin/Debug/net10.0/TimeWarp.Nuru.Analyzers.dll` (built after the emitter edit) and reading the generated initializer.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs:213
- Description: `UseTelemetry()` now prepends `TelemetryBehavior`, so every fluent route (lambda or method group) runs `EmitCommandCreation`. For a short-only flag the command class property comes from `CommandClassEmitter` (`LongForm ?? ParameterName ?? ShortForm`, so `-v` is `V` and `-i`/`-t` are `I`/`T`). The route matcher local is `ToCamelCase(LongForm ?? ShortForm)` (`v`, `i`, `t`) in `route-matcher-emitter.cs`. The behavior initializer ignores `ShortForm` and emits `Option = flag` for each such flag. Generated output for `.UseTelemetry().Map("build -v").WithHandler((bool v) => ...)` is a class with `public bool V` and an initializer `Option = flag`. `.Map("docker run -i -t")` initializes `Option` twice. Either program fails to compile (`Option` is not a member, `flag` does not exist, and two flags also duplicate the initializer). Before this change those routes compiled, because a `UseTelemetry()` app with no `AddBehavior` never built a command instance. `commit -m {message}` still generates `Message = message`, because that option has a parameter name. The telemetry-05 routes are parameterless, so they do not catch this.
- Suggestion: In `EmitCommandCreation`, name the property the way `CommandClassEmitter` does (`LongForm ?? ParameterName ?? ShortForm`) and name the local the way `RouteMatcherEmitter` does for flags (`LongForm ?? ShortForm`). Add a `UseTelemetry()` test that compiles and runs a short-only flag route (`build -v`, and `docker run -i -t`) and still sees one command span.
- Status: open
