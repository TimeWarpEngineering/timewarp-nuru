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

            // Repeated options (collect multiple values)
            .AddRoute("docker build --build-arg {args}*", (string[] args) => ...)
            // Matches: docker build --build-arg KEY1=val1 --build-arg KEY2=val2
            // args = ["KEY1=val1", "KEY2=val2"]
            ```
            """,

        ["separator"] = """
            ## End-of-Options Separator (--)

            The double dash `--` serves as an end-of-options marker (POSIX standard) that signals
            all following arguments should be treated as positional parameters:

            ```csharp
            // Pass remaining args to command (prevents option interpretation)
            .AddRoute("exec {cmd} -- {*args}", (string cmd, string[] args) => Shell.Run(cmd, args))
            // Matches: exec npm -- run build --watch
            // cmd = "npm", args = ["run", "build", "--watch"]

            // Docker exec pattern
            .AddRoute("docker exec {container} -- {*cmd}",
                (string container, string[] cmd) => Docker.Exec(container, cmd))
            // Matches: docker exec web -- npm test --coverage

            // Combined with repeated options
            .AddRoute("exec --env {e}* -- {*cmd}", (string[] e, string[] cmd) => ...)
            // Matches: exec --env PATH=/bin --env USER=root -- ls -la
            ```

            Rules:
            - `--` must be followed by a catch-all parameter
            - No options can appear after `--`
            - Arguments after `--` preserve their literal form (dashes not interpreted)
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
            - **Repeated Options**: `--tag {t}*` (can be used multiple times)
            - **End-of-Options**: `-- {*args}` (stop option processing)
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
        [Description("Pattern feature (basic, typed, optional, catchall, options, repeated, separator, complex)")] string feature = "basic")
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

            "repeated" => """
                ## Repeated Option Patterns

                Repeated options allow collecting multiple values from the same option:

                ```csharp
                // Docker build with multiple build args
                .AddRoute("docker build --build-arg {args}* {path}",
                    (string[] args, string path) => DockerBuild(args, path))
                // Matches: docker build --build-arg KEY1=val1 --build-arg KEY2=val2 .

                // Curl with multiple headers
                .AddRoute("curl {url} --header {headers}*",
                    (string url, string[] headers) => Curl(url, headers))
                // Matches: curl api.com --header "Accept: json" --header "Auth: Bearer token"

                // Combined with other options
                .AddRoute("kubectl apply -f {file} --label {labels}* --dry-run?",
                    (string file, string[] labels, bool dryRun) => KubectlApply(file, labels, dryRun))
                // Matches: kubectl apply -f app.yaml --label env=prod --label app=web --dry-run
                ```
                """,

            "separator" => """
                ## End-of-Options Separator (--)

                The -- separator stops option processing, treating everything after as positional arguments:

                ```csharp
                // Execute command with arguments that might start with dashes
                .AddRoute("exec {cmd} -- {*args}",
                    (string cmd, string[] args) => Shell.Run(cmd, args))
                // Matches: exec npm -- run build --watch
                // cmd = "npm", args = ["run", "build", "--watch"]

                // Docker exec with command
                .AddRoute("docker exec {container} -- {*cmd}",
                    (string container, string[] cmd) => DockerExec(container, cmd))
                // Matches: docker exec web -- ls -la /app

                // Combined with options
                .AddRoute("run --env {vars}* -- {*cmd}",
                    (string[] vars, string[] cmd) => RunWithEnv(vars, cmd))
                // Matches: run --env PATH=/bin --env USER=root -- python -m server
                ```
                """,

            _ => $"Unknown feature '{feature}'. Available features: basic, typed, optional, catchall, options, repeated, separator, complex"
        };
    }
}
