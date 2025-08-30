namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;

internal sealed class GetSyntaxTool
{
    private static readonly Dictionary<string, string> SyntaxSections = new()
    {
        ["literals"] = """
            ## Literal Segments

            Literal segments are plain text that must match exactly:

            ```csharp
            .AddRoute("status", () => Console.WriteLine("OK"))
            .AddRoute("git commit", () => Console.WriteLine("Committing..."))
            ```
            """,

        ["parameters"] = """
            ## Parameters

            Parameters are defined using curly braces `{}` and capture values from the command line:

            ```csharp
            // Basic parameter
            .AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello {name}"))

            // Multiple parameters
            .AddRoute("copy {source} {destination}", (string source, string dest) => ...)
            ```
            """,

        ["types"] = """
            ## Parameter Types

            Parameters can have type constraints using a colon `:` followed by the type:

            ```csharp
            .AddRoute("delay {ms:int}", (int milliseconds) => ...)
            .AddRoute("price {amount:double}", (double amount) => ...)
            .AddRoute("schedule {date:DateTime}", (DateTime date) => ...)
            ```

            Supported types:
            - `string` (default if no type specified)
            - `int`, `long`, `short`, `byte`
            - `double`, `float`, `decimal`
            - `bool`
            - `DateTime`, `DateOnly`, `TimeOnly`
            - `Guid`
            """,

        ["optional"] = """
            ## Optional Parameters

            Parameters can be made optional by adding `?` after the name:

            ```csharp
            .AddRoute("deploy {env} {tag?}", (string env, string? tag) => ...)
            ```

            Optional parameters can also have type constraints:

            ```csharp
            .AddRoute("wait {seconds:int?}", (int? seconds) => ...)
            .AddRoute("backup {source} {destination?}", (string source, string? destination) => ...)
            ```
            """,

        ["catchall"] = """
            ## Catch-all Parameters

            Use `*` prefix for catch-all parameters that capture all remaining arguments:

            ```csharp
            .AddRoute("docker {*args}", (string[] args) => ...)
            ```

            Catch-all parameters must be the last parameter in the route pattern.
            """,

        ["options"] = """
            ## Options

            Options start with `--` (long form) or `-` (short form):

            ```csharp
            // Boolean option
            .AddRoute("build --verbose", (bool verbose) => ...)

            // Option with value
            .AddRoute("build --config {mode}", (string mode) => ...)

            // Short form
            .AddRoute("build -c {mode}", (string mode) => ...)

            // Multiple options
            .AddRoute("deploy {env} --dry-run --force", (string env, bool dryRun, bool force) => ...)
            ```
            """,

        ["descriptions"] = """
            ## Descriptions

            ### Parameter Descriptions

            Add descriptions to parameters using the pipe `|` character:

            ```csharp
            .AddRoute("deploy {env|Target environment (dev, staging, prod)}",
                (string env) => ...)

            .AddRoute("copy {source|Source file path} {dest|Destination path}",
                (string source, string dest) => ...)
            ```

            ### Option Descriptions

            Options can have descriptions and short aliases:

            ```csharp
            // Option with description
            .AddRoute("build --verbose|Show detailed output",
                (bool verbose) => ...)

            // Option with short alias and description
            .AddRoute("build --config,-c|Build configuration mode",
                (string config) => ...)
            ```
            """,

        ["all"] = """
            # Route Pattern Syntax Reference

            ## Quick Reference

            - **Literals**: `status`, `git commit`
            - **Parameters**: `{name}`, `{id:int}`
            - **Optional**: `{tag?}`, `{count:int?}`
            - **Catch-all**: `{*args}`
            - **Options**: `--verbose`, `-v`, `--config {mode}`
            - **Descriptions**: `{env|Environment name}`, `--force|Skip confirmations`

            ## Common Patterns

            ```csharp
            // Simple command
            .AddRoute("version", () => Console.WriteLine("1.0.0"))

            // Command with required parameter
            .AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello {name}"))

            // Command with optional parameter
            .AddRoute("deploy {env} {tag?}", (string env, string? tag) => Deploy(env, tag))

            // Command with typed parameter
            .AddRoute("wait {seconds:int}", (int seconds) => Thread.Sleep(seconds * 1000))

            // Command with options
            .AddRoute("build --verbose --config {mode}", (bool verbose, string mode) => Build(verbose, mode))

            // Command with catch-all
            .AddRoute("docker {*args}", (string[] args) => Docker(args))

            // Complex command with descriptions
            .AddRoute("test {project|Project name} --verbose,-v|Show detailed output --filter {pattern|Test filter}",
                (string project, bool verbose, string? pattern) => RunTests(project, verbose, pattern))
            ```
            """
    };

