// ═══════════════════════════════════════════════════════════════════════════════
// PUBLISH COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// AOT publishes the dev CLI to ./bin for fast execution via direnv PATH.

using System.Runtime.InteropServices;

namespace DevCli.Commands;

/// <summary>
/// AOT publish dev CLI to ./bin directory.
/// </summary>
[NuruRoute("publish", Description = "AOT publish dev CLI to ./bin")]
internal sealed class PublishCommand : ICommand<Unit>
{
  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  internal sealed class Handler : ICommandHandler<PublishCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(PublishCommand command, CancellationToken ct)
    {
      // Get repo root
      string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

      // Verify we're in the right place
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        repoRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
        {
          throw new InvalidOperationException("Could not find repository root (timewarp-nuru.slnx not found)");
        }
      }

      string devCliSource = Path.Combine(repoRoot, "tools", "dev-cli", "dev.cs");
      string outputPath = Path.Combine(repoRoot, "bin");
      string rid = GetRuntimeIdentifier();

      Terminal.WriteLine("Publishing dev CLI as AOT binary...");
      Terminal.WriteLine($"Source: {devCliSource}");
      Terminal.WriteLine($"Output: {outputPath}/dev");
      Terminal.WriteLine($"Runtime: {rid}");

      // Ensure bin directory exists
      Directory.CreateDirectory(outputPath);

      // Build the AOT binary
      CommandResult publishResult = DotNet.Publish()
        .WithProject(devCliSource)
        .WithConfiguration("Release")
        .WithRuntime(rid)
        .WithSelfContained()
        .WithOutput(outputPath)
        .Build();

      if (command.Verbose)
      {
        Terminal.WriteLine($"\nRunning: {publishResult.ToCommandString()}");
      }

      int exitCode = await publishResult.RunAsync();

      if (exitCode != 0)
      {
        throw new InvalidOperationException("AOT publish failed!");
      }

      // Verify the binary was created
      string binaryName = rid.StartsWith("win", StringComparison.OrdinalIgnoreCase) ? "dev.exe" : "dev";
      string binaryPath = Path.Combine(outputPath, binaryName);

      if (File.Exists(binaryPath))
      {
        FileInfo info = new(binaryPath);
        Terminal.WriteLine($"\n✅ AOT binary created: {binaryPath}");
        Terminal.WriteLine($"   Size: {info.Length / 1024.0 / 1024.0:F1} MB");
        Terminal.WriteLine($"\nRun 'direnv allow' to add ./bin to PATH, then use: dev <command>");
      }
      else
      {
        throw new InvalidOperationException($"Binary not found at expected location: {binaryPath}");
      }

      return Unit.Value;
    }

    private static string GetRuntimeIdentifier()
    {
      if (OperatingSystem.IsWindows())
      {
        return Environment.Is64BitOperatingSystem ? "win-x64" : "win-x86";
      }
      else if (OperatingSystem.IsMacOS())
      {
        return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
      }
      else if (OperatingSystem.IsLinux())
      {
        return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
      }

      return "linux-x64";
    }
  }
}
