---
name: tw-nuru-migrate-system-commandline
description: Port a .NET CLI from System.CommandLine (2.0 or any 2.0 beta, including SetHandler/InvocationContext/CommandLineBuilder, NamingConventionBinder, Hosting, DragonFruit) to TimeWarp.Nuru. Use when asked to migrate, port, or convert a System.CommandLine app to Nuru, to make a System.CommandLine CLI Native AOT or trim clean, or to map RootCommand/Command/Option<T>/Argument<T>/SetAction to Nuru routes.
---

# Port a System.CommandLine CLI to TimeWarp.Nuru

The full explanation, with the reasons and the long tables, is the docs page
[Migrating from System.CommandLine](../../documentation/user/guides/migrating-from-system-commandline.md)
(online: https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/documentation/user/guides/migrating-from-system-commandline.md).
This skill is the recipe. Load the `tw-nuru` skill too for general Nuru syntax.

Follow the steps in order. When a step does not say how to do something you need, do not guess silently:
write it down as an **invented step** in your report, so the page and this skill can be fixed.

## Step 0: Inventory

Before you edit anything, list:

1. Every `System.CommandLine*` package reference and its version. A version below `2.0.0-beta5` means beta4
   APIs (`SetHandler`, `InvocationContext`, `CommandLineBuilder`).
2. Every `RootCommand` and `Command`, with its options, arguments, aliases, defaults, `Required`/`IsRequired`,
   `Recursive`/`AddGlobalOption`, validators, and `AcceptOnlyFromAmong`/`FromAmong`.
3. How the action reads values (`GetValue`, `SetHandler` value descriptors, a custom `BinderBase<T>`, or
   `CommandHandler.Create` by name).
4. How it sets the exit code and handles exceptions.
5. Passthrough (`--`, `TreatUnmatchedTokensAsErrors = false`, `UnmatchedTokens`) and free-form trailing
   items (`key=value`, `Header:value`).
6. Verbosity and configuration (`UseHost`, `IConfiguration`, a `--verbosity` option, `ILogger`).
7. The project's target frameworks, `PackAsTool`, `PublishAot`, `IsAotCompatible`, and
   `TreatWarningsAsErrors`.
8. Existing tests that call the parser (`root.Parse(...)`, `InvokeAsync(...)`). They need porting too.

Run the old CLI's `--help` for each command and save the output. You compare against it at the end.

## Step 1: Project file

- `<TargetFramework>net11.0</TargetFramework>`. Nuru 4.0 ships `lib/net11.0` only. Drop older targets from
  every project that references Nuru (the CLI and its tests). Libraries that do not reference Nuru can keep
  their targets.
- Under `TreatWarningsAsErrors`, retarget the library projects the CLI references too: Nuru's 11.0
  `Microsoft.Extensions.*` packages warn in their `net8.0`/`net9.0` builds.
- If `global.json` pins an older SDK, set `sdk.version` to an installed .NET 11 SDK. Repos versioned by
  Nerdbank.GitVersioning need full history (`git fetch --unshallow`) to build.
- `GeneratePackageOnBuild` breaks an AOT publish. Make it `Condition="'$(PublishAot)' != 'true'"`.
- Add `<PackageReference Include="TimeWarp.Nuru" Version="4.0.0-beta.14" />` (or the latest 4.0 beta).
  Central package management: add the version to `Directory.Packages.props`.
- Remove `System.CommandLine`, `System.CommandLine.NamingConventionBinder`, `System.CommandLine.Hosting`,
  `System.CommandLine.DragonFruit`, `System.CommandLine.Rendering`.
- Do not add `Microsoft.Extensions.Logging.Console`, `Microsoft.Extensions.Configuration*`, or
  `Microsoft.Extensions.Options.ConfigurationExtensions` at a lower version. Nuru brings the 11.0 versions,
  and a lower direct reference fails restore with `NU1605` (package downgrade).
- AOT: `<PublishAot>true</PublishAot>`, and `<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>`
  when the code calls `configuration.Get<T>()` or `Bind()`.
- Global tool: keep `PackAsTool` and `ToolCommandName`. Add `<RollForward>Major</RollForward>` if it is not
  there.
