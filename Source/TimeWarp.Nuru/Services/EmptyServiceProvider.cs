namespace TimeWarp.Nuru;

/// <summary>
/// Provides an empty service provider for scenarios without dependency injection.
/// </summary>
internal sealed class EmptyServiceProvider : IServiceProvider
{
  public static readonly EmptyServiceProvider Instance = new();

  private EmptyServiceProvider() { }

  public object? GetService(Type serviceType) => null;
}
