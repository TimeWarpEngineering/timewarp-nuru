// ═══════════════════════════════════════════════════════════════════════════════
// FACTORIAL COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Calculates factorial (n!) using scientific calculator service.

namespace EndpointCalculator.Endpoints;

using EndpointCalculator.Services;
using TimeWarp.Nuru;

/// <summary>
/// Calculate factorial (n!).
/// </summary>
[NuruRoute("factorial", Description = "Calculate factorial (n!)")]
public sealed class FactorialCommand : ICommand<Unit>
{
  [Parameter(Description = "Number to calculate factorial for")]
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<FactorialCommand, Unit>
  {
    public ValueTask<Unit> Handle(FactorialCommand command, CancellationToken cancellationToken)
    {
      try
      {
        long result = calc.Factorial(command.N);
        Console.WriteLine($"{command.N}! = {result}");
      }
      catch (ArgumentException ex)
      {
        Console.WriteLine($"Error: {ex.Message}");
      }

      return default;
    }
  }
}