- The TimeWarp build tasks print a `Failed to retrieve git metadata` warning outside a git repository. Build
  inside the repo clone.

## Step 2: Entry point

Replace the `RootCommand` setup and `Parse(args).Invoke()` with:

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .WithDescription("One line about the tool")
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
```

`.WithDescription(...)` takes a plain or raw string literal only. An interpolated string compiles but
prints an empty description.

`Program.cs` must use top-level statements or an `async Task<int> Main` (update tests that call `Main`).
Keep `RunAsync` on an app you built in the same method (`NURU_I001` warns otherwise).

## Step 3: One endpoint class per command

| System.CommandLine | Nuru |
|--------------------|------|
| `RootCommand` action | `[NuruRoute("")]` class |
| `Command("name")` | `[NuruRoute("name", Description = "...")]` class |
| Subcommand of `parent` | Class inherits an abstract `[NuruRouteGroup("parent")]` base |
| `Aliases.Add("x")` on a command | `[NuruRouteAlias("x")]` |
| `Argument<T>` required | `[Parameter] public T Name { get; set; }` (non-nullable) |
| `Argument<T?>` optional | `[Parameter] public T? Name { get; set; }` |
| optional argument with a default | nullable property, default applied in the handler (`?? "x"`) |
| optional argument before a required one (`[method] url`) | one catch-all, classify tokens in the handler |
| `Argument<T[]>` | `[Parameter(IsCatchAll = true)] public string[] Items { get; set; } = [];` (last) |
| several arguments | one `[Parameter]` property each, declared in command-line order (position = declaration order; `Order` is not read and `NURU_A002` is not reported on 4.0.0-beta.14, so do not set `Order`) |
| `Option<T>("--name", "-n")` | `[Option("name", "n")]` (no dashes) |
| extra long aliases (`--offline`, `--dry-run`) | one `[Option]` takes one long + one short (the short may be multi-char: `"cs"` → `-cs`); add a property per extra long name and OR them, or rewrite aliases in argv before `RunAsync` |
| an existing `-h` option | loses `-h` (reserved for help); keep the long form |
| `Option<bool>` | `bool` property: a flag (absent = `false`) |
| `Option<bool>` defaulting to `true` | **no bool initializer works** (`= true` is ignored); invert the flag (`--no-x`) |
| `--flag true` / `--flag false` | not supported (`false` becomes a positional); rewrite argv or drop |
| default value | property initializer (value options only) |
| `Required = true` / `IsRequired` | nullable property + check in handler (no attribute) |
| `Option<T[]>`, `AllowMultipleArgumentsPerToken` | `[Option("x")] public string[] X { get; set; } = [];` (the array/collection type makes it repeat; `IsRepeated` is not read on 4.0.0-beta.14) |
| `AcceptOnlyFromAmong` / `FromAmong` | an `enum` property, or a handler check |
| `Validators.Add` / `AddValidator` | handler check |
| `Recursive = true` / `AddGlobalOption` | `[GroupOption("name", "n")]` on the group base |
| options shared by top-level sibling commands | abstract base with `[NuruRouteGroup("")]` + `[GroupOption]` properties |
| `SetAction` / `SetHandler` body | nested `public sealed class Handler : IRequestHandler<TEndpoint, Unit>` |
| `GetValue(option)` | `command.Property` |
| `IConsole` / `context.Console` | inject `ITerminal` (`using TimeWarp.Terminal;`) |
| `System.CommandLine.Rendering` | `TimeWarp.Terminal` colors/widgets, or keep the existing output library (Spectre works) |
| `CancellationToken` | `Handle(..., CancellationToken cancellationToken)` |

Template:

```csharp
using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("deploy", Description = "Deploy the app")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Environment { get; set; } = string.Empty;

  [Option("tag", "t", Description = "Image tag")]
  public string? Tag { get; set; }

  [Option("replicas", Description = "Replica count")]
  public int Replicas { get; set; } = 1;

  public sealed class Handler(ITerminal terminal) : IRequestHandler<DeployCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployCommand command, CancellationToken cancellationToken)
    {
      // old action body; on failure:
      // terminal.WriteErrorLine("..."); System.Environment.ExitCode = 1;
      terminal.WriteLine($"Deploying {command.Tag ?? "latest"} to {command.Environment}");
      return Unit.ValueTask;
    }
  }
}
```

Rules:

- Endpoint and `Handler` are `sealed`, and either `public` or `internal`. Use `internal` when the handler
  takes an `internal` service (`CS0051` otherwise). `[NuruRoute]` takes one literal or `""`, never a pattern.
- A plain `[Option]` on a base class **without** `[NuruRouteGroup]` is silently ignored (the option becomes
  `Unknown option`). Shared options go on a `[NuruRouteGroup]` base as `[GroupOption]`; use
  `[NuruRouteGroup("")]` for no prefix. A `string[]` `[GroupOption]` repeats like a `string[]` `[Option]`.
- An enum cannot hold a hyphenated choice (`dotnet-coverage`). Use `string` and check it in the handler.
- Do not name a property `Type` (generated code fails to compile). Enum option types need a simple name that
  is unique in the compilation (`CS0104` in generated code otherwise).
- Under strict analyzers (`AnalysisMode=AllEnabledByDefault`), make endpoints `internal sealed` to avoid
  CA1515/CA1034/CS1591 on generated-facing public types.
- The `Handle` parameter is named `request` in `IRequestHandler`. Repos with Roslynator (`RCS1168`) want that
  name.
- `Handle` returns `ValueTask<T>`: `return Unit.ValueTask;` from a synchronous body, `return Unit.Value;` from
  an `async` one. Use `ICommand<Unit>` (side effects), `IQuery<Unit>` (read-only), or
  `IIdempotentCommand<Unit>`. These live in `TimeWarp.Mediator`.
- A property named like a type in scope (`Environment`) shadows it. Write `System.Environment.ExitCode`.
- An endpoint property of type `FileInfo`, `DirectoryInfo`, or `IPAddress` does not bind. Bind `string` and
  convert in the handler. Endpoint properties do bind `string`, the numeric types, `bool`, `char`, `Guid`,
  `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `BigInteger`, `Uri`, `Version`, and
  enums. (Fluent `{file:FileInfo}` constraints convert `FileInfo`/`DirectoryInfo`; endpoint properties do not.)
