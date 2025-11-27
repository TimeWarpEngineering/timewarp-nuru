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
NuruApp app = new NuruAppBuilder()
  // Type annotation optional for strings
  .Map("greet {name}", (string name) => $"Hello, {name}!")
  .Map("echo {message:string}", (string msg) => msg)
  .Build();
```

```bash
./cli greet Alice
./cli echo "Hello World"
```

### Integer

```csharp
NuruApp app = new NuruAppBuilder()
  .Map("wait {seconds:int}", (int sec) => Thread.Sleep(sec * 1000))
  .Map
  (
    "repeat {times:int} {text}",
    (int times, string text) =>
    {
      for (int i = 0; i < times; i++)
        Console.WriteLine(text);
    }
  )
  .Build();
```

```bash
./cli wait 5
./cli repeat 3 "Hello"
```

### Double

```csharp
NuruApp app = new NuruAppBuilder()
  .Map("calc {x:double} {y:double}", (double x, double y) => x + y)
  .Map("scale {factor:double}", (double f) => Scale(f))
  .Build();
```

```bash
./cli calc 3.14 2.86
./cli scale 1.5
```

### Boolean

```csharp
NuruApp app = new NuruAppBuilder()
  .Map
  (
    "set {key} {value:bool}",
    (string key, bool value) => Config.Set(key, value)
  )
  .Build();
```

```bash
./cli set debug true
./cli set verbose false
```

### DateTime

```csharp
NuruApp app = new NuruAppBuilder()
  .Map
  (
    "schedule {when:DateTime}",
    (DateTime dt) => Console.WriteLine($"Scheduled for {dt}")
  )
  .Build();
```

```bash
./cli schedule 2025-12-25
./cli schedule "2025-01-15 14:30:00"
```

### Guid

```csharp
NuruApp app = new NuruAppBuilder()
  .Map("get {id:Guid}", (Guid id) => GetRecord(id))
  .Map("delete {userId:Guid}", (Guid userId) => DeleteUser(userId))
  .Build();
```

```bash
./cli get 123e4567-e89b-12d3-a456-426614174000
```

### Long

```csharp
NuruApp app = new NuruAppBuilder()
  .Map("allocate {bytes:long}", (long bytes) => Allocate(bytes))
  .Build();
```

```bash
./cli allocate 9223372036854775807
```

### Decimal

```csharp
NuruApp app = new NuruAppBuilder()
  .Map
  (
    "pay {amount:decimal}",
    (decimal amt) => ProcessPayment(amt)
  )
  .Build();
```

```bash
./cli pay 999.99
```

### TimeSpan

```csharp
NuruApp app = new NuruAppBuilder()
  .Map
  (
    "timeout {duration:TimeSpan}",
    (TimeSpan ts) => SetTimeout(ts)
  )
  .Build();
```

```bash
./cli timeout 00:05:00      # 5 minutes
./cli timeout 1:30:00       # 1 hour 30 minutes
```

### Uri

```csharp
NuruApp app = new NuruAppBuilder()
  .Map("download {url:uri}", (Uri url) => Download(url))
  .Build();
```

```bash
./cli download https://example.com/file.zip
```

## Nullable Types

Use nullable types for optional parameters:

```csharp
NuruApp app = new NuruAppBuilder()
  .Map
  (
    "deploy {env} {version?}",
    (string env, string? version) =>
    {
      Console.WriteLine($"Deploying to {env}");
      if (version != null)
        Console.WriteLine($"Version: {version}");
    }
  )
  .Map
  (
    "wait {seconds:int?}",
    (int? seconds) =>
    {
      int delay = seconds ?? 5;  // Default to 5
      Thread.Sleep(delay * 1000);
    }
  )
  .Build();
```

## Arrays (Catch-All)

Use `string[]` for catch-all parameters:

```csharp
NuruApp app = new NuruAppBuilder()
  .Map
  (
    "echo {*words}",
    (string[] words) => Console.WriteLine(string.Join(" ", words))
  )
  .Map
  (
    "add {*files}",
    (string[] files) =>
    {
      foreach (string file in files)
        ProcessFile(file);
    }
  )
  .Build();
```

```bash
./cli echo Hello World from Nuru
./cli add file1.txt file2.txt file3.txt
```

## Type Conversion Errors

When conversion fails, users get clear error messages:

```bash
./cli wait abc
# Error: Cannot convert 'abc' to type Int32 for parameter 'seconds'

./cli schedule invalid-date
# Error: Cannot convert 'invalid-date' to type DateTime for parameter 'when'
```

## Custom Type Converters

You can add custom type converters for your own types:

```csharp
public class ColorConverter : ITypeConverter<Color>
{
  public Color Convert(string value)
  {
    return value.ToLower() switch
    {
      "red" => Color.Red,
      "green" => Color.Green,
      "blue" => Color.Blue,
      _ => throw new ArgumentException($"Unknown color: {value}")
    };
  }
}

// Register converter
builder.AddTypeConverter(new ColorConverter());

// Use in routes
builder.Map("paint {color:Color}", (Color color) => Paint(color));
```

```bash
./cli paint red
./cli paint blue
```

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
.Map("wait {seconds:int}", (int sec) => ...)

// ❌ String requires manual validation
NuruApp badApp = new NuruAppBuilder()
  .Map
  (
    "wait {seconds}",
    (string sec) =>
    {
      if (!int.TryParse(sec, out int value))
        throw new ArgumentException("Invalid number");
      // ...
    }
  )
  .Build();
```

### Nullable for Optional

```csharp
// ✅ Nullable type for optional parameter
.Map("deploy {env} {version?}", (string env, string? version) => ...)

// ❌ Don't use default values in pattern
.Map("deploy {env} {version:v1.0}", ...)  // Not supported
```

### Consistent Naming

```csharp
// ✅ Consistent type names
.Map("wait {seconds:int}", ...)
.Map("delay {milliseconds:int}", ...)

// ❌ Mixed naming styles
.Map("wait {seconds:Int}", ...)    // Use lowercase 'int'
.Map("delay {milliseconds:INT}", ...)  // Use lowercase 'int'
```

## Related Documentation

- **[Routing](../features/routing.md)** - Route pattern syntax
- **[Analyzer](../features/analyzer.md)** - Compile-time validation
- **[Examples](../../../Samples/)** - Working code samples
