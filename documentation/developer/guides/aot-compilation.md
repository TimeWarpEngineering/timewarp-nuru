# Native AOT Compilation Guide

TimeWarp.Nuru supports Native AOT compilation for creating fast, self-contained executables. This guide explains how to use TimeWarp.Nuru with AOT and the considerations for each routing approach.

## Quick Start

### Direct Delegate Routing (Full AOT Support)

Direct delegate routing works seamlessly with Native AOT:

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@2.1.0-beta.17

using TimeWarp.Nuru;

NuruApp app = new NuruAppBuilder()
    .AddAutoHelp()
    .AddRoute("greet {name}", (string name) =>
        Console.WriteLine($"Hello, {name}!"))
    .AddRoute("status", () => "System is operational")
    .Build();

return await app.RunAsync(args);
```

**Publish as AOT:**
```bash
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
```

### Mediator Pattern Routing (Partial Trim Mode)

Mediator pattern routing requires partial trim mode due to reflection:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <TrimMode>partial</TrimMode>
</PropertyGroup>
```

## AOT Compatibility Matrix

| Feature | AOT Support | Notes |
|---------|-------------|-------|
| Direct delegate routing | ✅ Full | No additional configuration needed |
| Mediator pattern routing | ⚠️ Partial | Requires `TrimMode=partial` |
| Type converters (built-in) | ✅ Full | int, bool, DateTime, etc. |
| Custom type converters | ✅ Full | Register before building app |
| Enum parameters | ✅ Full | Enum values preserved automatically |
| Array/catch-all parameters | ✅ Full | Works with both routing approaches |
| Options (--flag, -f) | ✅ Full | Works with both routing approaches |
| JSON serialization | ✅ Full | Uses source generators |

## Understanding AOT Warnings

TimeWarp.Nuru uses reflection internally for parameter binding and command routing. The library includes proper AOT annotations to guide the trimmer, but you may see warnings when building your application.

### Common Warnings

#### IL2026: RequiresUnreferencedCode
**What it means:** The code uses reflection to access types/members that might be trimmed.

**When you see it:** Building apps using mediator pattern or advanced reflection scenarios.

**How to fix:**
```csharp
// Option 1: Add DynamicDependency attribute to preserve types
[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(YourCommandType))]
class Program { ... }

// Option 2: Use TrimMode=partial (keeps all assemblies)
<TrimMode>partial</TrimMode>
```

#### IL3050: RequiresDynamicCode
**What it means:** The code may generate IL at runtime (e.g., delegate invocation).

**When you see it:** Using direct delegate routing with complex parameter types.

**How to fix:** This is usually safe to ignore. The warning is for transparency. If it causes issues, consider simplifying parameter types.

## Best Practices

### 1. Choose the Right Routing Approach

**Use Direct Delegates when:**
- Building minimal, fast CLI tools
- You want the smallest possible executable
- You don't need dependency injection
- Performance is critical

**Use Mediator Pattern when:**
- You need enterprise patterns (DI, validation, logging)
- Your commands have complex business logic
- You want testability and separation of concerns
- Executable size is less critical

### 2. Preserve Your Types

When using mediator pattern, ensure your command types are preserved:

```csharp
// Your command class
public class DeployCommand : IRequest
{
    public string Environment { get; set; } = string.Empty;
    public bool DryRun { get; set; }
}

// Preserve it for AOT
[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors |
                   DynamicallyAccessedMemberTypes.PublicProperties,
                   typeof(DeployCommand))]
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Register route
        var app = new NuruAppBuilder()
            .AddMediatorRoute<DeployCommand>("deploy {environment} --dry-run?")
            .BuildWithServices(services => services.AddMediator());

        return await app.RunAsync(args);
    }
}
```

### 3. Test Your AOT Build

Always test your AOT-compiled executable:

```bash
# Build with AOT
dotnet publish -c Release -r linux-x64 -p:PublishAot=true

# Test the executable
./bin/Release/net10.0/linux-x64/publish/your-app --help
./bin/Release/net10.0/linux-x64/publish/your-app greet World
```

### 4. Use Source-Generated JSON Serialization

If your commands return complex objects, create a source-generated JSON context:

```csharp
[JsonSerializable(typeof(YourResponseType))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class AppJsonContext : JsonSerializerContext { }

// Use it when serializing
string json = JsonSerializer.Serialize(response, AppJsonContext.Default.YourResponseType);
```

## Example: AOT-Compatible App

Here's a complete example that works with full Native AOT:

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@2.1.0-beta.17

using TimeWarp.Nuru;

NuruApp app = new NuruAppBuilder()
    .AddAutoHelp()
    .AddRoute("add {x:int} {y:int}", Add, "Add two numbers")
    .AddRoute("greet {name}", Greet, "Greet someone")
    .AddRoute("version", () => "1.0.0", "Show version")
    .Build();

return await app.RunAsync(args);

int Add(int x, int y)
{
    Console.WriteLine($"{x} + {y} = {x + y}");
    return x + y;
}

void Greet(string name)
{
    Console.WriteLine($"Hello, {name}!");
}
```

**Publish:**
```bash
chmod +x app.cs
dotnet publish app.cs -c Release -r linux-x64 -p:PublishAot=true -o ./publish
./publish/app add 5 3
./publish/app greet Alice
```

## Performance Characteristics

### Binary Size
- **Direct delegates (full AOT):** ~3-8 MB (depending on dependencies)
- **Mediator pattern (partial trim):** ~15-25 MB (includes all assemblies)

### Startup Time
- **Direct delegates:** <5ms cold start
- **Mediator pattern:** ~50-100ms cold start (includes DI container initialization)

### Memory Footprint
- **Direct delegates:** ~4KB heap allocations per command
- **Mediator pattern:** ~50-100KB (includes DI overhead)

## Troubleshooting

### Problem: "IL2104: Assembly produced trim warnings"

**Cause:** The trimmer can't statically determine which types are needed.

**Solution:** Use `TrimMode=partial` or add `<TrimmerRootAssembly>` for problematic assemblies:
```xml
<ItemGroup>
  <TrimmerRootAssembly Include="YourApp" />
</ItemGroup>
```

### Problem: "MissingMethodException at runtime"

**Cause:** A required method/constructor was trimmed.

**Solution:** Add `[DynamicDependency]` attributes or use `[DynamicallyAccessedMembers]`:
```csharp
[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ProblematicType))]
```

### Problem: "NotSupportedException: Dynamic code generation not supported"

**Cause:** Trying to use features that require runtime code generation (rare with TimeWarp.Nuru).

**Solution:** Avoid expression trees, dynamic types, and runtime compilation. Stick to concrete types.

## Further Reading

- [.NET Native AOT Documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Prepare Libraries for Trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)
- [Introduction to AOT Warnings](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/fixing-warnings)
