namespace TimeWarp.Nuru;

/// <summary>
/// Registry for managing completion sources by parameter name or type.
/// </summary>
public sealed class CompletionSourceRegistry
{
  private readonly Dictionary<string, ICompletionSource> parameterSources = [];
  private readonly Dictionary<Type, ICompletionSource> typeSources = [];

  /// <summary>
  /// Registers a completion source for a specific parameter name.
  /// </summary>
  /// <param name="parameterName">The parameter name (e.g., "env", "config").</param>
  /// <param name="source">The completion source to use for this parameter.</param>
  public void RegisterForParameter(string parameterName, ICompletionSource source)
  {
    ArgumentNullException.ThrowIfNull(parameterName);
    ArgumentNullException.ThrowIfNull(source);

    parameterSources[parameterName] = source;
  }

  /// <summary>
  /// Registers a completion source for a specific parameter type.
  /// This allows reusable completion sources across multiple parameters of the same type.
  /// </summary>
  /// <param name="type">The parameter type.</param>
  /// <param name="source">The completion source to use for this type.</param>
  public void RegisterForType(Type type, ICompletionSource source)
  {
    ArgumentNullException.ThrowIfNull(type);
    ArgumentNullException.ThrowIfNull(source);

    typeSources[type] = source;
  }

  /// <summary>
  /// Gets a completion source for a specific parameter name.
  /// </summary>
  /// <param name="parameterName">The parameter name to look up.</param>
  /// <returns>The registered completion source, or null if not found.</returns>
  public ICompletionSource? GetSourceForParameter(string parameterName)
  {
    ArgumentNullException.ThrowIfNull(parameterName);

    return parameterSources.TryGetValue(parameterName, out ICompletionSource? source)
      ? source
      : null;
  }

  /// <summary>
  /// Gets a completion source for a specific parameter type.
  /// </summary>
  /// <param name="type">The parameter type to look up.</param>
  /// <returns>The registered completion source, or null if not found.</returns>
  public ICompletionSource? GetSourceForType(Type type)
  {
    ArgumentNullException.ThrowIfNull(type);

    return typeSources.TryGetValue(type, out ICompletionSource? source)
      ? source
      : null;
  }
}
