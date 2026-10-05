# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit `eb8d9860` (`NuruApp.RunReplAsync` stub and XML, `repl-48-run-repl-async-stub-message.cs`)

## Summary

`RunReplAsync` now throws `RunReplAsync was not intercepted. Ensure AddRepl() is called and the source generator is enabled.` The `<exception>` doc gives the same guidance. `RunAsync` in the same file already required the generator to be enabled, and no other product string still says the generator is not enabled.

The new runfile reaches the stub through a method-group conversion. `ExtractRunReplAsyncWithDiagnostics` only accepts an `InvocationExpressionSyntax`, so that conversion is not an intercept site and the fallback body runs. The file is under the CI `timewarp-nuru-tests/**/*.cs` glob and is not in `CiTestExcludes`. `dotnet run tests/timewarp-nuru-tests/repl/repl-48-run-repl-async-stub-message.cs` passed 1/1. The assertion is the exact new message.

No defects found.

## Issues

None.
