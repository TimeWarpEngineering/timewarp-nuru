# Command Filter Comparison: Cocona vs Nuru

This example demonstrates cross-cutting concerns (logging, validation, exception handling) in command-line applications.

## Cocona Implementation

```csharp
[SampleCommandFilter("Class")]
class Program
{
    static void Main(string[] args)
    {
        CoconaApp.Run<Program>(args);
    }

    [SampleCommandFilter("Method")]
    [SampleCommandFilterWithDI]
    public void Hello()
    {
        Console.WriteLine($"Hello Konnichiwa");
    }
}

class SampleCommandFilterAttribute : CommandFilterAttribute
{
    private readonly string _label;

    public SampleCommandFilterAttribute(string label)
    {
        _label = label;
    }

    public override async ValueTask<int> OnCommandExecutionAsync(CoconaCommandExecutingContext ctx, CommandExecutionDelegate next)
    {
        Console.WriteLine($"[SampleCommandFilter({_label})] Before Command: {ctx.Command.Name}");
        try
        {
            return await next(ctx);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SampleCommandFilter({_label})] Exception: {ex.GetType().FullName}: {ex.Message}");
            throw;
        }
        finally
        {
            Console.WriteLine($"[SampleCommandFilter({_label})] End Command: {ctx.Command.Name}");
        }
    }
}
```

## Nuru Implementation (DI/Mediator)

```csharp
// Create app with DI and pipeline behaviors
var builder = new NuruAppBuilder()
    .AddDependencyInjection(config => 
    {
        config.RegisterServicesFromAssembly(typeof(HelloCommand).Assembly);
    });

// Configure logging and add pipeline behaviors
builder.Services.AddLogging(config => 
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var app = builder
    .AddRoute<HelloCommand>("hello")
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);

// Command with multiple pipeline behaviors applied
public sealed class HelloCommand : IRequest
{
    internal sealed class Handler : IRequestHandler<HelloCommand>
    {
        public async Task Handle(HelloCommand request, CancellationToken cancellationToken)
        {
            WriteLine($"Hello Konnichiwa");
            await Task.CompletedTask;
        }
    }
}

// Logging behavior (similar to CommandFilter)
public sealed class LoggingBehavior<TRequest, TResponse> : TimeWarp.Mediator.IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation($"[LoggingBehavior] Before Command: {requestName}");
        
        try
        {
            var response = await next();
            _logger.LogInformation($"[LoggingBehavior] After Command: {requestName}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[LoggingBehavior] Exception: {ex.GetType().FullName}: {ex.Message}");
            throw;
        }
    }
}
```

## Key Differences

### Filter/Middleware Approach
- **Cocona**: Uses attribute-based filters (CommandFilterAttribute)
- **Nuru**: Uses Mediator pipeline behaviors (IPipelineBehavior)

### Registration
- **Cocona**: Filters applied via attributes on classes or methods
- **Nuru**: Behaviors registered in DI container as open generics

### Execution Order
- **Cocona**: Filters execute in order of declaration (class then method)
- **Nuru**: Behaviors execute in registration order

### Dependency Injection
- **Cocona**: Supports DI through IFilterFactory pattern
- **Nuru**: Full DI support inherent in pipeline behaviors

### Flexibility
- **Cocona**: Filters are coupled to commands via attributes
- **Nuru**: Behaviors are decoupled and apply to all commands automatically

## Output Example

When running the command:

```bash
./command-filter-di hello
```

Output with logging enabled:
```
info: LoggingBehavior[0]
      [LoggingBehavior] Before Command: HelloCommand
info: ValidationBehavior[0]
      [ValidationBehavior] Validating: HelloCommand
Hello Konnichiwa
info: LoggingBehavior[0]
      [LoggingBehavior] After Command: HelloCommand
```

## Evaluation

Both frameworks support cross-cutting concerns effectively:

- **Cocona's approach** is more explicit with attributes directly on commands, making it clear which filters apply where
- **Nuru's approach** leverages the Mediator pattern's pipeline, providing a more decoupled and testable architecture

The Mediator pipeline in Nuru offers several advantages:
1. **Separation of Concerns**: Behaviors are independent of commands
2. **Testability**: Behaviors can be unit tested in isolation
3. **Reusability**: Same behavior applies to all commands automatically
4. **Composability**: Easy to add/remove behaviors without changing commands

For the delegate-based approach in Nuru, similar functionality could be achieved by wrapping delegate calls, but the Mediator pattern provides a cleaner solution for cross-cutting concerns.