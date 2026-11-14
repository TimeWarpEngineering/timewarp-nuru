#region Purpose
// Registry that maps parameter names and types to custom completion sources.
#endregion

#region Design
// Parameter-name sources are consulted before type sources (see DynamicCompletionHandler), so a
// name-specific source overrides a type-wide one. Registering again replaces silently. The registry
// is instance-owned by the app and holds only runtime-registered sources; static route data comes
// from the generated provider instead.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Registry for managing completion sources by parameter name or type.
/// </summary>
public sealed class CompletionSourceRegistry
{
  private readonly Dictionary<string, ICompletionSource> ParameterSources = [];
  private readonly Dictionary<Type, ICompletionSource> TypeSources = [];

  /// <summary>
  /// Registers a completion source for a specific parameter name.
  /// </summary>
  /// <param name="parameterName">The parameter name (e.g., "env", "config").</param>
  /// <param name="source">The completion source to use for this parameter.</param>
  public void RegisterForParameter(string parameterName, ICompletionSource source)
  {
    ArgumentNullException.ThrowIfNull(parameterName);
    ArgumentNullException.ThrowIfNull(source);

    ParameterSources[parameterName] = source;
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

    TypeSources[type] = source;
  }

  /// <summary>
  /// Gets a completion source for a specific parameter name.
  /// </summary>
  /// <param name="parameterName">The parameter name to look up.</param>
  /// <returns>The registered completion source, or null if not found.</returns>
  public ICompletionSource? GetSourceForParameter(string parameterName)
  {
    ArgumentNullException.ThrowIfNull(parameterName);

    return ParameterSources.TryGetValue(parameterName, out ICompletionSource? source)
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

    return TypeSources.TryGetValue(type, out ICompletionSource? source)
      ? source
      : null;
  }
}