- Keep business logic where it was. Move only the parsing and wiring.
- The builder must be one literal chain in one method. A ternary or helper around part of the chain fails
  with `NURU_S999`.

## Step 4: Exit codes and errors

- `return 2;` from an action → `System.Environment.ExitCode = 2; return Unit.ValueTask;` in the synchronous
  Step 3 template (`return Unit.Value;` only in an `async` `Handle`). A returned `int` is printed, not used as
  the exit code.
- `context.ExitCode = n` (beta4) → same.
- Wrap `RunAsync` in `try`/`catch` for the exceptions the old `EnableDefaultExceptionHandler` or
  `UseExceptionHandler` turned into exit code 1. Without it, a handler exception aborts the process with a
  stack trace (exit 134 on Linux).
- Required options: `if (command.X is null) { terminal.WriteErrorLine("Option '--x' is required."); System.Environment.ExitCode = 1; return Unit.ValueTask; }`

## Step 5: Passthrough and free-form tails

- `--` passthrough and `UnmatchedTokens` → `[Parameter(IsCatchAll = true)] public string[] Args { get; set; } = [];`
  as the last declared parameter property.
- Before `--`, an undeclared `-x` or `--name` token is an error (`Unknown option`). After `--`, it goes into the
  catch-all. Nuru removes the `--` itself. Add it back if the child program needs it.
- Free-form items (`key=value`, `key:=1`, `Header:value`) arrive unchanged in the catch-all. Classify them in
  the handler. Declared options can sit between them.
- Tokens after `--` are never removed or taken as configuration, so `/p:Configuration=Release` and
  `--verbosity=quiet` forward through a catch-all unchanged.
- Before `--`, `--name=value` and `--name:value` bind a declared long option (value = everything after the
  first `=` or `:`). Undeclared names (`--Section:Key=v`) and every slash form (`/Mode=fast`, declared or
  not) are still configuration overrides. `-o=value` binds a declared short value option.
  Before the release after 4.0.0-beta.14, `--name=value` never bound.