    [McpServerTool]
    [Description("Get TimeWarp.Nuru route pattern syntax documentation")]
    public static string GetSyntax(
        [Description("Syntax element (literals, parameters, types, optional, catchall, options, descriptions, all)")] string element = "all")
    {
        string normalizedElement = element.ToLowerInvariant().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        if (SyntaxSections.TryGetValue(normalizedElement, out string? section))
        {
            return section;
        }

        // Try to find partial matches
        KeyValuePair<string, string> match = SyntaxSections
            .FirstOrDefault(kvp => kvp.Key.Contains(normalizedElement, StringComparison.Ordinal) || normalizedElement.Contains(kvp.Key, StringComparison.Ordinal));

        if (!match.Equals(default(KeyValuePair<string, string>)))
        {
            return match.Value;
        }

        return $"Unknown syntax element '{element}'. Available elements:\n" +
               string.Join("\n", SyntaxSections.Keys.Select(k => $"- {k}"));
    }

    [McpServerTool]
    [Description("Get examples of specific route pattern features")]
    public static string GetPatternExamples(
        [Description("Pattern feature (basic, typed, optional, catchall, options, complex)")] string feature = "basic")
    {
        return feature.ToLowerInvariant() switch
        {
            "basic" => """
                ## Basic Route Patterns

                ```csharp
                // Simple literal route
                .AddRoute("status", () => Console.WriteLine("OK"))

                // Route with parameter
                .AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello {name}"))

                // Multiple segments
                .AddRoute("git commit", () => Console.WriteLine("Committing..."))

                // Multiple parameters
                .AddRoute("copy {source} {dest}", (string source, string dest) => Copy(source, dest))
                ```
                """,

            "typed" => """
                ## Typed Parameters

                ```csharp
                // Integer parameter
                .AddRoute("delay {ms:int}", (int ms) => Thread.Sleep(ms))

                // Double parameter
                .AddRoute("price {amount:double}", (double amount) => ShowPrice(amount))

                // DateTime parameter
                .AddRoute("schedule {date:DateTime}", (DateTime date) => Schedule(date))

                // Boolean parameter (usually as option)
                .AddRoute("build --release {isRelease:bool}", (bool isRelease) => Build(isRelease))

                // Guid parameter
                .AddRoute("user {id:Guid}", (Guid id) => GetUser(id))
                ```
                """,

            "optional" => """
                ## Optional Parameters

                ```csharp
                // Optional string parameter
                .AddRoute("deploy {env} {tag?}", (string env, string? tag) =>
                    Deploy(env, tag ?? "latest"))

                // Optional typed parameter
                .AddRoute("wait {seconds:int?}", (int? seconds) =>
                    Thread.Sleep((seconds ?? 5) * 1000))

                // Multiple optional parameters
                .AddRoute("backup {source} {dest?} {format?}",
                    (string source, string? dest, string? format) =>
                    Backup(source, dest ?? "./backup", format ?? "zip"))
                ```
                """,

            "catchall" => """
                ## Catch-all Parameters

                ```csharp
                // Capture all remaining arguments
                .AddRoute("docker {*args}", (string[] args) =>
                    Docker(string.Join(" ", args)))

                // Catch-all with preceding parameters
                .AddRoute("exec {command} {*args}", (string command, string[] args) =>
                    Execute(command, args))

                // Common use case: pass-through commands
                .AddRoute("npm {*args}", (string[] args) =>
                    RunNpm(args))
                ```
                """,

            "options" => """
                ## Options and Flags

                ```csharp
                // Boolean flag
                .AddRoute("build --verbose", (bool verbose) =>
                    Build(verbose))

                // Option with value
                .AddRoute("build --config {mode}", (string mode) =>
                    Build(mode))

                // Short form
                .AddRoute("test -v", (bool verbose) =>
                    RunTests(verbose))

                // Multiple options
                .AddRoute("deploy {env} --dry-run --force --tag {version}",
                    (string env, bool dryRun, bool force, string version) =>
                    Deploy(env, dryRun, force, version))

                // Options with aliases
                .AddRoute("build --verbose,-v --output,-o {path}",
                    (bool verbose, string path) =>
                    Build(verbose, path))
                ```
                """,

            "complex" => """
                ## Complex Route Patterns

                ```csharp
                // Full-featured command with descriptions
                .AddRoute(
                    "deploy {env|Target environment} " +
                    "{version?|Version to deploy} " +
                    "--dry-run,-d|Preview without deploying " +
                    "--force,-f|Skip confirmations " +
                    "--config {file|Configuration file path}",
                    (string env, string? version, bool dryRun, bool force, string? file) =>
                    {
                        Console.WriteLine($"Deploying to {env}");
                        Console.WriteLine($"Version: {version ?? "latest"}");
                        Console.WriteLine($"Dry run: {dryRun}");
                        Console.WriteLine($"Force: {force}");
                        Console.WriteLine($"Config: {file ?? "default.json"}");
                    })

                // Command with mixed parameter types
                .AddRoute(
                    "benchmark {iterations:int} " +
                    "{timeout:double?} " +
                    "--parallel,-p " +
                    "--threads {count:int} " +
                    "{*additional}",
                    (int iterations, double? timeout, bool parallel, int? count, string[] additional) =>
                        RunBenchmark(iterations, timeout, parallel, count, additional))
                ```
                """,

            _ => $"Unknown feature '{feature}'. Available features: basic, typed, optional, catchall, options, complex"
        };
    }
}
