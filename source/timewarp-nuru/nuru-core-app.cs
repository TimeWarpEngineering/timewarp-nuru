namespace TimeWarp.Nuru;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

public class NuruCoreApp
{
  public ITerminal Terminal { get; }
  public ReplOptions? ReplOptions { get; init; }
  public ILoggerFactory? LoggerFactory { get; init; }

  /// <summary>
  /// Tracer provider for distributed tracing. Set by generated telemetry code.
  /// </summary>
  public TracerProvider? TracerProvider { get; set; }

  /// <summary>
  /// Meter provider for metrics. Set by generated telemetry code.
  /// </summary>
  public MeterProvider? MeterProvider { get; set; }

  public NuruCoreApp(ITerminal? terminal = null)
  {
    Terminal = terminal ?? TimeWarpTerminal.Default;
  }

  /// <summary>
  /// Flushes all telemetry data and disposes providers.
  /// Called automatically by generated RunAsync code when telemetry is enabled.
  /// </summary>
  /// <param name="delayMs">Delay in milliseconds to allow export to complete. Default: 1000ms.</param>
  public async Task FlushTelemetryAsync(int delayMs = 1000)
  {
    TracerProvider?.ForceFlush();
    MeterProvider?.ForceFlush();

    if (delayMs > 0)
    {
      await Task.Delay(delayMs).ConfigureAwait(false);
    }

    TracerProvider?.Dispose();
    MeterProvider?.Dispose();
    TracerProvider = null;
    MeterProvider = null;
  }

  /// <summary>
  /// Runs the application with the given command-line arguments.
  /// This method is intercepted by generated code.
  /// </summary>
  /// <param name="args">Command-line arguments to parse and route.</param>
  /// <returns>
  /// Exit code: 0 for success, non-zero for failure.
  /// <para>
  /// <b>Important:</b> Handler return values are written to the terminal as output,
  /// they do NOT control the exit code. For example, <c>.WithHandler(() =&gt; 42)</c>
  /// outputs "42" to the terminal but still returns exit code 0.
  /// </para>
  /// <para>
  /// To signal failure with a non-zero exit code, throw an exception from your handler.
  /// The exception message will be displayed and exit code will be 1.
  /// </para>
  /// </returns>
  #pragma warning disable CA1822 // Member does not access instance data 
  public Task<int> RunAsync(string[] args)
  {
      // This should never execute - interceptor replaces this call
      throw new InvalidOperationException(
          "RunAsync was not intercepted. Ensure the source generator is enabled.");
  }
  #pragma warning restore CA1822

  /// <summary>
  /// Runs the application in REPL (interactive) mode.
  /// This method is intercepted by generated code when AddRepl() is called.
  /// </summary>
  /// <param name="cancellationToken">Optional cancellation token.</param>
  /// <returns>A task that completes when the REPL session exits.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if REPL mode is not enabled via AddRepl() in the builder,
  /// or if the source generator is not enabled.
  /// </exception>
  #pragma warning disable CA1822 // Member does not access instance data
  public Task RunReplAsync(CancellationToken cancellationToken = default)
  {
      // This should never execute - interceptor replaces this call
      throw new InvalidOperationException(
          "RunReplAsync was not intercepted. Ensure AddRepl() is called and the source generator is enabled.");
  }
  #pragma warning restore CA1822
}