- **Users rely on a legacy alias with `=` (`--old=value`)?** An undeclared alias is taken as configuration.
  Rewrite argv in `Program.cs` before `RunAsync`: turn the alias into the canonical name. Stop at `--` and
  copy it and the tail unchanged. Example: [Option syntax](../../documentation/user/guides/migrating-from-system-commandline.md#option-syntax).
- **Split at `--` first** when the tool takes variable items before `--` *and* forwards items after it (a
  single catch-all cannot tell them apart):

```csharp
// Program.cs, before CreateBuilder (the class goes after the top-level statements)
int separator = Array.IndexOf(args, "--");
if (separator >= 0)
{
  Passthrough.Arguments = args[(separator + 1)..];
  args = args[..separator];
}

internal static class Passthrough { public static string[] Arguments { get; set; } = []; }
```

  The handler forwards `Passthrough.Arguments`. The route's catch-all then holds only the items before `--`.

## Step 6: Configuration and verbosity

- A tool that builds its own `ServiceCollection` can keep it: store the `IServiceProvider` in a static and
  resolve in handlers, or move registrations into `ConfigureServices` with `.UseMicrosoftDependencyInjection()`.
- `UseHost(...)` → `.ConfigureServices(services => ...)` on the builder (`using Microsoft.Extensions.DependencyInjection;`).
  Register services with `AddSingleton`/`AddScoped`/`AddTransient`. Factory lambdas need `.UseMicrosoftDependencyInjection()`.
- Configuration: inject `IOptions<T>` or `IConfiguration` into the `Handler` constructor.
  `IOptions<DatabaseOptions>` binds the `Database` section (a trailing `Options` is removed);
  `[ConfigurationKey("Api")]` selects the section. An `IValidateOptions<T>` in the project runs when the
  value is created. `services.Configure<T>(...)` reports `NURU052` and does nothing under source-generated
  DI. Call `.UseMicrosoftDependencyInjection()` for `Configure`, data annotations, `IOptionsSnapshot<T>`,
  or `IOptionsMonitor<T>`. `IConfiguration` still works: `configuration.GetSection("X").Get<XOptions>()`.
- Sources: `appsettings.json`, `{app}.settings.json`, environment variables (no prefix), `--Section:Key=value`.
- Verbosity: the `AddLogging` lambda is compiled, so `SetMinimumLevel(fromArgs)` cannot work. Use a runtime
  filter:

```csharp
.ConfigureServices(services => services.AddLogging(logging => logging
  .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace)
  .SetMinimumLevel(LogLevel.Trace)
  .AddFilter((category, level) => level >= Verbosity.Minimum)))

public static class Verbosity { public static LogLevel Minimum { get; set; } = LogLevel.Warning; }
```

  Then put `[GroupOption("verbosity", "v")] public LogLevel? Level { get; set; }` on the group base (or an
  `[Option]` per endpoint), and set `Verbosity.Minimum` at the top of each handler, or once in a pipeline
  behavior.

## Step 7: Output

- `JsonSerializer.Serialize(result)` → return a named record or class from the handler (`ICommand<MyResult>`).
  Nuru writes it as JSON with a generated context. An anonymous type prints `ToString()`.
- Keep your own `JsonSerializer` calls only with a `JsonSerializerContext` (AOT).
- Logs to stderr, results to stdout.

## Step 8: Tests

Port parser tests to run the app with a `TestTerminal`:

```csharp
using TestTerminal terminal = new();
NuruApp app = NuruApp.CreateBuilder().UseTerminal(terminal).Map<DeployCommand>().Build();
int exitCode = await app.RunAsync(["deploy", "prod", "--tag", "v1"]);
```

Use `Map<T>()`, not `DiscoverEndpoints()`, in a test assembly that builds several apps (`NURU_A003`).

**Separate test project (the usual layout).** An app built in the test assembly fails with
`NoHandlerException`, and the test project gets `CS0436` (both assemblies generate a `Mediator` type). Do this:

1. In the CLI project, add `internal static class CliApp` with
   `internal static async Task<int> RunAsync(string[] args, ITerminal? terminal = null)` that builds the app
   (two literal chains: with and without `.UseTerminal(terminal)`). `Program.cs` calls it.
2. `InternalsVisibleTo` the test project. Tests call `CliApp.RunAsync([...], terminal)`.
3. Keep the generators out of the test project: `PrivateAssets="analyzers;build"` on the CLI project's
   `TimeWarp.Nuru` `PackageReference`. Exactly that value: `all` breaks the AOT binary (`TimeWarp.Nuru` not
   loaded), and adding `compile` breaks Mediator contract use. If `CS0436` remains, remove the analyzer in
   the test project:

```xml
<Target Name="RemoveTransitiveMediatorGenerator" BeforeTargets="CoreCompile;GenerateMSBuildEditorConfigFileCore">
  <ItemGroup>
    <Analyzer Remove="@(Analyzer)" Condition="$([System.String]::Copy('%(Filename)').StartsWith('TimeWarp.Mediator.Generators'))" />
  </ItemGroup>
</Target>
```

4. Reset `Environment.ExitCode = 0` before each exit-code assertion (it is process-wide). Errors written
   with `Console.Error` are not captured by `TestTerminal`; move them to `ITerminal` if tests assert them.
5. `ConfigureServices` lambdas are compiled, so tests cannot inject substitutes. Expose the services tests
   replace as settable statics that the registration or handler reads.

Full example: [Tests](../../documentation/user/guides/migrating-from-system-commandline.md#tests).

## Step 9: Verify

1. `dotnet build` with zero warnings (keep the repo's `TreatWarningsAsErrors`).
2. Every command's `--help` against the Step 0 output. Root `--help` lists the default route's parameters,
   options, and examples. A default route with only a description still shows that description under
   `Commands:` with a blank name.
3. The old tests, ported, pass.
4. Run each command with the arguments from the tool's readme. Check that `--name=value` examples
   bind declared options and that `--flag=true` on a bool flag is not used.
5. AOT: `dotnet publish -c Release -r linux-x64` (with `PublishAot`; add `-f net11.0` if the project uses
   plural `<TargetFrameworks>`). Expect zero `IL2xxx`/`IL3xxx` warnings from Nuru. Fix new warnings in the
   code. Do not suppress them. Warnings from other dependencies (Dapper, Roslyn scripting) stay theirs.
6. Tool: `dotnet pack -c Release -o ./artifacts`, then
   `dotnet tool install --tool-path ./tool-test --add-source ./artifacts <PackageId> --prerelease`, then
   `./tool-test/<command> --help`. Run it from a folder with no project file. If a private feed in
   `nuget.config` returns `401`, use `--source ./artifacts` instead of `--add-source`.

## Behavior differences to tell the user about

- `--flag=true` on a bool flag is an unknown-option error (`Unknown option '--verbose=true'`), exit 1.
- Extra positional arguments are ignored when the route has a `[Parameter]`; on a route with none they are
  `Unknown command`. Several values per token (`--env A B`) do the same: `B` becomes a positional.
- Help is a table about 80 columns wide: long names truncated, no defaults, no `(REQUIRED)`, no enum choices.
- `bool` flags cannot default to `true`; `--flag false` is not supported.
- Some parse errors print to stdout, not stderr.
- A user option named `version` binds `--version 1.2`; bare `--version` is the built-in.
- Ctrl+C ends the process; the handler's `CancellationToken` is not cancelled in a CLI run.
- Handler exceptions abort the process unless `Program.cs` catches them.
- `-o` with no value binds the default, exit 0.
- No `-?`, no bundled short options (`-vo`), option names case-sensitive, `-h` is help.
- `--help`, `-h`, `--version`, `--capabilities`, `--json-args` are reserved.

Details and examples: [Behavior differences to check](../../documentation/user/guides/migrating-from-system-commandline.md#behavior-differences-to-check).

## Report

End a port with:

- What was ported, what was left out and why.
- Gate results (build, tests, `--help` comparison, AOT publish warnings count, tool pack/install).
- **Invented steps**: anything this skill or the docs page did not tell you how to do.
- Behavior differences the tool's users will see.
