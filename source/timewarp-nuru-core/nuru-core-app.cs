namespace TimeWarp.Nuru;

public class NuruCoreApp
{
  public ITerminal Terminal { get; }
  public NuruCoreApp(ITerminal? terminal = null)
  {
    Terminal = terminal ?? TimeWarpTerminal.Default;
  }

  /// <summary>
  /// Runs the application. This method is intercepted by generated code.
  /// </summary>
  #pragma warning disable CA1822 // Member does not access instance data 
  public Task<int> RunAsync(string[] args)
  {
      // This should never execute - interceptor replaces this call
      throw new InvalidOperationException(
          "RunAsync was not intercepted. Ensure the source generator is enabled.");
  }
  #pragma warning restore CA1822
}