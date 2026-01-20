# Supported Types

Complete reference of supported parameter types in TimeWarp.Nuru route patterns.

## Built-In Type Converters

TimeWarp.Nuru includes converters for common .NET types:

| Type Syntax | C# Type | Example Pattern | Example Value |
|-------------|---------|-----------------|---------------|
| `string` (default) | `string` | `{name}` or `{name:string}` | `"Alice"` |
| `int` | `Int32` | `{count:int}` | `42`, `-10` |
| `double` | `Double` | `{price:double}` | `3.14`, `-2.5` |
| `bool` | `Boolean` | `{enabled:bool}` | `true`, `false` |
| `DateTime` | `DateTime` | `{date:DateTime}` | `2025-01-15` |
| `Guid` | `Guid` | `{id:Guid}` | `123e4567-e89b-...` |
| `long` | `Int64` | `{size:long}` | `9223372036854775807` |
| `decimal` | `Decimal` | `{amount:decimal}` | `999.99` |
| `TimeSpan` | `TimeSpan` | `{duration:TimeSpan}` | `1:30:00`, `00:05:00` |
| `uri` | `Uri` | `{url:uri}` | `https://example.com` |

## Type Usage Examples

### String (Default)

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  // Type annotation optional for strings
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done()
  .Map("echo {message:string}")
    .WithHandler((string msg) => Console.WriteLine(msg))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli greet Alice
./cli echo "Hello World"
```

### Integer

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("wait {seconds:int}")
    .WithHandler((int sec) => Thread.Sleep(sec * 1000))
    .AsCommand()
    .Done()
  .Map("repeat {times:int} {text}")
    .WithHandler((int times, string text) =>
    {
      for (int i = 0; i < times; i++)
        Console.WriteLine(text);
    })
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli wait 5
./cli repeat 3 "Hello"
```

### Double

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("calc {x:double} {y:double}")
    .WithHandler((double x, double y) => Console.WriteLine(x + y))
    .AsCommand()
    .Done()
  .Map("scale {factor:double}")
    .WithHandler((double f) => Scale(f))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli calc 3.14 2.86
./cli scale 1.5
```

### Boolean

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("set {key} {value:bool}")
    .WithHandler((string key, bool value) => Config.Set(key, value))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli set debug true
./cli set verbose false
```

### DateTime

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("schedule {when:DateTime}")
    .WithHandler((DateTime dt) => Console.WriteLine($"Scheduled for {dt}"))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli schedule 2025-12-25
./cli schedule "2025-01-15 14:30:00"
```

### Guid

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("get {id:Guid}")
    .WithHandler((Guid id) => GetRecord(id))
    .AsCommand()
    .Done()
  .Map("delete {userId:Guid}")
    .WithHandler((Guid userId) => DeleteUser(userId))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli get 123e4567-e89b-12d3-a456-426614174000
```

### Long

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("allocate {bytes:long}")
    .WithHandler((long bytes) => Allocate(bytes))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli allocate 9223372036854775807
```

### Decimal

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("pay {amount:decimal}")
    .WithHandler((decimal amt) => ProcessPayment(amt))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli pay 999.99
```

### TimeSpan

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("timeout {duration:TimeSpan}")
    .WithHandler((TimeSpan ts) => SetTimeout(ts))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli timeout 00:05:00      # 5 minutes
./cli timeout 1:30:00       # 1 hour 30 minutes
```

### Uri

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("download {url:uri}")
    .WithHandler((Uri url) => Download(url))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli download https://example.com/file.zip
```

## Nullable Types

Use nullable types for optional parameters:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("deploy {env} {version?}")
    .WithHandler((string env, string? version) =>
    {
      Console.WriteLine($"Deploying to {env}");
      if (version != null)
        Console.WriteLine($"Version: {version}");
    })
    .AsCommand()
    .Done()
  .Map("wait {seconds:int?}")
    .WithHandler((int? seconds) =>
    {
      int delay = seconds ?? 5;  // Default to 5
      Thread.Sleep(delay * 1000);
    })
    .AsCommand()
    .Done()
  .Build();
