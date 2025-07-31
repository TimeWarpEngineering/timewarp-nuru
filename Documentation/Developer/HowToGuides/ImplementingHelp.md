# Implementing Help Commands in TimeWarp.Nuru

This guide demonstrates various approaches to implementing help functionality in your TimeWarp.Nuru CLI applications, from simple manual help to sophisticated automatic help generation.

## Table of Contents
- [Overview](#overview)
- [Basic Help Implementation](#basic-help-implementation)
- [Contextual Help Commands](#contextual-help-commands)
- [Automatic Help Generation](#automatic-help-generation)
- [Advanced Help Patterns](#advanced-help-patterns)
- [Best Practices](#best-practices)

## Overview

TimeWarp.Nuru provides flexible options for implementing help functionality:

1. **Manual Help Routes** - Define custom help commands with full control over formatting
2. **Contextual Help** - Add help for specific commands (e.g., `add help`)
3. **Built-in Help Provider** - Use `RouteHelpProvider.GetHelpText()` for automatic help generation
4. **Standard Conventions** - Support `--help`, `-h`, and `help` patterns

## Basic Help Implementation

### Simple Help Command

The most straightforward approach is to add a dedicated help route:

```csharp
var builder = new NuruAppBuilder();

// Basic help command
builder.AddRoute("help", () => Console.WriteLine(
    "MyApp - A sample CLI application\n" +
    "\n" +
    "Commands:\n" +
    "  add <x> <y>       Add two numbers\n" +
    "  multiply <x> <y>  Multiply two numbers\n" +
    "  help              Show this help message"
));

// Also support --help convention
builder.AddRoute("--help", () => Console.WriteLine("Use 'help' for available commands"));
```

### Help with Descriptions

Enhance readability by adding descriptions when registering routes:

```csharp
builder.AddRoute("add {x:double} {y:double}", 
    (double x, double y) => Console.WriteLine($"{x} + {y} = {x + y}"),
    description: "Add two numbers together");

builder.AddRoute("multiply {x:double} {y:double}", 
    (double x, double y) => Console.WriteLine($"{x} × {y} = {x * y}"),
    description: "Multiply two numbers");
```

## Contextual Help Commands

Provide detailed help for specific commands:

```csharp
// Command-specific help
builder.AddRoute("deploy help", () => Console.WriteLine(
    "Usage: myapp deploy <environment> [options]\n" +
    "\n" +
    "Deploy application to the specified environment\n" +
    "\n" +
    "Arguments:\n" +
    "  environment    Target environment (dev, staging, prod)\n" +
    "\n" +
    "Options:\n" +
    "  --version      Version to deploy (default: latest)\n" +
    "  --dry-run      Preview changes without deploying\n" +
    "\n" +
    "Examples:\n" +
    "  myapp deploy prod\n" +
    "  myapp deploy staging --version 1.2.3\n" +
    "  myapp deploy dev --dry-run"
));

// The actual command
builder.AddRoute("deploy {env} --version {version?} --dry-run", 
    (string env, string? version) => DeployDryRun(env, version));
```

## Automatic Help Generation

### Using RouteHelpProvider

TimeWarp.Nuru includes a built-in `RouteHelpProvider` static class that generates help text from your registered routes:

```csharp
using TimeWarp.Nuru.Help;

var builder = new NuruAppBuilder();

// Register routes with descriptions
builder.AddRoute("status", ShowStatus, "Show application status");
builder.AddRoute("deploy {env}", Deploy, "Deploy to environment");
builder.AddRoute("config get {key}", GetConfig, "Get configuration value");
builder.AddRoute("config set {key} {value}", SetConfig, "Set configuration value");

// Add automatic help generation
builder.AddRoute("help", () =>
{
    var endpoints = builder.Build().Endpoints; // Note: In real code, store this reference
    Console.WriteLine(RouteHelpProvider.GetHelpText(endpoints));
});

// Or write to stderr
builder.AddRoute("--help", () =>
{
    var endpoints = builder.Build().Endpoints;
    Console.Error.WriteLine(RouteHelpProvider.GetHelpText(endpoints));
});
```

The `RouteHelpProvider.GetHelpText()` method:
- Returns help as a string, letting you decide how to output it
- Groups commands by prefix (e.g., all "config" commands together)
- Aligns descriptions for readability
- Handles route patterns with parameters

This gives you flexibility to:
- Write to Console.WriteLine or Console.Error
- Log the help text
- Return it from an API endpoint
- Write it to a file

### Injecting EndpointCollection for DI Scenarios

When using dependency injection, you can inject `EndpointCollection` directly:

```csharp
builder.AddDependencyInjection();

public class HelpHandler(EndpointCollection endpoints) : IRequestHandler<HelpCommand>
{
    public Task Handle(HelpCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine(RouteHelpProvider.GetHelpText(endpoints));
        return Task.CompletedTask;
    }
}

// Or get help as a string
public class HelpApiHandler(EndpointCollection endpoints) : IRequestHandler<HelpApiCommand, string>
{
    public Task<string> Handle(HelpApiCommand request, CancellationToken cancellationToken)
    {
        var helpText = RouteHelpProvider.GetHelpText(endpoints);
        return Task.FromResult(helpText);
    }
}
```

### Custom Help Formatting

For more control, create custom help formatting methods:

```csharp
public static class CustomHelpFormatter
{
    public static void ShowHelp(EndpointCollection endpoints)
    {
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║         MyApp CLI v1.0.0             ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.WriteLine();
        
        // Group by category (using custom logic)
        var grouped = endpoints.Endpoints
            .GroupBy(e => GetCategory(e.RoutePattern))
            .OrderBy(g => g.Key);
            
        foreach (var group in grouped)
        {
            Console.WriteLine($"[{group.Key}]");
            foreach (var endpoint in group.OrderBy(e => e.RoutePattern))
            {
                var pattern = FormatPattern(endpoint.RoutePattern);
                var desc = endpoint.Description ?? "No description";
                Console.WriteLine($"  {pattern,-30} {desc}");
            }
            Console.WriteLine();
        }
    }
    
    private static string GetCategory(string pattern)
    {
        // Custom categorization logic
        if (pattern.StartsWith("deploy")) return "Deployment";
        if (pattern.StartsWith("config")) return "Configuration";
        if (pattern.Contains("help") || pattern == "--help") return "Help";
        return "General";
    }
    
    private static string FormatPattern(string pattern)
    {
        // Custom formatting logic
        return pattern
            .Replace("{", "<")
            .Replace("}", ">")
            .Replace(":int", "")
            .Replace(":double", "")
            .Replace("?", " (optional)");
    }
}
```

## Advanced Help Patterns

### Mediator-Based Help Command

For applications using the Mediator pattern:

```csharp
public class HelpCommand : IRequest 
{
    public string? Topic { get; set; }
    
    public class Handler(EndpointCollection endpoints) : IRequestHandler<HelpCommand>
    {
        public Task Handle(HelpCommand request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(request.Topic))
            {
                ShowTopicHelp(request.Topic);
            }
            else
            {
                ShowGeneralHelp();
            }
            
            return Task.CompletedTask;
        }
        
        private void ShowTopicHelp(string topic)
        {
            // Show help for specific topic
            var relevantRoutes = endpoints.Endpoints
                .Where(e => e.RoutePattern.Contains(topic, StringComparison.OrdinalIgnoreCase))
                .ToList();
                
            if (relevantRoutes.Any())
            {
                Console.WriteLine($"Help for '{topic}':");
                foreach (var route in relevantRoutes)
                {
                    Console.WriteLine($"  {route.RoutePattern}");
                    if (!string.IsNullOrEmpty(route.Description))
                    {
                        Console.WriteLine($"    {route.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"No help available for '{topic}'");
            }
        }
        
        private void ShowGeneralHelp()
        {
            Console.WriteLine(RouteHelpProvider.GetHelpText(endpoints));
        }
    }
}

// Register the help command
builder.AddDependencyInjection();
builder.AddRoute<HelpCommand>("help {topic?}");
```

### Interactive Help System

Create an interactive help experience:

```csharp
builder.AddRoute("help --interactive", async () =>
{
    Console.WriteLine("Interactive Help - Type a command name or 'exit' to quit");
    
    while (true)
    {
        Console.Write("\nhelp> ");
        var input = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrEmpty(input) || input == "exit")
            break;
            
        // Find matching commands
        var matches = endpoints.Endpoints
            .Where(e => e.RoutePattern.Contains(input, StringComparison.OrdinalIgnoreCase))
            .ToList();
            
        if (matches.Count == 0)
        {
            Console.WriteLine($"No commands found matching '{input}'");
        }
        else if (matches.Count == 1)
        {
            ShowDetailedHelp(matches[0]);
        }
        else
        {
            Console.WriteLine($"Multiple matches for '{input}':");
            foreach (var match in matches)
            {
                Console.WriteLine($"  - {match.RoutePattern}");
            }
        }
    }
});
```

### Version and Help Integration

Combine version information with help:

```csharp
builder.AddRoute("--version", () => 
{
    Console.WriteLine("MyApp version 1.2.3");
    Console.WriteLine("Copyright (c) 2025 MyCompany");
});

builder.AddRoute("help", () =>
{
    Console.WriteLine("MyApp v1.2.3 - CLI Application");
    Console.WriteLine("Usage: myapp [command] [arguments] [options]");
    Console.WriteLine();
    
    // Show commands
    Console.WriteLine(RouteHelpProvider.GetHelpText(endpoints));
    
    Console.WriteLine("For more information, visit: https://myapp.com/docs");
});
```

## Best Practices

### 1. Support Multiple Help Patterns

Users expect different help invocation patterns:

```csharp
// Support all common help patterns
var helpAction = () => ShowHelp();

builder.AddRoute("help", helpAction);
builder.AddRoute("--help", helpAction);
builder.AddRoute("-h", helpAction);
builder.AddRoute("-?", helpAction);

// Also support help as a fallback
builder.AddRoute("{*args}", (string args) =>
{
    if (string.IsNullOrEmpty(args))
    {
        ShowHelp();
    }
    else
    {
        Console.WriteLine($"Unknown command: {args}");
        Console.WriteLine("Use 'help' to see available commands");
    }
});
```

### 2. Provide Meaningful Descriptions

Always include descriptions when registering routes:

```csharp
// Good: Includes description
builder.AddRoute("backup {path} --compress", BackupWithCompression, 
    "Create a compressed backup of the specified path");

// Better: Detailed description
builder.AddRoute("restore {backup} --target {path?}", Restore,
    "Restore from backup file. Target defaults to original location");
```

### 3. Group Related Commands

Organize commands logically:

```csharp
// Database commands
builder.AddRoute("db migrate", DbMigrate, "Run database migrations");
builder.AddRoute("db seed", DbSeed, "Seed the database");
builder.AddRoute("db reset", DbReset, "Reset database to initial state");

// User management
builder.AddRoute("user create {email}", CreateUser, "Create a new user");
builder.AddRoute("user list", ListUsers, "List all users");
builder.AddRoute("user delete {id}", DeleteUser, "Delete a user");
```

### 4. Include Examples in Help

Show practical usage examples:

```csharp
builder.AddRoute("help examples", () =>
{
    Console.WriteLine("Examples:");
    Console.WriteLine();
    Console.WriteLine("  # Deploy to production");
    Console.WriteLine("  myapp deploy prod --version 1.2.3");
    Console.WriteLine();
    Console.WriteLine("  # Backup with compression");
    Console.WriteLine("  myapp backup /data --compress");
    Console.WriteLine();
    Console.WriteLine("  # Configure API endpoint");
    Console.WriteLine("  myapp config set api.url https://api.example.com");
});
```

### 5. Handle Help Errors Gracefully

Provide helpful feedback for invalid help requests:

```csharp
builder.AddRoute("help {command}", (string command) =>
{
    var matches = endpoints.Endpoints
        .Where(e => e.RoutePattern.Contains(command, StringComparison.OrdinalIgnoreCase))
        .ToList();
        
    if (matches.Count == 0)
    {
        Console.WriteLine($"No help available for '{command}'");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        
        // Show command prefixes
        var prefixes = endpoints.Endpoints
            .Select(e => e.RoutePattern.Split(' ')[0])
            .Distinct()
            .OrderBy(p => p);
            
        foreach (var prefix in prefixes)
        {
            Console.WriteLine($"  {prefix}");
        }
    }
    else
    {
        // Show specific help
        foreach (var match in matches)
        {
            Console.WriteLine($"{match.RoutePattern}");
            if (!string.IsNullOrEmpty(match.Description))
            {
                Console.WriteLine($"  {match.Description}");
            }
        }
    }
});
```

### 6. Consider Output Formatting

Make help output easy to read:

```csharp
public static class HelpFormatter
{
    public static void ShowCommandHelp(string command, string description, 
        string[] arguments, (string name, string desc)[] options)
    {
        Console.WriteLine($"\n{command}");
        Console.WriteLine(new string('-', command.Length));
        Console.WriteLine(description);
        
        if (arguments.Length > 0)
        {
            Console.WriteLine("\nArguments:");
            foreach (var arg in arguments)
            {
                Console.WriteLine($"  {arg}");
            }
        }
        
        if (options.Length > 0)
        {
            Console.WriteLine("\nOptions:");
            foreach (var (name, desc) in options)
            {
                Console.WriteLine($"  {name,-20} {desc}");
            }
        }
        
        Console.WriteLine();
    }
}
```

## Summary

TimeWarp.Nuru provides flexible help implementation options:

- **Manual Help**: Full control over formatting and content
- **Automatic Generation**: Use `RouteHelpProvider.GetHelpText()` for consistent help output
- **Contextual Help**: Provide detailed help for specific commands
- **Mixed Approaches**: Combine automatic and manual help for best results

Choose the approach that best fits your application's needs and user expectations. For simple CLIs, manual help may suffice. For complex applications with many commands, leverage automatic help generation with custom enhancements where needed.