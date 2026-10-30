# Round 1 — general
**Date:** 2026-10-31
**Scope reviewed:** branch `task/561-usetelemetry-exports-extra-activitysources-an-app` vs merge-base `31382e6c` (product commit `e11b441b`: options, extractor, model, emitter, generator-43, telemetry-06, telemetry docs).

## Summary

`NuruTelemetryOptions` gains get-only `ActivitySources` and `Meters` lists. `TelemetryOptionsExtractor` reads each `Add` whose argument is a compile-time constant string (literal or `const`) and stores the names on `TelemetryModel`. `TelemetryEmitter` emits one `AddSource` or `AddMeter` literal per name after Nuru's own calls, and only inside the tracing or metrics block. With no names, those loops emit nothing, so the generated setup matches the previous output.

The goal restatement matches the task on the API, the AOT literal rule, unchanged output when nothing is configured, and Taratibu in the telemetry docs. The restatement's test mechanism differs from the task's "in-memory exporter" wording; that mismatch is recorded under `## Learnings`. The generated providers only attach the OTLP exporter, and telemetry-06 proves export with the same in-process HTTP collector pattern as telemetry-04, including the meter the requirements asked for. OpenTelemetry 1.x treats `*` and `?` as wildcards, so the docs' `"MyCompany.*"` example is a name the SDK already matches; the generator passes it through as a literal.

Expression-bodied `o => o.ActivitySources.Add("TimeWarp.Taratibu")` is the form the task shows. The extractor applies an invocation body through the same `Apply` path the block form uses, and generator-43 already covers a block `Add` of a literal and a `const`, dedup, the unconfigured call list, and skipping sources when tracing is off. Empty names are dropped. Repeats are dropped with ordinal comparison. Names are escaped with `EmitterStringUtils.EscapeForStringLiteral` before they are embedded.

This session could not re-run the runfiles. `dotnet run` in the sandbox cannot write the runfile cache under `~/.local/share/dotnet/runfile`, and unrestricted execution was not granted. The verdict is from the diff and the call sites (`DispatchUseTelemetry`, `EmitTelemetrySetup`, the CI include globs). generator-43 is on the standalone list. telemetry-06 is not in `CiTestExcludes`, disables logging, and checks traces and metrics, which the multi-mode assembly already exports.

## Issues
