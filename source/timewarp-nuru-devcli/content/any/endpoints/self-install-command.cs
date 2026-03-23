#region Purpose
// Installs the dev CLI executable locally
#endregion
#region Design
// Uses dotnet publish to create the binary in ./bin
// Standalone - no external service dependencies
// CA1849 suppressed: synchronous terminal methods are acceptable in CLI context.
// CA1031 suppressed: catch-all is needed for rollback on any failure.
#endregion

namespace DevCli.Endpoints;

using TimeWarp.Amuru;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

#pragma warning disable CA1849 // Consider using an async method overload
#pragma warning disable CA1031 // Do not catch generally exception types

/// <summary>
/// AOT compile dev CLI to ./bin.
/// </summary>
[NuruRoute("self-install", Description = "AOT compile dev CLI to ./bin")]
public sealed class SelfInstallCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<SelfInstallCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(SelfInstallCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      string? repoRoot = Git.FindRoot();
      if (repoRoot is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      string devCliPath = Path.Combine(repoRoot, "tools", "dev-cli", "dev.cs");
      string outputDir = Path.Combine(repoRoot, "bin");

      string? targetExe = null;
      string? oldExe = null;
      bool renamed = false;

      // On Windows, rename running exe so we can replace it
      if (OperatingSystem.IsWindows())
      {
        targetExe = Path.Combine(outputDir, "dev.exe");
        oldExe = targetExe + ".old";

        if (File.Exists(targetExe))
        {
          // Clean up any leftover from previous failed update
          if (File.Exists(oldExe))
          {
            File.Delete(oldExe);
          }
          // Rename running exe - Windows allows this even while locked
          File.Move(targetExe, oldExe, overwrite: true);
          renamed = true;
        }
      }

      Terminal.WriteLine("Publishing dev CLI...");

      CommandOutput result = await DotNet.Publish(devCliPath)
        .WithOutput(outputDir)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (!result.Success)
      {
        Terminal.WriteLine("=== BUILD FAILED ===");
        Terminal.WriteLine($"Exit code: {result.ExitCode}");
        Terminal.WriteLine("=== STDOUT ===");
        Terminal.WriteLine(result.Stdout);
        Terminal.WriteLine("=== STDERR ===");
        Terminal.WriteLine(result.Stderr);

        // Rollback: restore old exe if we renamed it
        if (renamed && targetExe is not null && oldExe is not null && File.Exists(oldExe))
        {
          try
          {
            File.Move(oldExe, targetExe, overwrite: true);
            Terminal.WriteLine("Rolled back to previous version.");
          }
          catch (IOException)
          {
            Terminal.WriteLine($"WARNING: Could not restore {targetExe}. Old version at {oldExe}");
          }
        }

        Terminal.WriteErrorLine("Self-install failed!".Red());
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Terminal.WriteLine($"Successfully installed dev CLI to {outputDir}".Green());
      return Unit.Value;
    }
  }
}

#pragma warning restore CA1031
#pragma warning restore CA1849
