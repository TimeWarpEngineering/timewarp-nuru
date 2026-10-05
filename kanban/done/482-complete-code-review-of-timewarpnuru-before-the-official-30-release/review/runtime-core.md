# Runtime core (`source/timewarp-nuru`)

Baseline `5a06e900`. Reviewed builder lifecycle, REPL, completion, DI, type conversion, public hooks, and `documentation/developer/reference/error-handling.md`.

## R-1 — `EndpointBuilder.Build()` drops the open route

- Disposition: **fix-now**
- Evidence: `EndpointBuilder.Build()` is `ParentBuilder.Build()` (`builders/endpoint-builder.cs`). Its summary says it terminates the fluent chain with `Build()`. The generator registers a route only in `IrRouteBuilder.Done()` (`generators/ir-builders/ir-route-builder.cs`). `DispatchBuild` ignores any receiver that is not an `IrAppBuilder` (`generators/interpreter/dsl-interpreter.cs`). `.Map(...).WithHandler(...).Build()` never calls `Done()`, so the route is not registered and the app is not marked built. `RunAsync` then hits the stub that throws.
- Examples on `EndpointBuilder` correctly show `Done()` before a later `Build()`. The `Build()` method on the endpoint itself still looks legal.

## R-2 — Binding errors use stdout; the error-handling guide does not match the generator

- Disposition: **post-3.0** for a stream change. The guide itself is **fix-now** under the docs child (D-1).
- Evidence: Scalar conversion failure is `app.Terminal.WriteLine(...)` then `return 1` (`route-matcher-emitter.cs`, required-parameter arm). `route-matcher-emitter.cs` has no `WriteErrorLine`. `NuruApp.RunAsync` XML docs (`nuru-app.cs`) say handler return values are terminal output and `Environment.ExitCode` is the process exit code. `documentation/developer/reference/error-handling.md` describes a top-level `Console.Error` catch, `Error executing handler`, a thrown `Cannot convert...` exception, and a handler `return 1` as the exit code. CI is green against the generator's contract. Changing stdout to stderr is a behavior change, not a 3.0 blocker. The guide teaches an API the generator does not implement, so the guide is in the docs refresh.

## R-3 — Custom completion only binds the first parameter, and type lookup cannot see user types

- Disposition: **fix-now**
- Evidence: `EmitTryGetParameterInfoMethod` (`completion-emitter.cs`) sets `parameterName` only when `paramPos == 0`, and the prefix test is `StartsWith(commandPrefix)` with no token boundary, so `github` matches `git`. `DynamicCompletionHandler` then calls `Type.GetType(paramTypeName)` (`dynamic-completion-handler.cs`) from the Nuru assembly. The emitted name is the route constraint (`"int"`, `"LogLevel"`), not an assembly-qualified name, so `RegisterForType` does not resolve user types or C# keywords.

## R-4 — Shell completion never delegates to the filesystem

- Disposition: **fix-now**
- Evidence: `DynamicCompletionHandler.HandleCompletion` always sets `CompletionDirective.NoFileComp` and never reads `CompletionCandidate.Type`. `File` and `Directory` candidates therefore never run. Public `CompletionDirective` flags (`NoSpace`, `KeepOrder`, file fallback) are not applied. Zsh completion splits unquoted suggestion lines, so values that contain spaces break. Bash completion was already quoted.

## R-5 — Typed catch-all and repeated options use a partial throwing parser

- Disposition: **fix-now** (same child as A-3)
- Evidence: `EmitCatchAllTypeConversion` and `EmitRepeatedOptionTypeConversion` (`route-matcher-emitter.cs`) call `GetParseExpression` inside `catch (FormatException)`. That switch has `int`, `long`, `short`, `byte`, `double`, `float`, `decimal`, `bool`, `DateTime`, and `Guid` only. `GetBuiltInTryConversion` also accepts `sbyte`, `ushort`, `uint`, `ulong`, `char`, `TimeSpan`, `DateOnly`, `TimeOnly`, and `IPAddress`. For those, `GetParseExpression` returns the raw string, so the generated array assignment does not compile. For the types it does parse, overflow throws `OverflowException`, which is not `FormatException`, so the clear error is skipped.

## R-6 — `RunReplAsync` fallback tells the user to disable the source generator

- Disposition: **fix-now**
- Evidence: `nuru-app.cs` throws `"RunReplAsync was not intercepted. Ensure AddRepl() is called and the source generator is not enabled."` `RunAsync` in the same file says the generator must be enabled. The REPL sentence is inverted. The XML exception doc on the same method says the generator must be enabled.

## R-7 — Public examples called APIs that do not exist

- Disposition: **fixed on this task** for the public XML in `source/timewarp-nuru`. `source/timewarp-nuru-devcli/readme.md` still shows `CreateBuilder(args)` and stays on the docs child.
- What changed: `AddReplSupport()` examples are now `AddRepl()`. `NuruApp.CreateBuilder(args)` in builder, completion, and behavior XML is now `NuruApp.CreateBuilder()`. `CreateBuilder` takes no arguments (`nuru-app.cs`).

## R-8 — Obsolete builder members

- Disposition: **post-3.0**
- Decision: `NuruAppBuilder.Services` (getter always throws `InvalidOperationException`, `[Obsolete]` without `error: true`) and `AddReplOptions` (working alias of `AddRepl`) **survive into 3.0** as the migration shims. Remove them after 3.0. The migration guide names both.

## R-9 — `NuruAppHolder` and `ResponseDisplay` are public and unused

- Disposition: **fix-now**
- Evidence: `NuruAppHolder.SetApp` is `internal` and has no callers. `Build()` does not set it. `App` throws `"NuruApp has not been set. This is a framework bug"`. `ResponseDisplay.Write` (`io/response-display.cs`) is unreferenced and is marked `RequiresUnreferencedCode` / `RequiresDynamicCode`. The generator writes handler output itself. Both look like supported hooks. Do not delete them on this review task.

## Checked, not a 3.0 blocker

- REPL: `InitializeMultilineFromSingleLine` (`repl-console-reader.multiline.cs`) is unreferenced and duplicates `SyncToMultilineBuffer`. Kill-line helpers in `repl-console-reader.editing.cs` delegate to the kill ring. **post-3.0** dead-code cleanup. No second edit implementation found.
- DI: session services stay on the root cache; command services are per scope. `ReleaseSessionInstances` reflects the private `_disposables` field, which exists on `Microsoft.Extensions.DependencyInjection` 10.0.12. **post-3.0** fragility if that field moves. Not a functional break on the pinned package.
- `TypeConverterRegistry` is not on the generated execution path. Nullable and optional values are null-tested before `TryParse` in the generator.
- AOT publish of `tests/test-apps/timewarp-nuru-testapp-delegates` (`-c Release -r linux-x64 -p:PublishAot=true`) exited 0 with zero IL2026/IL3050 warnings. Four CS0436 warnings are the test app's generated `Mediator` beside Nuru's internal `Mediator` via `InternalsVisibleTo`. **wont-fix** for consumers. The test suite already `NoWarn`s CS0436; the test app does not.
