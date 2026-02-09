// ═══════════════════════════════════════════════════════════════════════════════
// PRIME CHECK COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Checks if a number is prime using scientific calculator service.

namespace EndpointCalculator.Endpoints;

using EndpointCalculator.Services;
using TimeWarp.Nuru;

/// <summary>
/// Check if a number is prime.
/// </summary>
[NuruRoute("isprime", Description = "Check if a number is prime")]
public sealed class PrimeCheckCommand : ICommand<Unit>
{
  [Parameter(Description = "Number to check")]
  public int N { get; set; }

  public sealed class Handler(IScientificCalculator calc) : ICommandHandler<PrimeCheckCommand, Unit>
  {
    public ValueTask<Unit> Handle(PrimeCheckCommand command, CancellationToken cancellationToken)
    {
      bool result = calc.IsPrime(command.N);
      Console.WriteLine($"{command.N} is {(result ? "prime" : "not prime")}");
      return default;
    }
  }
}
