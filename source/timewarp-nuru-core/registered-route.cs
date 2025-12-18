namespace TimeWarp.Nuru;

/// <summary>
/// Represents a route registered via source-generated [ModuleInitializer] code.
/// Used by <see cref="NuruRouteRegistry"/> for attributed route auto-registration.
/// </summary>
public sealed class RegisteredRoute
{
  /// <summary>
  /// Gets the compiled route for matching.
  /// </summary>
  public CompiledRoute CompiledRoute { get; }

  /// <summary>
  /// Gets the route pattern string for help display.
  /// </summary>
  public string Pattern { get; }

  /// <summary>
  /// Gets the request type that handles this route.
  /// </summary>
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
  public Type RequestType { get; }

  /// <summary>
  /// Gets the optional description for help text.
  /// </summary>
  public string? Description { get; }

  /// <summary>
  /// Creates a new registered route.
  /// </summary>
  /// <param name="compiledRoute">The compiled route for matching.</param>
  /// <param name="pattern">The route pattern string for help display.</param>
  /// <param name="requestType">The request type that handles this route.</param>
  /// <param name="description">Optional description for help text.</param>
  public RegisteredRoute
  (
    CompiledRoute compiledRoute,
    string pattern,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    Type requestType,
    string? description = null
  )
  {
    CompiledRoute = compiledRoute ?? throw new ArgumentNullException(nameof(compiledRoute));
    Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
    Description = description;
  }
}
