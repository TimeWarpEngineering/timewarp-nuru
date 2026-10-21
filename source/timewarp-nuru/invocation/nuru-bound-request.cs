namespace TimeWarp.Nuru;

/// <summary>
/// A route request bound from JSON by <see cref="NuruApp.TryBind"/>. Pass it to
/// <see cref="NuruApp.InvokeAsync"/> on the same app.
/// </summary>
/// <remarks>
/// The source generator emits one sealed subclass per route that holds the bound values.
/// The values are copied out of the JSON, so the request does not keep the
/// <see cref="JsonDocument"/> alive.
/// </remarks>
public abstract class NuruBoundRequest
{
  /// <summary>
  /// Creates a bound request. Called by generated code.
  /// </summary>
  /// <param name="route">The route pattern the request was bound to.</param>
  protected NuruBoundRequest(string route)
  {
    Route = route;
  }

  /// <summary>
  /// The route pattern, as in <see cref="EndpointCapability.Pattern"/>.
  /// </summary>
  public string Route { get; }
}
