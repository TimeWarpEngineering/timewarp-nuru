# Error Handling in TimeWarp.Nuru

Documentation of the error handling behavior of the generated code in TimeWarp.Nuru 3.0.

`NuruApp.RunAsync(args)` is replaced at compile time by a source-generated interceptor. All behavior below
describes what that generated code does.

## Error Handling Architecture

```mermaid
graph TD
    A[User Input] --> B[Generated Route Matching]
    B --> C{Route Matched?}
    C -->|No| D["'Unknown command. Use --help for usage.' to stderr, return 1"]
    C -->|Yes| E[Parameter and Option Conversion]
    E --> F{Conversion Succeeded?}
    F -->|No| G["'Error: Invalid value ...' to stderr, return 1"]
    F -->|Yes| H[Handler Execution]
    H --> I{Exception?}
    I -->|Yes| J["CLI RunAsync: message to stderr, return 1. ExecuteAsync: propagates"]
    I -->|No| K[Result written to terminal; return Environment.ExitCode]
```

## Key Error Handling Mechanisms

### 1. **CLI RunAsync catches handler exceptions**

A CLI `RunAsync` writes the exception message to stderr and returns 1. It does not print the stack trace.
`OptionsValidationException` still propagates. `ExecuteAsync` and `InvokeAsync` do not catch, so a daemon
or MCP host still sees the exception. When telemetry is enabled, the command span records `error.type`
and rethrows into that CLI boundary.

There is no framework-written `Error executing handler` prefix. To choose a different exit code, catch
inside the handler. The example below still works for an `IOException` you want to phrase yourself:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("process {file}")
    .WithHandler((string file, ITerminal terminal) =>
    {
      try
      {
        ProcessFile(file);
        terminal.WriteLine($"Processed {file}");
      }
      catch (IOException ex)
      {
        terminal.WriteErrorLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
      }
    })
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);
```

### 2. **Exit Codes and Handler Return Values**

Per the `NuruApp.RunAsync` documentation, handler return values are written to the terminal as output;
they do **not** control the exit code. For example, `.WithHandler(() => 42)` prints `42` and the process
still exits with `0`. The generated code ends each matched route with `return NuruCallContext.CurrentExitCode;`
(`Environment.ExitCode` outside a daemon or MCP host call, the call's own exit code inside one), so
to signal failure from a handler, set `NuruCallContext.CurrentExitCode` (a plain CLI may also set
`Environment.ExitCode`; a served call ignores it, since calls there run concurrently):

```csharp
.Map("check")
  .WithHandler(() =>
  {
    NuruCallContext.CurrentExitCode = 1;
  })
  .AsCommand()
  .Done()
```

Generated binding and no-match errors (below) return `1` directly.

**Key principles:**
- Handler output is written through the terminal (`app.Terminal`), not via a returned exit code.
- A non-zero exit code from a handler requires setting `NuruCallContext.CurrentExitCode` (or `Environment.ExitCode` in a plain CLI).
- Exit codes: `0` = success, `1` = a binding failure, no match, or a handler exception in a CLI `RunAsync`, `130` = SIGINT after `ProcessTerminationTimeout`, `143` = SIGTERM after that timeout.

### 3. **Route Parsing Errors**
Route patterns are parsed by the source generator at compile time, so malformed patterns are reported as
build diagnostics rather than runtime errors. Examples include unsupported `{param:type}` constraints,
duplicate parameter names, unbalanced braces and invalid parameter syntax.

### 4. **Parameter and Option Binding Errors**
After a route matches, the generated code converts string arguments to the declared parameter types.
It uses `TryParse()`-style conversion for built-in types (`int`, `long`, `double`, `decimal`, `bool`,
`DateTime`, `Guid`, `TimeSpan`, and so on), `Uri.TryCreate` for `Uri`, constructor calls with a
`try`/`catch` for `FileInfo` and `DirectoryInfo`, and the registered `TryConvert()` for enums and custom
converters.

On conversion failure the generated code:
- writes one line to `app.Terminal.WriteLine(...)` (the terminal's **standard output**, not
  standard error), and
- returns exit code `1` immediately.

It does **not** throw an exception and does **not** try the next route.

Messages emitted by the generator:

| Case | Message |
|------|---------|
| Scalar parameter (built-in type) | `Error: Invalid value 'abc' for parameter 'port'. Expected: int` |
| Typed catch-all parameter | `Error: Invalid value in 'args'. Expected: int[]` |
| Repeated or typed option | `Error: Invalid value in option '--ids'. Expected: int` |
| Scalar option | `Error: Invalid value 'abc' for option '--port'. Expected: int` |
| Enum parameter or option | `Error: Invalid value 'x' for parameter 'env'. <valid values message>` |
| Custom converter parameter | `Error: Invalid <Type> value for parameter 'name': 'x'` |
| Option missing a `FileInfo`/`DirectoryInfo` value | `Error: Missing value for option '--file'. Expected: FileInfo` |

**Design Decision:** Type conversion failures are binding errors, not matching failures. When a user types
`server --port abc` and a route exists for `server --port {num:int}`, the route **matched** but the
**binding failed**. The user gets a clear error, not a confusing "unknown command" message from fallback
behavior.

Binding errors go to the terminal's standard error.

### 5. **Command Matching Errors**
When no route matches the input, the generated fallback writes
`Unknown command. Use --help for usage.` to the terminal's **standard error**
(`WriteErrorLineAsync`) and returns exit code `1`.

### 6. **Output Stream Usage**
- **Handler results**: written to `app.Terminal` (standard output). `void`/`Task`/`Unit` results produce
  no output; primitives are written as text, dates as ISO 8601, and complex types as JSON.
- **Binding errors**: `app.Terminal.WriteErrorLine` (standard error). With `UseProblemRendererForErrors()` and a problem renderer, the rendered problem goes to standard error too.
- **No-match error**: `app.Terminal.WriteErrorLineAsync` (standard error).
- Handlers that need to write errors to standard error should use
  `ITerminal.WriteErrorLine` / `WriteErrorLineAsync`.

## Implementation Details

- Matched routes return `Environment.ExitCode` (default `0`); binding and no-match failures return `1`.
- A CLI `RunAsync` catches a handler exception, writes its message to stderr, and returns `1`. `OptionsValidationException` still propagates. `ExecuteAsync` does not catch. Ctrl+C cancels the handler token and returns `130` if the handler is still running after `ProcessTerminationTimeout`; SIGTERM returns `143`.
- Binding errors name the parameter or option and the offending value.
