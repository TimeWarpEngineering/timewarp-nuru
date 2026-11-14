#region Purpose
// Runs a CLI executable with `--capabilities` and parses the JSON catalog it returns for indexing.
#endregion

#region Design
// The catalog is obtained by running the CLI with --capabilities rather than reading its assembly, so
// any Nuru app (AOT or not, any location) can be indexed with no reference to it. A non-zero exit,
// empty output or invalid JSON returns null and a logged warning, so one broken CLI does not abort an
// index run. RawJson is kept verbatim because the index stores the full catalog, while only the fields
// search needs are copied into CliCapabilities (invocation stays in RawJson).
#endregion

namespace TimeWarp.Nuru.Search.Services;

public sealed partial class CapabilitiesClient
{
  private readonly ILogger<CapabilitiesClient> Logger;

  public CapabilitiesClient(ILogger<CapabilitiesClient> logger)
  {
    this.Logger = logger;
  }

  public async Task<CliCapabilities?> GetCapabilitiesAsync(
    string cliPath,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(cliPath);

    CommandOutput output = await Shell.Builder(cliPath)
      .WithArguments("--capabilities")
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    if (!output.Success)
    {
      LogCliFailed(Logger, cliPath, output.ExitCode);
      return null;
    }

    string stdout = output.Stdout.Trim();

    if (string.IsNullOrEmpty(stdout))
    {
      LogEmptyCapabilities(Logger, cliPath);
      return null;
    }

    try
    {
      CapabilitiesResponse? response = JsonSerializer.Deserialize(stdout, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

      if (response is null)
      {
        LogDeserializeFailed(Logger, cliPath);
        return null;
      }

      // Search indexes endpoints. invocation stays in RawJson and is not copied here.
      return new CliCapabilities
      {
        Name = response.Name,
        Version = response.Version,
        Description = response.Description,
        Endpoints = response.Endpoints,
        RawJson = stdout
      };
    }
    catch (JsonException ex)
    {
      LogJsonParseError(Logger, ex, cliPath);
      return null;
    }
  }

  [LoggerMessage(LogLevel.Warning, "CLI {CliPath} returned exit code {ExitCode}")]
  private static partial void LogCliFailed(ILogger logger, string cliPath, int exitCode);

  [LoggerMessage(LogLevel.Warning, "CLI {CliPath} returned empty capabilities")]
  private static partial void LogEmptyCapabilities(ILogger logger, string cliPath);

  [LoggerMessage(LogLevel.Warning, "Failed to deserialize capabilities from {CliPath}")]
  private static partial void LogDeserializeFailed(ILogger logger, string cliPath);

  [LoggerMessage(LogLevel.Error, "Failed to parse capabilities JSON from {CliPath}")]
  private static partial void LogJsonParseError(ILogger logger, Exception ex, string cliPath);
}

public sealed class CliCapabilities
{
  public required string Name { get; init; }
  public required string Version { get; init; }
  public string? Description { get; init; }
  public required IReadOnlyList<EndpointCapability> Endpoints { get; init; }
  public required string RawJson { get; init; }
}
