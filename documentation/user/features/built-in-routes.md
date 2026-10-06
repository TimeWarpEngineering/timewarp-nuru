# Built-in Routes

When you use `NuruApp.CreateBuilder()`, several utility routes are built in to provide common CLI functionality out of the box.

## Overview

| Route | Description |
|-------|-------------|
| `--version` | Display version information. There is no `-v` alias. |
| `--check-updates` | Check GitHub for newer versions (opt-in via `AddCheckUpdatesRoute()`) |
| `--help`, `-h` | Show help (see [Auto-Help](auto-help.md)) |
| `--interactive`, `-i` | Enter REPL mode (requires `AddRepl()`) |
| `--capabilities` | Machine-readable endpoint catalog for agents |
| `--json-args <value>` | Bind parameter and option values from JSON |

## Version Route (`--version`)

Displays the application version. `CreateBuilder()` registers this route. There is no `-v` form.

### Output Format

```bash
$ myapp --version
1.2.3
```

When the app model has a name, that name is written on the same line before the version:

```bash
$ myapp --version
MyApp 1.2.3
```

### What It Displays

`VersionEmitter` writes one line:

1. The assembly informational version, when `AssemblyInformationalVersionAttribute` is present.
2. Otherwise `AssemblyName.Version`.
3. Otherwise `1.0.0`.

It does not write a commit hash or a commit date.

## Check Updates Route (`--check-updates`)

Queries GitHub releases to check if a newer version is available.

### Output Examples

**Up to date:**
```bash
$ myapp --check-updates
✓ You are on the latest version
```

**Update available:**
```bash
$ myapp --check-updates
⚠ A newer version is available: 2.0.0
  Released: 2024-02-01
  https://github.com/owner/repo/releases/tag/v2.0.0
```

### Prerequisites

This route requires the `RepositoryUrl` property to be set in your project file:

```xml
<PropertyGroup>
  <RepositoryUrl>https://github.com/your-org/your-repo</RepositoryUrl>
</PropertyGroup>
```

If `RepositoryUrl` is not configured or is not a GitHub URL, the route displays an informative error:

```bash
$ myapp --check-updates
Unable to check for updates: RepositoryUrl not configured in project
```

### Version Comparison Logic

- Compares SemVer versions (major.minor.patch)
- A stable current version is compared only with stable GitHub releases
- A pre-release current version (the version string contains `-`, for example `1.0.0-beta.1`) is compared with every release, stable and pre-release
- Colored output: green checkmark for up-to-date, yellow warning for updates

## Interactive Route (`--interactive`, `-i`)

Enters REPL (Read-Eval-Print Loop) mode for interactive command execution.

```bash
$ myapp --interactive
Welcome to MyApp
myapp> add 1 2
3
myapp> multiply 3 4
12
myapp> exit
```

Inside the REPL, `key-bindings` lists the active built-in profile. See [REPL Key Bindings](repl-key-bindings.md) for keyboard shortcuts, filters, and customization.

## Help Route (`--help`, `-h`)

Displays help information for your application. See [Auto-Help](auto-help.md) for details on customizing help output.
Per-route help (`<command> --help`) also renders an "Examples:" section when the route declares
usage examples via `[NuruRouteExample]` or `.WithExample()`. When several routes share the typed
literal prefix, help prints each of them in that same layout, most specific first. A single match
prints that route alone. Longer commands and near-prefixes stay on their own help invocations.
See [Auto-Help](auto-help.md) for the shared-prefix case.

## Capabilities and JSON arguments

`--capabilities` writes the endpoint catalog as JSON. `--json-args` binds values for the matched route from `-` (stdin), `@path`, or an inline object. The `invocation` object, the exact-name rule, argv-overrides-JSON, and the app-level fat-field pattern are in [Agent invocation](agent-invocation.md).

`--json-args` is not an endpoint. It does not appear in the `endpoints` array. Root help lists it next to `--capabilities`.

## Overriding Built-in Routes

There is no option to switch built-in routes off. The generator emits your own routes first and the built-in `--help`, `--version` and `--capabilities` handling after them, so a route you map with the same pattern takes precedence:

```csharp
NuruApp app = NuruApp.CreateBuilder()
    .Map("--version")
      .WithHandler(() => Console.WriteLine("MyApp 1.2.3 (custom)"))
      .AsQuery()
      .Done()
    .Build();
```

Two routes are different:

- `--check-updates` is opt-in. It exists only when you call `AddCheckUpdatesRoute()` (see below).
- `--interactive` / `-i` exists only when you call `AddRepl()`, and it is matched before your routes so a catch-all route cannot intercept it.

## Manual Feature Registration

You can manually add specific features using the builder, for example `AddCheckUpdatesRoute()`:

```csharp
NuruApp app = NuruApp.CreateBuilder()
    .Map("greet {name}")
      .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
      .AsQuery()
      .Done()
    .AddCheckUpdatesRoute()  // Manually add the check-updates route
    .Build();
```

See [Architecture Choices](../guides/architecture-choices.md) for more guidance.

## Related Documentation

- **[Agent invocation](agent-invocation.md)** - `--capabilities` then `--json-args`
- **[Auto-Help](auto-help.md)** - Help generation and customization
- **[REPL Key Bindings](repl-key-bindings.md)** - Interactive mode keyboard shortcuts
- **[Getting Started](../getting-started.md)** - Quick start guide
