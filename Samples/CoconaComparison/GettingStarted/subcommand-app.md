# SubCommandApp: Cocona vs Nuru Comparison

This document compares the implementation of a CLI application with sub-commands between Cocona and Nuru frameworks.

## Overview

The SubCommandApp demonstrates:
- Top-level commands with options and arguments
- Sub-commands (nested commands)
- Sub-sub-commands (deeply nested)
- Primary/default commands
- Enum parameter support
- Shell completion support

Nuru provides two approaches:
1. **Delegate-based** (`subcommand-app`) - Direct lambda expressions for maximum performance
2. **Class-based with DI** (`subcommand-app-di`) - Mediator pattern with dependency injection, closer to Cocona's model

## Side-by-Side Comparison

### Cocona Implementation

```csharp
[HasSubCommands(typeof(SubCommands), Description = "Nested sub-commands")]
class Program
{
    static void Main(string[] args)
    {
        CoconaApp.Run<Program>(args, options =>
        {
            options.EnableShellCompletionSupport = true;
        });
    }

    [Command(Description = "Say hello")]
    public void Hello([Option('u', Description = "Print a name converted to upper-case.")]bool toUpperCase, 
                      [Argument(Description = "Your name")]string name)
    {
        Console.WriteLine($"Hello {(toUpperCase ? name.ToUpper() : name)}!");
    }

    [Command(Description = "Say goodbye")]
    public void Bye([Option('l', Description = "Print a name converted to lower-case.")]bool toLowerCase, 
                    [Argument(Description = "Your name")]string name)
    {
        Console.WriteLine($"Goodbye {(toLowerCase ? name.ToLower() : name)}!");
    }
}

[HasSubCommands(typeof(SubSubCommands))]
class SubCommands
{
    public enum Member { Alice, Karen }

    public void Konnichiwa(Member member)
    {
        Console.WriteLine($"Konnichiwa! {member}");
    }

    public void Hello()
    {
        Console.WriteLine("Hello!");
    }
}

class SubSubCommands
{
    public void Foobar()
    {
        Console.WriteLine("Foobar!");
    }

    [PrimaryCommand]
    public void Primary(string value)
    {
        Console.WriteLine($"value={value}");
    }
}
```

### Nuru Implementation (Delegate)

```csharp
var app = new NuruAppBuilder()
    // Top-level commands
    .AddRoute("hello {name|Your name} --to-upper-case,-u|Print a name converted to upper-case {toUpperCase:bool}", 
        (string name, bool toUpperCase) => 
        {
            WriteLine($"Hello {(toUpperCase ? name.ToUpper() : name)}!");
        },
        description: "Say hello")
    .AddRoute("bye {name|Your name} --to-lower-case,-l|Print a name converted to lower-case {toLowerCase:bool}", 
        (string name, bool toLowerCase) => 
        {
            WriteLine($"Goodbye {(toLowerCase ? name.ToLower() : name)}!");
        },
        description: "Say goodbye")
    
    // Sub-commands
    .AddRoute("sub-commands konnichiwa {member}", 
        (Member member) => 
        {
            WriteLine($"Konnichiwa! {member}");
        },
        description: "Say konnichiwa to a member")
    .AddRoute("sub-commands hello", 
        () => WriteLine("Hello!"),
        description: "Say hello from sub-commands")
    
    // Sub-sub-commands
    .AddRoute("sub-commands sub-sub-commands foobar", 
        () => WriteLine("Foobar!"),
        description: "Execute foobar")
    .AddRoute("sub-commands sub-sub-commands {value:string}", 
        (string value) => WriteLine($"value={value}"),
        description: "Primary command with value")
    
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);
```

### Nuru Implementation (Class-based with DI)

