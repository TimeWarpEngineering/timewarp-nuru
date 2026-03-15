namespace TimeWarp.Nuru.Search.Services;

public sealed partial class CapabilitiesClient
{
  private readonly ILogger<CapabilitiesClient> logger;

  public CapabilitiesClient(ILogger<CapabilitiesClient> logger)
  {
    this.logger = logger;
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
      LogCliFailed(logger, cliPath, output.ExitCode);
      return null;
    }

    string stdout = output.Stdout.Trim();

    if (string.IsNullOrEmpty(stdout))
    {
      LogEmptyCapabilities(logger, cliPath);
      return null;
    }

    try
    {
      CapabilitiesResponse? response = JsonSerializer.Deserialize(stdout, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

      if (response is null)
      {
        LogDeserializeFailed(logger, cliPath);
        return null;
      }

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
      LogJsonParseError(logger, ex, cliPath);
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
