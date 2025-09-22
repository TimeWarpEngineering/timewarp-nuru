#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru.Parsing;
using static System.Console;

// Enable diagnostic output
Environment.SetEnvironmentVariable("NURU_DEBUG", "true");

WriteLine("Testing parser with all routes from test suite:");
WriteLine();

// These are all the routes that should be registered in the test apps
string[] routes =
[
    // Basic Commands
    "status",
    "version",

    // Sub-Commands
    "git status",
    "git commit",
    "git push",

    // Option-Based Routing
    "git commit --amend",
    "git commit --amend --no-edit",

    // Options with Values
    "git commit --message {message}",
    "git commit -m {message}",
    "git log --max-count {count:int}",

    // Docker Pass-Through with Enhanced Features
    "docker run --enhance-logs {image}",
    "docker {*args}",

    // kubectl Enhancement
    "kubectl get {resource} --watch --enhanced",
    "kubectl get {resource} --watch",
    "kubectl get {resource}",
    "kubectl apply -f {file}",

    // npm with Options
    "npm install {package} --save-dev",
    "npm install {package} --save",
    "npm install {package}",
    "npm run {script}",

    // Async methods
    "async-test",

    // Optional Parameters
    "deploy {env} {tag?}",
    "backup {source} {destination?}",

    // Nullable Type Parameters
    "sleep {seconds:int?}",

    // Ultimate Catch-All
    "{*args}"
];

int passed = 0;
int failed = 0;

foreach (string route in routes)
{
    try
    {
        CompiledRoute parsed = RoutePatternParser.Parse(route);
        WriteLine($"✓ '{route}' - OK");

        // Show parsed route structure
        Write("  Positional: ");
        bool first = true;
        foreach (RouteMatcher matcher in parsed.PositionalMatchers)
        {
            if (!first) Write(", ");
            first = false;

            switch (matcher)
            {
                case LiteralMatcher lit:
                    Write($"Literal('{lit.Value}')");
                    break;
                case ParameterMatcher param:
                    Write($"Parameter('{param.Name}'");
                    if (param.Constraint is not null) Write($", type={param.Constraint}");
                    if (param.IsOptional) Write(", optional");
                    if (param.IsCatchAll) Write(", catchAll");
                    Write(")");
                    break;
            }
        }

        if (parsed.OptionMatchers.Count > 0)
        {
            Write(" | Options: ");
            first = true;
            foreach (OptionMatcher opt in parsed.OptionMatchers)
            {
                if (!first) Write(", ");
                first = false;
                Write($"Option('{opt.MatchPattern}'");
                if (opt.AlternateForm is not null) Write($", alt='{opt.AlternateForm}'");
                if (opt.ParameterName is not null)
                {
                    Write($", param='{opt.ParameterName}'");
                    if (opt.IsOptional) Write(", optional");
                }

                Write(");");
            }
        }

        WriteLine();
        passed++;
    }
    catch (Exception ex)
    {
        WriteLine($"✗ '{route}' - FAILED");
        WriteLine($"  Error: {ex.Message}");
        failed++;
    }

    WriteLine();
}

WriteLine($"\nSummary: {passed} passed, {failed} failed out of {routes.Length} routes");

// Now test some specific problematic patterns
WriteLine("\nTesting specific problematic commands:");

(string command, string description)[] testCommands =
[
    ("git commit --amend --no-edit", "Should parse two boolean options"),
    ("git commit -m \"Test message\" --amend", "Should parse short option with value and boolean option"),
    ("git log --max-count 5", "Should parse option with integer value"),
    ("npm install express --save-dev", "Should parse command with parameter and boolean option"),
];

foreach ((string command, string description) in testCommands)
{
    WriteLine($"\nTest: {description}");
    WriteLine($"Command: {command}");

    // First, split the command into parts (simulating command line parsing)
    List<string> parts = SplitCommand(command);
    WriteLine($"Parts: [{string.Join(", ", parts.Select(p => $"'{p}'"))}]");

    // Now we need to find which route pattern matches
    // This is what NuruApp would do internally
}

return failed == 0 ? 0 : 1;

// Simple command splitter that respects quotes
static List<string> SplitCommand(string command)
{
    var parts = new List<string>();
    string current = "";
    bool inQuotes = false;

    for (int i = 0; i < command.Length; i++)
    {
        char c = command[i];

        if (c == '"' && (i == 0 || command[i-1] != '\\'))
        {
            inQuotes = !inQuotes;
        }
        else if (c == ' ' && !inQuotes)
        {
            if (current.Length > 0)
            {
                parts.Add(current);
                current = "";
            }
        }
        else
        {
            current += c;
        }
    }

    if (current.Length > 0)
    {
        parts.Add(current);
    }

    return parts;
}