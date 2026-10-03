# Round 1 — general
**Date:** 2026-10-03
**Scope reviewed:** branch vs master (e965b383)

## Summary

The catalog is well guarded. The parity test in repl-47 compares every catalog chord with the live `GetBindings` dictionary in both directions, and checks method identity through `KeyBindingActionRegistry`. All 17 tests pass. The tool smoke commands behave as task.md describes (`--profile Mine` exits 1). Two behavior problems remain. First, when a JSON key-binding file is loaded, the REPL command silently prints the Default catalog, which contradicts the docs. Second, the standalone tool ignores a positional profile name and exits 0. There are also a few smaller consistency and maintainability points.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru/repl/repl-session.cs:243
- Description: The REPL works out the active profile name from `ReplOptions` alone: `(ReplOptions.KeyBindingProfile as IKeyBindingProfile)?.Name ?? ReplOptions.KeyBindingProfileName`. The live reader resolves its profile differently, through `KeyBindingConfigLoader.ResolveStartupProfile` (repl-console-reader.cs:95). When `KeyBindingProfile` is null and the name is `Default`, the reader loads `NURU_KEYBINDINGS`, `./.nuru/keybindings.json`, or `~/.nuru/keybindings.json` if one exists. That resolved profile is never written back to `ReplOptions`. So a user with a global JSON profile who types `key-bindings` gets "Profile: Default" and the Default catalog, with no error, and those bindings are not the ones in effect. This contradicts documentation/user/features/repl-key-bindings.md:290, which says a JSON profile "prints an error and does not expand those chords". No test covers the JSON-loaded path.
- Suggestion: Keep the profile the reader actually resolved (expose it from `ReplConsoleReader`, or resolve it once in the session and pass it in), and use its `Name`. A JSON-loaded `CustomKeyBindingProfile` is named `Custom`, so it then takes the documented error path. Alternatively, fix the docs to describe the current behavior. Add a test that sets `NURU_KEYBINDINGS` (or an injected search) and asserts the outcome.
- Status: open

### Issue 2 — Severity: bug
- File: tools/nuru-key-bindings/nuru-key-bindings.cs:8
- Description: The REPL command accepts a positional profile (`key-bindings Emacs`, documented and tested). The standalone tool does not. `dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- key-bindings Emacs` printed "Profile: Default" and exited 0, so the extra token was silently ignored (verified). A bare `nuru-key-bindings.cs -- Emacs` is not forwarded either, because it does not start with `-`. Users who follow the REPL docs get the wrong profile from the tool and no error.
- Suggestion: Either add an optional positional parameter to `KeyBindingsQuery` (for example `[NuruRoute("key-bindings {profile?}")]`, or the attribute equivalent) and fold it into `Profile`, or reject unexpected tokens. Also forward a leading known profile name in the entry point, and add a tool-level test or smoke check.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-nuru/repl/repl-session.cs:243
- Description: Every other REPL built-in matches an exact token list (`["history"]`, `["clear-history"]`, and so on), so `history 10` still reaches app routes. `key-bindings` matches on prefix (`args[0] == "key-bindings"`) and swallows every invocation. Any existing app that defines a `key-bindings ...` route (the new tool itself declares `[NuruRoute("key-bindings")]`) is now silently shadowed in REPL mode. This change affects behavior for every app that uses the REPL. It is not listed in the changelog.
- Suggestion: Note in the changelog that `key-bindings` is now a reserved REPL word. Alternatively, skip the built-in when the app's route provider already knows the command (`RouteProvider.IsKnownCommand("key-bindings")`).
- Status: open

### Issue 4 — Severity: suggestion
- File: source/timewarp-nuru/repl/key-bindings/key-binding-catalog.cs:132
- Description: `List` calls `EnsureTable()` on every call. That rebuilds a HashSet over all of roughly 300 assignments and checks the registry each time. Because the check runs at user-call time, catalog drift would surface to end users as an `InvalidOperationException` (which neither the REPL nor the tool catches; both catch only `ArgumentException`) rather than as a failing test.
- Suggestion: Run the validation once, in the static initializer or a `Lazy`. Better, move it into repl-47, which already does stronger parity checks, and keep runtime listing free of validation throws.
- Status: open

### Issue 5 — Severity: nit
- File: tools/nuru-key-bindings/endpoints/key-bindings-query.cs:74
- Description: For an unknown profile, the tool returns the error text as the query result, so it goes to stdout. Only the exit code signals failure (verified: stderr was empty for `--profile Mine`). The REPL command writes the same error to stderr (`WriteErrorLine`). Scripts that pipe stdout will capture the error message as if it were listing output.
- Suggestion: Write the message through `ITerminal.WriteErrorLine` (inject `ITerminal`) and return an empty result, or throw so the framework's error path handles it.
- Status: open

### Issue 6 — Severity: nit
- File: source/timewarp-nuru/repl/repl-commands.cs:182
- Description: `TryTakeValue` treats any value that starts with `-` as a missing value. `key-bindings --key -` (looking for the `Ctrl+-` Undo chord by substring) therefore reports "Missing value for --key." `--key Ctrl+-` works, so the impact is small.
- Suggestion: Only reject values that match a known flag (`--profile`, `-p`, `--key`, `-k`, `--function`, `-f`, `--detailed`, `-d`), or accept any following token.
- Status: open
