// ═══════════════════════════════════════════════════════════════════════════════
// VALIDATE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Validate input (throws ArgumentException on invalid).

namespace PipelineException.Endpoints;

using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("validate", Description = "Validate input (throws ArgumentException on invalid)")]
public sealed class ValidateCommand : ICommand<Unit>
{
  [Parameter(Description = "Value to validate")]
  public string Value { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<ValidateCommand, Unit>
  {
    public ValueTask<Unit> Handle(ValidateCommand command, CancellationToken ct)
    {
      if (string.IsNullOrWhiteSpace(command.Value))
      {
        throw new ArgumentException("Value cannot be empty");
      }

      if (command.Value.Length < 3)
      {
        throw new ArgumentException("Value must be at least 3 characters");
      }

      WriteLine($"✓ Valid: {command.Value}");
      return default;
    }
  }
}
