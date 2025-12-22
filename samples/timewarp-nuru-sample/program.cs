// ═══════════════════════════════════════════════════════════════════════════════
// TIMEWARP.NURU SAMPLE - GENERAL REFERENCE APPLICATION
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) which provides:
// - Full DI container setup
// - Configuration support
// - Auto-help generation
// - REPL support with tab completion
// - All extensions enabled by default
//
// REQUIRED PACKAGES (in .csproj):
//   Mediator.Abstractions    - Interfaces (IRequest, IRequestHandler)
//   Mediator.SourceGenerator - Generates AddMediator() in YOUR assembly
//
// COMMON ERROR:
//   "No service for type 'Mediator.IMediator' has been registered"
//   SOLUTION: Ensure ConfigureServices calls services.AddMediator()
// ═══════════════════════════════════════════════════════════════════════════════

using System.Globalization;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using static System.Console;

// Build the app with canonical CreateBuilder pattern
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services => services.AddMediator())
  // Default route when no command is specified
  .Map("")
    .WithHandler(() => WriteLine("Welcome to the Nuru sample app! Use --help to see available commands."))
    .WithDescription("Default welcome message")
    .AsQuery()
    .Done()
  .Map("status")
    .WithHandler(() => WriteLine("✓ System is running"))
    .WithDescription("Check system status")
    .AsQuery()
    .Done()
  .Map("echo {message}")
    .WithHandler((string message) => WriteLine($"Echo: {message}"))
    .WithDescription("Echo a message back")
    .AsQuery()
    .Done()
  .Map("proxy {command} {*args}")
    .WithHandler((string command, string[] args) => WriteLine($"Would execute: {command} {string.Join(" ", args)}"))
    .WithDescription("Proxy command execution")
    .AsCommand()
    .Done()
  .Map<CalculateCommand, CalculateResponse>("calc {value1:double} {value2:double} --operation {operation}")
    .WithDescription("Perform calculation (operations: add, subtract, multiply, divide)")
    .AsQuery()
    .Done()
  .Build();

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
