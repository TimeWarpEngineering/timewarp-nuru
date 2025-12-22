# TimeWarp.Builder

Fluent builder interfaces and Kotlin-inspired scope extensions for .NET.

## Interfaces

### IBuilder\<T\>

Interface for standalone builders that create objects via `Build()`.

```csharp
public class MyWidgetBuilder : IBuilder<Widget>
{
    public Widget Build() => new Widget(_options);
}

// Usage
Widget widget = new MyWidgetBuilder()
    .WithColor("blue")
    .WithSize(10)
    .Build();
```

### INestedBuilder\<TParent\>

Interface for nested builders that return to a parent context via `Done()`.

```csharp
// Nested builder returns to parent after building
app.Map(route => route
    .WithLiteral("deploy")
    .WithParameter("env")
    .Done())  // Returns to parent builder
    .WithHandler(handler);
```

## Scope Extensions

Kotlin-inspired extension methods for fluent object manipulation.

### Also

Executes an action on the object and returns the original object. Useful for side effects during method chaining.

```csharp
var builder = new AppBuilder()
    .Also(b => Console.WriteLine("Building app..."))
    .Configure(options);
```

### Apply

Configures the object and returns the original object. Semantically similar to `Also` but with clearer intent for configuration.

```csharp
app.Map("status", handler)
   .Apply(r => r.AsQuery());
```

### Let

Transforms the object to a different type.

```csharp
int length = "hello".Let(s => s.Length);  // 5
```

### Run

Executes an action on the object with no return value. Terminal operation in a method chain.

```csharp
app.Build().Run(a => a.RunAsync(args));
```
