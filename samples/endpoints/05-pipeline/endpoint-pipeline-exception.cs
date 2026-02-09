#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - EXCEPTION HANDLING PIPELINE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates consistent exception handling using INuruBehavior.
//
// DSL: Endpoint with ExceptionHandlingBehavior registered via .AddBehavior()
//
// BEHAVIOR DEMONSTRATED:
//   - ExceptionHandlingBehavior: Catches and categorizes exceptions
//   - Provides user-friendly error messages
//   - Logs detailed error information
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(ExceptionHandlingBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);

// =============================================================================
// EXCEPTION HANDLING BEHAVIOR
// =============================================================================

/// <summary>
/// Global exception handler that catches all exceptions and provides
/// user-friendly error messages with categorized error types.
/// </summary>
public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    try
    {
      await proceed();
    }
    catch (ArgumentException ex)
    {
      WriteLine($"[ERROR] Invalid input: {ex.Message}");
      throw;
    }
    catch (InvalidOperationException ex)
    {
      WriteLine($"[ERROR] Operation failed: {ex.Message}");
      throw;
    }
    catch (UnauthorizedAccessException ex)
    {
      WriteLine($"[ERROR] Access denied: {ex.Message}");
      throw;
    }
    catch (Exception ex)
    {
      WriteLine($"[ERROR] Unexpected error: {ex.Message}");
      throw;
    }
  }
}

// =============================================================================
// ENDPOINT DEFINITIONS - Various exception scenarios
// =============================================================================

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

[NuruRoute("process", Description = "Process an operation (may fail)")]
public sealed class ProcessCommand : ICommand<Unit>
{
  [Parameter(Description = "Operation to perform")]
  public string Operation { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<ProcessCommand, Unit>
  {
    public ValueTask<Unit> Handle(ProcessCommand command, CancellationToken ct)
    {
      if (command.Operation == "fail")
      {
        throw new InvalidOperationException("Processing intentionally failed");
      }

      if (command.Operation == "deny")
      {
        throw new UnauthorizedAccessException("Access denied for this operation");
      }

      WriteLine($"✓ Processed: {command.Operation}");
      return default;
    }
  }
}

[NuruRoute("divide", Description = "Divide two numbers (handles divide by zero)")]
public sealed class DivideCommand : ICommand<double>
{
  [Parameter(Description = "Dividend")]
  public double X { get; set; }

  [Parameter(Description = "Divisor")]
  public double Y { get; set; }

  public sealed class Handler : ICommandHandler<DivideCommand, double>
  {
    public ValueTask<double> Handle(DivideCommand command, CancellationToken ct)
    {
      if (command.Y == 0)
      {
        throw new DivideByZeroException("Cannot divide by zero");
      }

      return new ValueTask<double>(command.X / command.Y);
    }
  }
}
