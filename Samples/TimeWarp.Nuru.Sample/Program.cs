using System.Globalization;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using static System.Console;

// Build the app
NuruAppBuilder builder =
  new NuruAppBuilder()
    // If using Mediator Commands/Handlers, add dependency injection
    .AddDependencyInjection()
    .ConfigureServices(services => services.AddMediator());

// Add routes
builder.MapDefault // Default route when no command is specified
(
  () => WriteLine("Welcome to the Nuru sample app! Use --help to see available commands."),
  "Default welcome message"
);

builder.Map("status", () => WriteLine("✓ System is running"), "Check system status");
builder.Map("echo {message}", (string message) => WriteLine($"Echo: {message}"), "Echo a message back");

builder.Map
(
  "proxy {command} {*args}",
  (string command, string[] args) => WriteLine($"Would execute: {command} {string.Join(" ", args)}"),
  "Proxy command execution"
);

builder.Map<CalculateCommand, CalculateResponse>
(
  "calc {value1:double} {value2:double} --operation {operation}",
  "Perform calculation (operations: add, subtract, multiply, divide)"
);

// Build and run
NuruApp app = builder.Build();
return await app.RunAsync(args).ConfigureAwait(false);

// Command and handler definitions
internal sealed class CalculateCommand : IRequest<CalculateResponse>
{
  public double Value1 { get; set; }
  public double Value2 { get; set; }
  public string Operation { get; set; } = "add";
}

internal sealed class CalculateResponse
{
  public double Result { get; set; }
  public string Formula { get; set; } = "";
}

#pragma warning disable CA1812 // CalculateHandler is instantiated by dependency injection
internal sealed class CalculateHandler : IRequestHandler<CalculateCommand, CalculateResponse>
#pragma warning restore CA1812
{
  public ValueTask<CalculateResponse> Handle(CalculateCommand request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);

    double result = request.Operation.ToLower(CultureInfo.InvariantCulture) switch
    {
      "add" => request.Value1 + request.Value2,
      "subtract" => request.Value1 - request.Value2,
      "multiply" => request.Value1 * request.Value2,
      "divide" => request.Value2 != 0 ? request.Value1 / request.Value2 : double.NaN,
      _ => throw new ArgumentException($"Unknown operation: {request.Operation}")
    };

    string op = request.Operation.ToLower(CultureInfo.InvariantCulture) switch
    {
      "add" => "+",
      "subtract" => "-",
      "multiply" => "*",
      "divide" => "/",
      _ => "?"
    };

    return new ValueTask<CalculateResponse>(new CalculateResponse
    {
      Result = result,
      Formula = $"{request.Value1} {op} {request.Value2} = {result}"
    });
  }
}
