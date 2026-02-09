// ═══════════════════════════════════════════════════════════════════════════════
// FIBONACCI COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Calculates the nth Fibonacci number using scientific calculator service.

namespace EndpointCalculator.Endpoints;

using EndpointCalculator.Services;
using TimeWarp.Nuru;

/// <summary>
/// Calculate the nth Fibonacci number.
/// </summary>
[NuruRoute("fibonacci", Description = "Calculate the nth Fibonacci number")]
public sealed class FibonacciCommand : ICommand<Unit>
{
  [Parameter(Description = "Index in Fibonacci sequence")]
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<FibonacciCommand, Unit>
  {
    public ValueTask<Unit> Handle(FibonacciCommand command, CancellationToken cancellationToken)
    {
      try
      {
        long result = calc.Fibonacci(command.N);
        Console.WriteLine($"Fibonacci({command.N}) = {result}");
      }
      catch (ArgumentException ex)
      {
        Console.WriteLine($"Error: {ex.Message}");
      }

      return default;
    }
  }
}