```csharp
var app = new NuruAppBuilder()
    .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(HelloCommand).Assembly))
    
    // Register commands with routes
    .AddRoute<HelloCommand>("hello {name|Your name} --to-upper-case,-u|Print a name converted to upper-case {toUpperCase:bool}")
    .AddRoute<ByeCommand>("bye {name|Your name} --to-lower-case,-l|Print a name converted to lower-case {toLowerCase:bool}")
    .AddRoute<KonnichiwaCommand>("sub-commands konnichiwa {member}")
    .AddRoute<SubHelloCommand>("sub-commands hello")
    .AddRoute<FoobarCommand>("sub-commands sub-sub-commands foobar")
    .AddRoute<PrimaryCommand>("sub-commands sub-sub-commands {value:string}")
    
    .AddAutoHelp()
    .Build();

// Command classes follow Mediator pattern
public class HelloCommand : IRequest
{
    public string Name { get; set; }
    public bool ToUpperCase { get; set; }
    
    public class Handler : IRequestHandler<HelloCommand>
    {
        public async Task Handle(HelloCommand request, CancellationToken cancellationToken)
        {
            WriteLine($"Hello {(request.ToUpperCase ? request.Name.ToUpper() : request.Name)}!");
        }
    }
}
```

## Key Differences

### 1. Sub-Command Organization
- **Cocona**: Uses `[HasSubCommands]` attribute to link classes hierarchically
- **Nuru (Both)**: Uses route patterns with command prefixes (e.g., "sub-commands konnichiwa")

### 2. Command Hierarchy
- **Cocona**: Nested class structure reflects command hierarchy
- **Nuru (Delegate)**: Flat list of routes with hierarchical patterns
- **Nuru (Class-based)**: Flat command classes with hierarchical route registration

### 3. Primary/Default Commands
- **Cocona**: `[PrimaryCommand]` attribute designates default handler
- **Nuru (Both)**: Route with parameter captures unmatched input (e.g., `{value:string}`)

### 4. Shell Completion
- **Cocona**: `options.EnableShellCompletionSupport = true` in configuration
- **Nuru**: Shell completion support is planned but not yet implemented

### 5. Enum Support
- **Cocona**: Direct enum parameter support in methods
- **Nuru (Both)**: Full enum support with automatic case-insensitive parsing (as of recent update)

### 6. Short Option Aliases
- **Cocona**: Single character in `[Option('u')]` attribute
- **Nuru (Both)**: Comma syntax in route pattern: `--to-upper-case,-u`

## Usage Examples

All implementations support the same command-line interface:

```bash
# Top-level commands
./app hello Alice --to-upper-case true
./app hello Alice -u true
./app bye Bob --to-lower-case true
./app bye Bob -l true

# Sub-commands
./app sub-commands konnichiwa Alice
./app sub-commands hello

# Sub-sub-commands
./app sub-commands sub-sub-commands foobar
./app sub-commands sub-sub-commands "some value"

# Help
./app --help
./app hello --help
./app sub-commands --help
```

## Architecture Comparison

### Cocona
- Object-oriented with attribute-based metadata
- Hierarchical class structure mirrors command structure
- Reflection-based command discovery
- Built-in shell completion support

### Nuru (Delegate)
- Functional approach with inline lambdas
- Route patterns define hierarchy
- Direct execution without reflection
- Minimal memory footprint

### Nuru (Class-based)
- Mediator pattern with command/handler separation
- Full dependency injection support
- Better for complex business logic and testing
- Similar structure to Cocona but with explicit routing

## Performance Considerations

- **Cocona**: Reflection overhead for command discovery and execution
- **Nuru (Delegate)**: Minimal overhead, direct delegate invocation
- **Nuru (Class-based)**: Mediator overhead but optimized execution

## Migration Notes

When migrating from Cocona to Nuru:

1. **Sub-command structure**: Replace `[HasSubCommands]` with route prefixes
2. **Primary commands**: Replace `[PrimaryCommand]` with catch-all route parameters
3. **Options**: Convert `[Option('x')]` to route pattern `--option,-x`
4. **Shell completion**: Note that this feature is not yet available in Nuru
5. **Choose your approach**:
   - Use delegate version for simple commands and maximum performance
   - Use class-based version for complex logic, DI needs, and better testability