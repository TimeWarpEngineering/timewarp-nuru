#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Parsing;
using TimeWarp.Nuru.Parsing.Segments;
using static System.Console;

// Enable diagnostic output
Environment.SetEnvironmentVariable("NURU_DEBUG", "true");

WriteLine("Testing parser with all routes from test suite:");
WriteLine();

// These are all the routes that should be registered in the test apps
var routes = new[]
{
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
};

int passed = 0;
int failed = 0;

foreach (var route in routes)
{
    try
    {
        var parsed = RoutePatternParser.Parse(route);
        WriteLine($"✓ '{route}' - OK");
        
        // Show parsed segments
        Write("  Positional: ");
        bool first = true;
        foreach (var segment in parsed.PositionalTemplate)
        {
            if (!first) Write(", ");
            first = false;
            
            switch (segment)
            {
                case LiteralSegment lit:
                    Write($"Literal('{lit.Value}')");
                    break;
                case ParameterSegment param:
                    Write($"Parameter('{param.Name}'");
                    if (param.Constraint != null) Write($", type={param.Constraint}");
                    if (param.IsOptional) Write(", optional");
                    if (param.IsCatchAll) Write(", catchAll");
                    Write(")");
                    break;
            }
        }
        
        if (parsed.OptionSegments.Count > 0)
        {
            Write(" | Options: ");
            first = true;
            foreach (var opt in parsed.OptionSegments)
            {
                if (!first) Write(", ");
                first = false;
                Write($"Option('{opt.Name}'");
                if (opt.ShortAlias != null) Write($", alias='{opt.ShortAlias}'");
                if (opt.ExpectsValue) 
                {
                    Write($", param='{opt.ValueParameterName}'");
                }
                Write(")");
            }
        }
        
        if (parsed.HasCatchAll)
        {
            Write($" | CatchAll: '{parsed.CatchAllParameterName}'");
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

var testCommands = new[]
{
    ("git commit --amend --no-edit", "Should parse two boolean options"),
    ("git commit -m \"Test message\" --amend", "Should parse short option with value and boolean option"),
    ("git log --max-count 5", "Should parse option with integer value"),
    ("npm install express --save-dev", "Should parse command with parameter and boolean option"),
};

foreach (var (command, description) in testCommands)
{
    WriteLine($"\nTest: {description}");
    WriteLine($"Command: {command}");
    
    // First, split the command into parts (simulating command line parsing)
    var parts = SplitCommand(command);
    WriteLine($"Parts: [{string.Join(", ", parts.Select(p => $"'{p}'"))}]");
    
    // Now we need to find which route pattern matches
    // This is what NuruApp would do internally
}

return failed == 0 ? 0 : 1;

// Simple command splitter that respects quotes
List<string> SplitCommand(string command)
{
    var parts = new List<string>();
    var current = "";
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