```

## Arrays (Catch-All)

Use `string[]` for catch-all parameters:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("echo {*words}")
    .WithHandler((string[] words) => Console.WriteLine(string.Join(" ", words)))
    .AsCommand()
    .Done()
  .Map("add {*files}")
    .WithHandler((string[] files) =>
    {
      foreach (string file in files)
        ProcessFile(file);
    })
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli echo Hello World from Nuru
./cli add file1.txt file2.txt file3.txt
```

## Type Conversion Errors

When conversion fails, users get clear error messages and exit code 1:

```bash
./cli wait abc
# Error: Invalid value 'abc' for parameter 'seconds'. Expected: int

./cli schedule invalid-date
# Error: Invalid value 'invalid-date' for parameter 'when'. Expected: DateTime
```

**Note:** Type conversion errors are binding failures, not matching failures. The route matched, but the value couldn't be converted to the expected type. This gives users actionable feedback rather than a generic "unknown command" message.

## Custom Type Converters

You can add custom type converters for your own types by implementing `IRouteTypeConverter`:

```csharp
public interface IRouteTypeConverter
{
  Type TargetType { get; }
  string? ConstraintAlias { get; }  // Optional short name for type constraint
  bool TryConvert(string value, out object? result);
}
```

### Example: Email Address Converter

```csharp
public class EmailAddressConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(EmailAddress);
  public string? ConstraintAlias => "email";  // Allows {param:email}

  public bool TryConvert(string value, out object? result)
  {
    if (EmailAddress.TryParse(value, out var email))
    {
      result = email;
      return true;
    }
    result = null;
    return false;
  }
}
```

### Example: Color Converter

```csharp
public class ColorConverter : IRouteTypeConverter
{
  public Type TargetType => typeof(Color);
  public string? ConstraintAlias => "color";  // Allows {param:color}

  public bool TryConvert(string value, out object? result)
  {
    result = value.ToLower() switch
    {
      "red" => Color.Red,
      "green" => Color.Green,
      "blue" => Color.Blue,
      _ => null
    };
    return result != null;
  }
}
```

### Registering Custom Converters

Register converters using `.AddTypeConverter()`:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .AddTypeConverter(new EmailAddressConverter())
  .AddTypeConverter(new ColorConverter())
  .Map("send {to:email}")
    .WithHandler((EmailAddress to) => SendEmail(to))
    .AsCommand()
    .Done()
  .Map("paint {color:color}")
    .WithHandler((Color color) => Paint(color))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./cli send user@example.com
./cli paint red
./cli paint blue
```

For a complete working example, see `samples/10-type-converters/`.

## Type Constraints in Analyzer

The Roslyn analyzer validates type syntax at compile-time:

```csharp
// ✅ Valid types
builder.Map("test {value:int}", handler);
builder.Map("test {value:DateTime}", handler);

// ❌ Invalid type (analyzer error NURU_P004)
builder.Map("test {value:integer}", handler);  // Use 'int'
builder.Map("test {value:float}", handler);    // Use 'double'
```

See [Analyzer Documentation](../features/analyzer.md) for more details.

## Best Practices

### Use Specific Types

```csharp
// ✅ Specific type for validation
.Map("wait {seconds:int}")
  .WithHandler((int sec) => ...)
  .AsCommand()
  .Done()

// ❌ String requires manual validation
NuruApp badApp = NuruApp.CreateBuilder(args)
  .Map("wait {seconds}")
    .WithHandler((string sec) =>
    {
      if (!int.TryParse(sec, out int value))
        throw new ArgumentException("Invalid number");
      // ...
    })
    .AsCommand()
    .Done()
  .Build();
```

### Nullable for Optional

```csharp
// ✅ Nullable type for optional parameter
.Map("deploy {env} {version?}")
  .WithHandler((string env, string? version) => ...)
  .AsCommand()
  .Done()

// ❌ Don't use default values in pattern
.Map("deploy {env} {version:v1.0}")  // Not supported
```

### Consistent Naming

```csharp
// ✅ Consistent type names
.Map("wait {seconds:int}")
.Map("delay {milliseconds:int}")

// ❌ Mixed naming styles
.Map("wait {seconds:Int}")    // Use lowercase 'int'
.Map("delay {milliseconds:INT}")  // Use lowercase 'int'
```

## Related Documentation

- **[Routing](../features/routing.md)** - Route pattern syntax
- **[Analyzer](../features/analyzer.md)** - Compile-time validation
- **[Examples](../../../samples/)** - Working code samples
