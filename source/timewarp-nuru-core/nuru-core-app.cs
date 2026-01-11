namespace TimeWarp.Nuru;

public class NuruCoreApp
{
  public ITerminal Terminal { get; }
  public NuruCoreApp(ITerminal? terminal = null)
  {
    Terminal = terminal ?? TimeWarpTerminal.Default;
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
}