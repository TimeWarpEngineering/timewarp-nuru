# Migrating from System.CommandLine to TimeWarp.Nuru

This guide is for people who have a CLI built on `System.CommandLine` (the 2.0 release or any 2.0 beta) and
want to move it to TimeWarp.Nuru. It covers three topics:

- **Native AOT and trimming.** Nuru's source generator writes the parsing, binding, help, and DI code at
  build time, so a Nuru app publishes with `PublishAot` and no trim or AOT warnings.
- **API churn.** `System.CommandLine` renamed or removed most of its surface between beta4 and 2.0
  (`SetHandler`, `InvocationContext`, `CommandLineBuilder`, `IConsole`, the binder packages). If you are still
  on a beta, you can port straight to Nuru and skip the 2.0 migration.
- **Behavior differences.** Some `System.CommandLine` behaviors work differently in Nuru. They are listed
  in [Behavior differences to check](#behavior-differences-to-check). Read that section before you ship a
  port.

The agent version of this guide is the
[`tw-nuru-migrate-system-commandline` skill](../../../skills/tw-nuru-migrate-system-commandline/SKILL.md).
It has the same mapping as a step-by-step recipe. This page is the full explanation.

If you are upgrading an existing Nuru 2.x app, read [Migrating to 3.0](migrating-to-3.0.md) instead.

## Before you start

| Requirement | Why |
|-------------|-----|
| `<TargetFramework>net11.0</TargetFramework>` | The `TimeWarp.Nuru` 4.0 package ships `lib/net11.0` only. A tool that targets `net8.0`, `net9.0`, or `net10.0` must retarget, and its users need the .NET 11 runtime (or an AOT binary). |
| .NET 11 SDK | Required to build `net11.0`. Bump `sdk.version` in `global.json` if the repo pins one. |
| `<PackageReference Include="TimeWarp.Nuru" Version="4.0.0-beta.*" />` | Brings `TimeWarp.Mediator`, `TimeWarp.Terminal`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Logging.Console`, and the analyzers and generators. |
| Remove every `System.CommandLine*` package | `System.CommandLine`, `System.CommandLine.NamingConventionBinder`, `System.CommandLine.Hosting`, `System.CommandLine.DragonFruit`, and `System.CommandLine.Rendering`. |

The examples use the **Endpoint DSL**: one class per command with `[NuruRoute]`, `[Parameter]`, and `[Option]`.
That is the closest match to a `Command` with its options and arguments. The Fluent DSL (`Map("greet
{name}").WithHandler(...)`) also works and is fine for a one-file tool. See
[Architecture Choices](architecture-choices.md).

## Concept map

| System.CommandLine 2.0 | TimeWarp.Nuru |
|------------------------|---------------|
| `new RootCommand("desc")` | `NuruApp.CreateBuilder().WithDescription("desc")`. The root action is a `[NuruRoute("")]` endpoint (the default route). |
| `new Command("deploy", "desc")` | `[NuruRoute("deploy", Description = "desc")]` on a `sealed` class that implements `ICommand<T>`, `IQuery<T>`, or `IIdempotentCommand<T>`. |
| Nested `Command` (`root.Subcommands.Add(db); db.Subcommands.Add(migrate)`) | `[NuruRouteGroup("db")]` on an abstract base class. The endpoint `[NuruRoute("migrate")]` inherits from it. Groups nest through inheritance. |
| `command.Aliases.Add("rm")` | `[NuruRouteAlias("rm")]` |
| `new Argument<string>("name")` | `[Parameter] public string Name { get; set; } = "";` (required) |
| `Argument<string?>` with `Arity = ZeroOrOne` | `[Parameter] public string? Name { get; set; }` (nullable means optional) |
| Optional argument with a default (`DefaultValueFactory = _ => "2026"`) | Nullable property; apply the default in the handler (`command.Version ?? "2026"`). A non-nullable property is required. |
| Optional argument *before* a required one (`[method] url`) | Not expressible as two parameters. Use one catch-all and classify the tokens in the handler. |
| `Argument<string[]>` (`ZeroOrMore`, `OneOrMore`) | `[Parameter(IsCatchAll = true)] public string[] Items { get; set; } = [];` (must be last) |
| Several arguments | One `[Parameter]` property per argument, declared in command-line order. Position is property declaration order. On 4.0.0-beta.14 the generator does not read `Order`, and analyzer `NURU_A002` is not reported, so leave `Order` out and order the properties instead. |
| `new Option<string>("--output", "-o")` | `[Option("output", "o")] public string? Output { get; set; }` (no dashes in the attribute). The short name may be several characters: `[Option("connectionstring", "cs")]` matches `-cs`. |
| Extra option aliases (`new Option<bool>("--offline", "--dry-run", "-n")`) | `[Option]` takes one long and one short name. Add one property per extra long name and combine them in the handler. |
| `new Option<bool>("--force")` | `[Option("force")] public bool Force { get; set; }` (a flag: presence binds `true`, absence binds `false`) |
| `Option<bool>` with `DefaultValueFactory = _ => true` | Not expressible as a `bool`: an initializer `= true` is ignored and absence binds `false`. Invert the flag (`--no-create`), or check the raw arguments. `--create false` is not supported; `false` becomes a positional argument. |
| `DefaultValueFactory = _ => 3` (beta4: `getDefaultValue: () => 3`) | Property initializer: `public int Count { get; set; } = 3;` (value options; not `bool` flags) |
| `Required = true` (beta4: `IsRequired = true`) | No attribute. Check the value in the handler and set `Environment.ExitCode`. See [Required options](#required-options). |
| `Option<string[]>` with `AllowMultipleArgumentsPerToken` | `[Option("tag", "t")] public string[] Tags { get; set; } = [];` The array (or collection) property type is what makes the option repeat; the `IsRepeated` flag is not read on 4.0.0-beta.14 and does nothing on a `string`. Repeat the flag for each value: `-t a -t b`. |
| `Option<MyEnum>` | `[Option("format")] public MyEnum Format { get; set; }` (case-insensitive) |
| `AcceptOnlyFromAmong("a", "b")` (beta4: `FromAmong`) | An `enum`, or a check in the handler |
| `Validators.Add(...)` (beta4: `AddValidator`) | A check in the handler, or a [pipeline behavior](../features/pipeline-behaviors.md) |
| `Recursive = true` (beta4: `AddGlobalOption`) | `[GroupOption("verbose", "v")]` on the group base class. Every route in the group inherits it. For options shared by top-level commands, use `[NuruRouteGroup("")]` (no prefix). |
| Options shared by sibling commands (a common base, or a helper that adds them) | `[GroupOption]` on a `[NuruRouteGroup]` base. A plain `[Option]` on a base class without `[NuruRouteGroup]` is **ignored** with no warning. |
| `command.SetAction(parseResult => ...)` | A nested `sealed class Handler : IRequestHandler<TEndpoint, TResult>`. Bound values arrive as the endpoint's properties. |
| `parseResult.GetValue(option)` | Read the property: `command.Output` |
| `SetAction` returning `int` | Set `Environment.ExitCode`. A returned `int` is **printed**, not used as the exit code. |
| `CancellationToken` in an async action | The `CancellationToken` parameter of `Handle`. In a CLI run, Ctrl+C ends the process; it does not cancel this token. |
| `parseResult.Invoke()` / `InvokeAsync()` | `await app.RunAsync(args)` (returns the exit code) |
| `--help`, `-h`, `-?` | `--help` and `-h` are built in. `-?` is not. |
| `--version` | Built in. It prints `AssemblyInformationalVersion`. |
| `[suggest]` and `dotnet-suggest` completion | `.EnableCompletion()`. See [Shell completion](../features/shell-completion.md). |
| `[diagram]` directive | No equivalent. Use `<command> --help` or `--capabilities`. |
| `TreatUnmatchedTokensAsErrors = false` / `parseResult.UnmatchedTokens` | A catch-all parameter. See [Free-form trailing arguments](#free-form-trailing-arguments). |
| `System.CommandLine.Hosting` `UseHost` | `ConfigureServices(...)` on the builder. Configuration and logging are built in. See [Configuration and verbosity](#configuration-and-verbosity). |
| `IConsole` / `parseResult.Configuration.Output` | `ITerminal` (inject it; `using TimeWarp.Terminal;`) or `Console`. Test with `TestTerminal`. |
| `System.CommandLine.Rendering` | `TimeWarp.Terminal` colors and widgets (tables, panels, rules), or keep the output library you already use (Spectre.Console works) |

## A worked example

A `System.CommandLine` 2.0 tool with one command, one argument, and three options:

```csharp
using System.CommandLine;

Argument<DirectoryInfo?> pathArgument = new("path")
{
  Description = "Folder to scan",
  Arity = ArgumentArity.ZeroOrOne
};

Option<FileInfo?> outputOption = new("--output", "-o") { Description = "Output file" };
Option<int> depthOption = new("--depth") { Description = "Scan depth", DefaultValueFactory = _ => 3 };
Option<bool> verboseOption = new("--verbose", "-v") { Description = "Verbose output" };

RootCommand root = new("Scans a folder");
root.Arguments.Add(pathArgument);
root.Options.Add(outputOption);
root.Options.Add(depthOption);
root.Options.Add(verboseOption);

root.SetAction(parseResult =>
{
  DirectoryInfo path = parseResult.GetValue(pathArgument) ?? new DirectoryInfo(".");
  FileInfo? output = parseResult.GetValue(outputOption);
  int depth = parseResult.GetValue(depthOption);
  bool verbose = parseResult.GetValue(verboseOption);
  return Scanner.Run(path, output, depth, verbose);
});

return root.Parse(args).Invoke();
```

The same tool in Nuru:

```csharp
// Program.cs
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .WithDescription("Scans a folder")
  .Map<ScanCommand>()
  .Build();

return await app.RunAsync(args);
```

```csharp
// scan-command.cs
using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("", Description = "Scan a folder")]
public sealed class ScanCommand : ICommand<Unit>
{
  [Parameter(Description = "Folder to scan")]
  public string? Path { get; set; }

  [Option("output", "o", Description = "Output file")]
  public string? Output { get; set; }

  [Option("depth", Description = "Scan depth")]
  public int Depth { get; set; } = 3;

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : IRequestHandler<ScanCommand, Unit>
  {
    public ValueTask<Unit> Handle(ScanCommand command, CancellationToken cancellationToken)
    {
      DirectoryInfo path = new(command.Path ?? ".");
      FileInfo? output = command.Output is null ? null : new FileInfo(command.Output);
      Environment.ExitCode = Scanner.Run(path, output, command.Depth, command.Verbose);
      return Unit.ValueTask;
    }
  }
}
```

Notes:

- `[NuruRoute("")]` is the default route, the equivalent of the root command's action. A tool with
  subcommands gives each one its own `[NuruRoute("name")]` class.
- `.Map<ScanCommand>()` registers one endpoint. `.DiscoverEndpoints()` registers every `[NuruRoute]` class in
  the compilation. Either works in an app. Use `Map<T>()` in a test assembly that builds several apps
  (analyzer `NURU_A003`).
- An endpoint property of type `FileInfo`, `DirectoryInfo`, or `IPAddress` does not bind on 4.0.0-beta.14.
  Bind a `string` and build the object in the handler, or register a type converter (see
  [Supported Types](../reference/supported-types.md)). Endpoint properties bind `string`, the numeric types,
  `bool`, `char`, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `BigInteger`,
  `Uri`, `Version`, and enums. Fluent route constraints such as `{file:FileInfo}` convert the full
  [built-in type](../../../glossary.md) set, which includes `FileInfo` and `DirectoryInfo`.
- The exit code comes from `Environment.ExitCode`, not from the handler's return value. A property named
  `Environment` shadows the type; write `System.Environment.ExitCode` then.
- Endpoint classes can be `public sealed` or `internal sealed`. Use `internal` when the handler takes an
  `internal` service type (otherwise `CS0051`).
- `ConfigureServices(services => services.AddSingleton<IMyService, MyService>())` needs
  `using Microsoft.Extensions.DependencyInjection;`.
- Keep the builder a single literal chain in one method. A ternary or a helper that returns a partly built
  builder fails with `NURU_S999`.
- Builder values are read at compile time. `.WithDescription(...)` needs a plain string literal (or a raw
  string literal). An interpolated string compiles but prints an empty description.
- Do not name an endpoint property `Type`; the generated code does not compile. Give enum option types a
  simple name that is unique in the compilation, or the generated code is ambiguous (`CS0104`).

## Arguments and options

### Required options

Nuru has no required-option attribute. A non-nullable option with no value on the command line binds its
property initializer (or `default`). For a required option, make it nullable and check it in the handler:

```csharp
[Option("connection", "c", Description = "Connection string (required)")]
public string? Connection { get; set; }

public sealed class Handler(ITerminal terminal) : IRequestHandler<MigrateCommand, Unit>
{
  public ValueTask<Unit> Handle(MigrateCommand command, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(command.Connection))
    {
      terminal.WriteErrorLine("Option '--connection' is required.");
      Environment.ExitCode = 1;
      return Unit.ValueTask;
    }

    // ...
    return Unit.ValueTask;
  }
}
```

A required positional parameter does not need this check. A non-nullable `[Parameter]` must be on the
command line, or the route does not match.

### Option syntax

| Command line | Nuru 4.0 betas |
|--------------|----------------|
| `--output x.txt`, `-o x.txt` | Supported |
| `--tag a --tag b` (repeated option) | Supported when the property is an array or collection (`string[]`) |
| `--tag a b` (several values per token) | Only `a` binds. `b` is treated as a positional argument: it fills an optional `[Parameter]` if the route has one, otherwise the call fails with `Unknown command`. Repeat the flag. |
| `--force true`, `--force false` (explicit bool value) | Not supported. The flag binds `true` and the value is treated as a positional argument. |
| `--output=x.txt`, `--output:x.txt` | Supported for a declared long option. The value is everything after the first `=` or `:` (`--connection=Server=h:5432` binds `Server=h:5432`). |
| `--verbose=true` (bool flag with a value) | Error: `Unknown option '--verbose=true'. Use --help for usage.`, exit 1. |
| `-vo x` (bundled short options) | Not supported. Write `-v -o x`. |
| `--OUTPUT` | Not matched. Option names are case-sensitive. Enum values are not. |

Before `--`, a token shaped like `--name=value`, `--name:value`, `/name=value`, or `/name:value` whose name is
not a long option declared by any route is a configuration override (`--Database:Host=prod`). Nuru removes it
from the arguments before route matching, so `Microsoft.Extensions.Configuration` can read it. Name matching
is ordinal (case-sensitive). When the name is a declared long option, the token binds instead: `--format=json`
and `--format:json` both bind `--format` to `json`. (Before the release after 4.0.0-beta.14, `--name=value`
was always taken as configuration and never bound; fixed in task 591-001.) Short options with `=` (`-o=x`)
are not filtered or bound.

If your users and scripts depend on several long aliases per option, or `--flag false`, rewrite the
arguments in `Program.cs` before `RunAsync`. An alias that is not a declared option is read as configuration
when written `--alias=value`, so map it to the canonical name first:

```csharp
// Program.cs: --dt sqlite -> --databasetype sqlite, --dt=sqlite -> --databasetype sqlite
Dictionary<string, string> canonical = new(StringComparer.Ordinal)
{
  ["--dt"] = "--databasetype",
};

List<string> normalized = [];
for (int i = 0; i < args.Length; i++)
{
  string arg = args[i];
  if (arg == "--")
  {
    // Forwarded arguments: copy "--" and everything after it unchanged.
    normalized.AddRange(args[i..]);
    break;
  }

  int equals = arg.IndexOf('=', StringComparison.Ordinal);
  string name = equals > 0 ? arg[..equals] : arg;
  if (canonical.TryGetValue(name, out string? longName))
  {
    normalized.Add(longName);
    if (equals > 0)
      normalized.Add(arg[(equals + 1)..]);
  }
  else
  {
    normalized.Add(arg);
  }
}

args = [.. normalized];
```

The loop stops at `--` and copies the separator and the tail unchanged, so a forwarded `-- --dt=xml` is
not rewritten. Nuru does not touch tokens after `--` either (see [`--` passthrough](#---passthrough)).

## Subcommands and global options

```csharp
[NuruRouteGroup("db")]
public abstract class DbGroup
{
  [GroupOption("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }
}

[NuruRoute("migrate", Description = "Apply migrations")]
public sealed class DbMigrateCommand : DbGroup, ICommand<Unit>
{
  [Option("dry-run", Description = "Print the plan only")]
  public bool DryRun { get; set; }

  public sealed class Handler : IRequestHandler<DbMigrateCommand, Unit>
  {
    public ValueTask<Unit> Handle(DbMigrateCommand command, CancellationToken cancellationToken)
    {
      // command.Verbose comes from the group option
      return Unit.ValueTask;
    }
  }
}
```

`myapp db migrate --dry-run -v` matches. `myapp db --help` lists the group's routes, and
`myapp db migrate --help` shows the parameters and options of that route.

## Results, output, and exit codes

| `System.CommandLine` | Nuru |
|----------------------|------|
| Action returns `int` | Set `Environment.ExitCode = n`. A returned `int` is written to stdout. |
| Action returns `void` or `Task` | Return `Unit` (`return Unit.ValueTask;`) |
| Write JSON yourself with `JsonSerializer` | Return a named record or class. Nuru writes it as JSON through a generated `JsonSerializerContext`, so it is AOT-safe. An anonymous type prints through `ToString()`. |
| Parse error: message on stderr, help, exit code 1 | Unknown option or command: one error line, exit code 1. A bad typed value: `Error: Invalid value 'x' for option '--count'. Expected: int`, exit code 1. Some of these errors go to **stdout**, not stderr; adjust tests that assert stderr. |
| Exception in the action: `EnableDefaultExceptionHandler` prints it, exit code 1 | The exception escapes `RunAsync`. Catch it in `Program.cs` (below). |

An exception thrown in a handler propagates out of `RunAsync`. Without a `catch`, the runtime prints the
stack trace and the process aborts (exit code 134 on Linux). To match the `System.CommandLine` behavior, catch
the exceptions your tool expects:

```csharp
try
{
  return await app.RunAsync(args);
}
catch (IOException ex)
{
  Console.Error.WriteLine($"error: {ex.Message}");
  return 1;
}
```

## Free-form trailing arguments

`System.CommandLine` collects leftover tokens in `ParseResult.UnmatchedTokens` when
`TreatUnmatchedTokensAsErrors = false`, or in an `Argument<string[]>`. In Nuru, use a catch-all parameter. It
must be the last parameter:

```csharp
[NuruRoute("get", Description = "Send a GET request")]
public sealed class GetCommand : IQuery<Unit>
{
  [Parameter(Description = "URL")]
  public string Url { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Request items: key=value, Header:value")]
  public string[] Items { get; set; } = [];

  [Option("verbose", "v", Description = "Print the request")]
  public bool Verbose { get; set; }

  public sealed class Handler : IRequestHandler<GetCommand, Unit>
  {
    public ValueTask<Unit> Handle(GetCommand command, CancellationToken cancellationToken)
    {
      foreach (string item in command.Items)
      {
        // classify each item yourself: "a=b", "a:=1", "Header:value", ...
      }

      return Unit.ValueTask;
    }
  }
}
```

`myapp get https://example.com name=nuru Accept:application/json -v` binds `Url`, both items, and `Verbose`.
Options declared on the route can appear before, between, or after the catch-all items.

Two rules decide what lands in the catch-all:

- **An option-shaped token that the route does not declare is an error**, as long as it comes before `--`.
  `myapp get url --pretty=all` fails with `Unknown option`. A catch-all only takes `-x` or `--name` tokens
  after `--`. `-` alone and negative numbers (`-5`) are positionals.
- **Tokens after `--` are never removed.** `--name=value`, `--name:value`, `/name=value`, and `/name:value`
  after `--` reach the catch-all unchanged and never become configuration overrides. Before `--`, undeclared
  tokens of that shape are still taken as configuration. See [Option syntax](#option-syntax).

Items such as `key=value`, `key:=json`, `Header:value`, and `:3000/path` do not start with `--` or `/letter`, so
they arrive unchanged.

## `--` passthrough

A tool that forwards arguments to another program (`dotnet r build -- --no-restore`) uses the same catch-all:

```csharp
[NuruRoute("run", Description = "Run a script")]
public sealed class RunCommand : ICommand<Unit>
{
  [Parameter(Description = "Script name")]
  public string Script { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Arguments for the script")]
  public string[] Args { get; set; } = [];

  [Option("if-present", Description = "Skip a missing script")]
  public bool IfPresent { get; set; }

  // Handler forwards command.Args
}
```

| Command line | `Args` |
|--------------|--------|
| `myapp run build` | `[]` |
| `myapp run build -- --no-restore -c Release` | `["--no-restore", "-c", "Release"]` |
| `myapp run build --if-present -- -x` | `["-x"]` (and `IfPresent` is `true`) |
| `myapp run build --no-restore` | Error: `Unknown option '--no-restore'` |
| `myapp run build -- --verbosity=quiet /p:Configuration=Release` | `[--verbosity=quiet, /p:Configuration=Release]`. Tokens after `--` are never removed. |

Nuru removes the `--` separator itself, so your handler does not see it. If the program you forward to needs
it, add it back.

The catch-all approach has one limit:

- **The catch-all cannot tell items before `--` from items after it.** A tool such as `dotnet r build test --
  --no-restore` takes several script names *and* forwarded arguments. With one catch-all, both end up in the
  same array.

When the limit matters, split the arguments at the first `--` in `Program.cs`, before Nuru sees them, and
hand the tail to the handler yourself:

```csharp
// Program.cs
int separator = Array.IndexOf(args, "--");
if (separator >= 0)
{
  Passthrough.Arguments = args[(separator + 1)..];
  args = args[..separator];
}

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

internal static class Passthrough
{
  public static string[] Arguments { get; set; } = [];
}
```

The handler reads `Passthrough.Arguments`. Everything after `--` reaches it unchanged. The route's own
catch-all (if any) holds only the items before `--`.

## Configuration and verbosity

### Configuration

`System.CommandLine.Hosting` apps call `UseHost(...)` to get `IConfiguration`, options binding, and DI.
Nuru builds configuration without a host. When any handler or service takes `IConfiguration` (or the app calls
`.AddConfiguration()`), the generated startup loads these sources, later ones winning:

1. `appsettings.json` and `appsettings.{Environment}.json` next to the executable
2. `{appname}.settings.json` and `{appname}.settings.{Environment}.json`
3. Environment variables (all of them, no prefix: `Database__Host=prod`)
4. Command-line overrides: `--Database:Host=prod`

Read a section in the handler:

```csharp
public sealed class Handler(IConfiguration configuration) : IRequestHandler<MigrateCommand, Unit>
{
  public ValueTask<Unit> Handle(MigrateCommand command, CancellationToken cancellationToken)
  {
    DatabaseOptions database = configuration.GetSection("Database").Get<DatabaseOptions>() ?? new();
    // An explicit option wins over configuration
    string host = command.Host ?? database.Host;
    return Unit.ValueTask;
  }
}
```

For AOT, set `<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>` in the project.
The configuration binder then generates `Get<T>()` and `Bind()` code instead of using reflection (warning
`IL2026` otherwise).

The [Configuration](../features/configuration.md) page also describes `IOptions<T>` injection by naming
convention (`DatabaseOptions` binds the `Database` section). In the 4.0 betas, `IOptions<T>` works as a
delegate handler parameter (Fluent DSL), but **not** as an endpoint `Handler` constructor parameter. That fails
at run time with `No service for type 'IOptions<T>'`. Inject `IConfiguration` and bind the section as shown.

### An existing service container

A tool that builds its own `ServiceCollection` (factory lambdas, `TryAddEnumerable`, logging setup) can keep
it. Either move the registrations into `ConfigureServices` and call `.UseMicrosoftDependencyInjection()` (the
runtime container supports factories), or build the container as before, store the `IServiceProvider` in a
static, and resolve from it in the handlers. The second keeps the change small; the first lets handler
constructors take the services.

### Verbosity

A `System.CommandLine` app usually maps a `--verbosity` option to a minimum log level at run time. In Nuru, the
`AddLogging` lambda is read at **compile time**, so the level you pass to `SetMinimumLevel` is fixed when you
build. To choose the level from a command-line option, log everything and filter on a value the handler sets:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services => services.AddLogging(logging => logging
    .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace)
    .SetMinimumLevel(LogLevel.Trace)
    .AddFilter((category, level) => level >= Verbosity.Minimum)))
  .DiscoverEndpoints()
  .Build();

public static class Verbosity
{
  public static LogLevel Minimum { get; set; } = LogLevel.Warning;
}

[NuruRouteGroup("db")]
public abstract class DbGroup
{
  [GroupOption("verbosity", "v", Description = "Minimum log level")]
  public LogLevel? Level { get; set; }
}

// First line of every handler in the group (or in one pipeline behavior):
// if (command.Level is { } level) Verbosity.Minimum = level;
```

`LogToStandardErrorThreshold = LogLevel.Trace` keeps log lines on stderr, so stdout stays clean for command
output. `ILogger<T>` can be injected into handler constructors, behaviors, and registered services. See
[Logging](../features/logging.md).

## AOT and trimming

### Where System.CommandLine apps get warnings

`System.CommandLine` 2.0 itself is annotated for trimming. The warnings in a typical app come from the code
around it:

| Source | Why it warns |
|--------|--------------|
| `System.CommandLine.NamingConventionBinder` (`CommandHandler.Create`) | Binds handler parameters by name through reflection |
| `System.CommandLine.DragonFruit` | Reflects over `Main`'s parameters |
| `System.CommandLine.Hosting` (`UseHost`, `BindCommandLine`, `GetCommandHandler`) | Reflection-based model binding, plus the generic host's dynamic DI |
| `JsonSerializer.Serialize(obj)` without a `JsonSerializerContext` | Reflection-based serialization (`IL2026`, `IL3050`) |
| `configuration.Get<T>()` / `Bind()` without the binder generator | Reflection-based configuration binding (`IL2026`) |
| `services.AddSingleton<T>()` with complex factories in a runtime container | Usually fine, but `ActivatorUtilities` paths warn |

Removing those is most of the work of an AOT migration on `System.CommandLine`. In Nuru, the generator already
does it:

- Route matching, type conversion, and binding into the endpoint's properties are generated code.
- Help, `--capabilities`, and `--json-args` binding are generated code.
- Dependency injection is source-generated by default. `UseMicrosoftDependencyInjection()` turns on the runtime
  container if you need it.
- Returned objects are serialized through a generated `JsonSerializerContext`.

What stays your job: your own `JsonSerializer` calls (add a `JsonSerializerContext`), configuration binding
(set `EnableConfigurationBindingGenerator`), and any reflection in your own code or dependencies.

### Publish with zero warnings

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net11.0</TargetFramework>
  <PublishAot>true</PublishAot>
  <EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>
</PropertyGroup>
```

```bash
dotnet publish -c Release -r linux-x64
```

A Nuru app with endpoints, a group option, `AddLogging`, `IConfiguration` binding, and an enum option publishes
this way with no `IL` warnings. The binary is about 6 MB on linux-x64. Treat a new `IL2xxx` or `IL3xxx` warning
as a defect in your code or a dependency, and fix it rather than suppressing it. See
[Deployment](deployment.md) for cross-platform builds and size options.

## Packaging as a .NET tool

A global tool keeps working. Retarget and keep the tool properties:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net11.0</TargetFramework>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>mytool</ToolCommandName>
  <RollForward>Major</RollForward>
</PropertyGroup>
```

```bash
dotnet pack -c Release -o ./artifacts
dotnet tool install --global --add-source ./artifacts MyTool --prerelease
mytool --help
```

If the repo's `nuget.config` lists a private feed you cannot reach, `--add-source` still queries it and the
install fails with `401`. Use `--source ./artifacts` instead, which replaces the configured feeds.

Points to check:

- Users need the .NET 11 runtime to run a framework-dependent `net11.0` tool. `RollForward` lets it run on a
  later runtime, not an earlier one. If your tool used to multi-target `net8.0;net9.0;net10.0`, call this out in
  your release notes.
- Library projects that the CLI references may warn once Nuru brings the 11.0 `Microsoft.Extensions.*`
  packages into their `net8.0`/`net9.0` builds. Under `TreatWarningsAsErrors`, retarget them too.
- `dotnet publish` needs `-f net11.0` when the project uses `<TargetFrameworks>` (plural), even with one
  value (`NETSDK1129`).
- `--version` prints `AssemblyInformationalVersion`, which comes from `<Version>`.
- `--prerelease` is needed while your tool depends on a prerelease package.
- `PublishAot` in the project file does not change `dotnet pack`: the package is still a framework-dependent
  `tools/net11.0/any` tool. If the project also sets `GeneratePackageOnBuild`, `dotnet publish` with AOT fails
  ("GeneratePackageOnBuild is not supported for native compilation"). Make it conditional:
  `<GeneratePackageOnBuild Condition="'$(PublishAot)' != 'true'">true</GeneratePackageOnBuild>`, or drop it.
- To ship native binaries instead, publish with `PublishAot` per runtime identifier and distribute the
  archives (see [Deployment](deployment.md)).

## Tests

`System.CommandLine` tests usually call `root.Parse(args)` or `InvokeAsync(args, console)`. In Nuru, run the
app with a `TestTerminal` and assert on the output and the exit code. `ShouldBeTrue()` comes from the
[Shouldly](https://www.nuget.org/packages/Shouldly) package; use your test framework's assertion if you do
not reference it:

```csharp
using Shouldly;
using TimeWarp.Terminal;

using TestTerminal terminal = new();
NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .Map<DeployCommand>()
  .Build();

int exitCode = await app.RunAsync(["deploy", "prod", "--tag", "v1"]);
terminal.OutputContains("Deploying").ShouldBeTrue();
```

That works when the test builds the app in the **same assembly** as the endpoints. Most ported tools have a
separate test project that references the CLI project, and there it does not:

- The `TimeWarp.Mediator` source generator flows into the test project and runs in both and emits a `Mediator` type in each. With
  `InternalsVisibleTo`, the test project gets `CS0436` (type conflicts with an imported type).
- An app built in the test assembly uses the test assembly's `Mediator`, which knows no handlers. It fails
  with `NoHandlerException: No handler registered for request type '...'`.

Build the app in the CLI assembly and let tests call it:

```csharp
// In the CLI project
internal static class CliApp
{
  internal static async Task<int> RunAsync(string[] args, ITerminal? terminal = null)
  {
    // The builder must be a literal chain, so each variant is spelled out
    if (terminal is null)
    {
      NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
      return await app.RunAsync(args);
    }

    NuruApp testApp = NuruApp.CreateBuilder().UseTerminal(terminal).DiscoverEndpoints().Build();
    return await testApp.RunAsync(args);
  }
}
```

`Program.cs` becomes `return await CliApp.RunAsync(args);`, and the tests call
`CliApp.RunAsync([...], terminal)` (through `InternalsVisibleTo`). Then keep the generators out of the test
project so `CS0436` goes away. The simplest way is `PrivateAssets` on the CLI project's Nuru reference:

```xml
<PackageReference Include="TimeWarp.Nuru" Version="4.0.0-beta.14" PrivateAssets="analyzers;build" />
```

Use exactly `analyzers;build`. `PrivateAssets="all"` drops the `TimeWarp.Nuru` runtime assembly from the AOT
publish (`Failed to load assembly 'TimeWarp.Nuru'` at startup), and adding `compile` breaks projects that use
the Mediator contracts.

If `CS0436` remains, remove the analyzer in the test project instead:

```xml
<!-- Test project: the CLI assembly already has the generated Mediator -->
<Target Name="RemoveTransitiveMediatorGenerator" BeforeTargets="CoreCompile;GenerateMSBuildEditorConfigFileCore">
  <ItemGroup>
    <Analyzer Remove="@(Analyzer)" Condition="$([System.String]::Copy('%(Filename)').StartsWith('TimeWarp.Mediator.Generators'))" />
  </ItemGroup>
</Target>
```

`Environment.ExitCode` is process-wide. Reset it to `0` before each test that checks an exit code. Code that
writes errors with `Console.Error` instead of `ITerminal` is not captured by `TestTerminal`.

The `ConfigureServices` lambda is compiled, so a test cannot pass a substitute service into it. Expose the
services your tests replace as settable statics (or a small holder type) that the registration or the handler
reads.

## The 2.0 API churn table

If you are on a 2.0 beta, you do not need to move to 2.0 first. Find each API you use in this table and go
straight to the Nuru column.

| beta4 and earlier (2022) | 2.0 (beta5 and later) | Nuru |
|--------------------------|-----------------------|------|
| `command.SetHandler((string a, int b) => ..., argA, optB)` | `command.SetAction(parseResult => ...)` | A nested `Handler` class. Values arrive as endpoint properties. |
| `InvocationContext context` | `ParseResult parseResult` (+ `CancellationToken`) | The endpoint instance and the `Handle` `CancellationToken` |
| `context.ExitCode = 2` | Return `int` from the action | `Environment.ExitCode = 2` |
| `context.Console` / `IConsole` | `parseResult.Configuration.Output` / `InvocationConfiguration.Output` | Inject `ITerminal`. Test with `TestTerminal`. |
| `new CommandLineBuilder(root).UseDefaults().Build()` | `root.Parse(args)` / `ParserConfiguration` + `InvocationConfiguration` | `NuruApp.CreateBuilder()...Build()` |
| `.UseExceptionHandler(...)` | `InvocationConfiguration.EnableDefaultExceptionHandler` | `try`/`catch` around `RunAsync` |
| `.AddMiddleware(...)` | Removed | A pipeline behavior: `.AddBehavior(typeof(MyBehavior))` |
| `command.AddOption(o)` / `AddArgument(a)` / `AddCommand(c)` | `command.Options.Add(o)` / `Arguments.Add(a)` / `Subcommands.Add(c)` | Properties with `[Option]` / `[Parameter]`; a class with `[NuruRoute]` |
| `root.AddGlobalOption(o)` | `Recursive = true` | `[GroupOption]` on a `[NuruRouteGroup]` base |
| `new Option<int>("--count", getDefaultValue: () => 1)` | `DefaultValueFactory = _ => 1` | Property initializer `= 1` |
| `option.IsRequired = true` | `Required = true` | Nullable option plus a handler check |
| `option.AddAlias("-c")` / `new Option(new[] { "--count", "-c" })` | `new Option<int>("--count", "-c")` / `Aliases.Add` | `[Option("count", "c")]` |
| `option.ArgumentHelpName = "N"` | `HelpName = "N"` | Help uses the property name |
| `option.FromAmong("a", "b")` | `AcceptOnlyFromAmong("a", "b")` | An enum, or a handler check |
| `option.AddValidator(r => ...)` | `Validators.Add(r => ...)` | A handler check, or a pipeline behavior |
| `option.AllowMultipleArgumentsPerToken = true` | Same | A `string[]` property, one value per flag |
| `parseResult.GetValueForOption(o)` / `GetValueForArgument(a)` | `parseResult.GetValue(o)` | Endpoint property |
| `BinderBase<T>` custom binder | Removed | A property of the type with a type converter, or inject a service |
| `System.CommandLine.NamingConventionBinder` `CommandHandler.Create(...)` | Separate package, reflection | A nested `Handler` class |
| `System.CommandLine.Hosting` `UseHost(...)` | Separate package | `ConfigureServices`; configuration and logging built in |
| `System.CommandLine.DragonFruit` (`Main(string name, int count)`) | Not updated for 2.0 | `Map("{name} --count {count:int}").WithHandler(...)` |
| `[suggest]` directive / `dotnet-suggest` | Same | `.EnableCompletion()` |
| `TreatUnmatchedTokensAsErrors = false` | Same | A catch-all parameter |

## Behavior differences to check

Each item comes from running the 4.0.0-beta.14 package. Check the ones that affect your tool:

1. **Root `--help` does not list the default route's parameters or options.** A single-command tool
   (`[NuruRoute("")]`) shows only the route description under `Commands:`. Put a usage line in
   `.WithDescription(...)` until this changes. Subcommands show their parameters and options under
   `<command> --help`.
2. **Help looks different.** Help is drawn as tables about 80 columns wide, so long command and option names
   are truncated. Default values, `(REQUIRED)` markers, and enum choices are not shown. Put them in the
   `Description` text if your users rely on them.
3. **Extra positional arguments.** On a route that has a `[Parameter]`, extras are ignored: one parameter
   given `a b` binds `a`, drops `b`, and exits 0. On a route with no parameters, an extra is
   `Unknown command`, exit 1. `System.CommandLine` reports `Unrecognized command or argument 'b'`. Use a
   catch-all parameter when you need to see or reject extras.
4. **An option with a missing value binds its default.** `myapp -o` (no value) leaves `Output` at its default
   with exit code 0.
5. **Handler exceptions are not caught.** See [Results, output, and exit codes](#results-output-and-exit-codes).
6. **`IOptions<T>` cannot be injected into an endpoint `Handler` constructor.** Inject `IConfiguration`.
7. **Ctrl+C ends the process.** The handler's `CancellationToken` is not cancelled in a CLI run, so there is
   no graceful-shutdown window like `System.CommandLine`'s `ProcessTerminationTimeout`. If you need cleanup,
   register `Console.CancelKeyPress` or `PosixSignalRegistration` yourself.
8. **`-h` belongs to help.** An existing `-h` option (for example `-h` for `--headers`) loses its short form.
   No required-option attribute, no `-?` help alias, no bundled short options, and no `[diagram]`,
   `[parse]`, `[env]`, or `[suggest]` directives.

   An enum cannot hold a hyphenated value such as `dotnet-coverage`; check those in the handler.
9. **`--version` has no `-v` alias.** That leaves `-v` free for `--verbose`, as in most `System.CommandLine`
   tools. A value option named `version` (`[Option("version")] public string? Version`) still binds
   `--version 1.2`; a bare `--version` prints the app version.
10. **`bool` flags cannot default to `true`, and `--flag false` is not supported.** See the concept map.
11. **Built-in routes reserve names**: `--help`, `-h`, `--version`, `--capabilities`, `--json-args`, plus
   `--interactive`/`-i` when you add the REPL. Do not declare these yourself.

## What you get after the port

- **`--capabilities`**: a JSON catalog of every route for agents. It is a flat `endpoints` array with
  `parameters[]` and `options[]` per route. See [Migrating to 3.0](migrating-to-3.0.md#--capabilities-json-is-a-flat-endpoints-list)
  for the shape and [Agent invocation](../features/agent-invocation.md) for `--json-args`.
- **Testable handlers**: build the app with `UseTerminal(new TestTerminal())` and assert on output. See
  [Best Practices](best-practices.md).
- **REPL**: `.AddRepl()` turns the same routes into an interactive shell.
- **MCP**: `TimeWarp.Nuru.McpHost` serves the routes as MCP tools.

## Migration checklist

- [ ] Retarget to `net11.0`, swap the packages, and remove every `System.CommandLine*` reference
- [ ] One `[NuruRoute]` class per `Command`; `[NuruRoute("")]` for the root action
- [ ] `[Parameter]` per argument, properties in command-line order (catch-all last)
- [ ] `[Option]` per option; defaults as initializers; required options checked in the handler
- [ ] `[NuruRouteGroup]` per command level; `[GroupOption]` for recursive options
- [ ] Exit codes through `Environment.ExitCode`; expected exceptions caught around `RunAsync`
- [ ] `--` passthrough and free-form tails through a catch-all, tested with `=` tokens
- [ ] Configuration through `IConfiguration`; verbosity through a runtime log filter
- [ ] Docs and scripts updated: no `-?`, no `--flag=true` on bool flags
- [ ] `dotnet publish -c Release -r <rid>` with `PublishAot` and zero warnings, or `dotnet pack` for a tool

## See also

- [`tw-nuru-migrate-system-commandline` skill](../../../skills/tw-nuru-migrate-system-commandline/SKILL.md) - the
  agent recipe for this guide
- [Migrating to 3.0](migrating-to-3.0.md) - Nuru 2.x to 3.0 changes
- [Migrating to TimeWarp.Mediator 15](migrating-to-timewarp-mediator-15.md) - handler interfaces in Nuru 4.0
- [Endpoints](../features/endpoints.md), [Routing](../features/routing.md),
  [Configuration](../features/configuration.md), [Logging](../features/logging.md)
- [Deployment](deployment.md) - AOT, runfiles, and distribution
