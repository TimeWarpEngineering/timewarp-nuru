namespace TimeWarp.Nuru;

/// <summary>
/// Static registry for routes registered via source-generated [ModuleInitializer] code.
/// Used by the attributed route source generator for auto-registration without explicit Map() calls.
/// </summary>
/// <remarks>
/// <para>
/// Routes are registered at module initialization time (before Main() runs) via generated
/// [ModuleInitializer] methods. The registry uses a thread-safe collection since multiple
/// assemblies may register routes concurrently during startup.
/// </para>
/// <para>
/// During <see cref="NuruCoreAppBuilder.Build()"/>, all registered routes are added to the
/// endpoint collection alongside explicitly mapped routes.
/// </para>
/// <para>
/// A single request type can have multiple routes registered (e.g., for aliases).
/// </para>
/// </remarks>
public static class NuruRouteRegistry
{
  // Use ConcurrentBag to allow multiple routes for the same request type (aliases)
  private static readonly ConcurrentBag<RegisteredRoute> Routes = [];

  // Track registered patterns to avoid duplicates
  private static readonly ConcurrentDictionary<string, bool> RegisteredPatterns = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Registers a route for a request type. Called by generated [ModuleInitializer] code.
  /// </summary>
  /// <typeparam name="TRequest">The request type that implements <see cref="IMessage"/>.</typeparam>
  /// <param name="route">The compiled route for matching.</param>
  /// <param name="pattern">The route pattern string for help display.</param>
  /// <param name="description">Optional description for help text.</param>
  public static void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TRequest>(CompiledRoute route, string pattern, string? description = null)
    where TRequest : IMessage
  {
    Register(typeof(TRequest), route, pattern, description);
  }

  /// <summary>
  /// Registers a route for a request type (non-generic version for generated code).
  /// </summary>
  /// <param name="requestType">The request type.</param>
  /// <param name="route">The compiled route for matching.</param>
  /// <param name="pattern">The route pattern string for help display.</param>
  /// <param name="description">Optional description for help text.</param>
  public static void Register(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    Type requestType,
    CompiledRoute route,
    string pattern,
    string? description = null)
  {
    ArgumentNullException.ThrowIfNull(requestType);

    // Avoid duplicate pattern registrations
    if (!RegisteredPatterns.TryAdd(pattern, true))
    {
      return; // Pattern already registered
    }

    Routes.Add(new RegisteredRoute(route, pattern, requestType, description));
  }

  /// <summary>
  /// Gets all registered routes. Called by NuruApp.Build() to include auto-registered routes.
  /// </summary>
  public static IEnumerable<RegisteredRoute> RegisteredRoutes => Routes;

  /// <summary>
  /// Gets the count of registered routes.
  /// </summary>
  public static int Count => Routes.Count;

  /// <summary>
  /// Checks if a route is registered for the given request type.
  /// </summary>
  /// <typeparam name="TRequest">The request type to check.</typeparam>
  /// <returns>True if at least one route is registered for the type.</returns>
  public static bool IsRegistered<TRequest>() where TRequest : IMessage
    => Routes.Any(r => r.RequestType == typeof(TRequest));

  /// <summary>
  /// Checks if a route is registered for the given request type.
  /// </summary>
  /// <param name="requestType">The request type to check.</param>
  /// <returns>True if at least one route is registered for the type.</returns>
  public static bool IsRegistered(Type requestType)
    => Routes.Any(r => r.RequestType == requestType);

  /// <summary>
  /// Clears all registered routes. Used for testing.
  /// </summary>
  public static void Clear()
  {
    Routes.Clear();
    RegisteredPatterns.Clear();
  }
}
