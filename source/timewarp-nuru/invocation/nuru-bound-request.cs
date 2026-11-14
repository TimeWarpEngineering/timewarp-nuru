#region Purpose
// Base type for requests bound from JSON by TryBind, ready to pass to InvokeAsync.
#endregion

#region Design
// Abstract so each route's generated sealed subclass can carry strongly typed bound values while
// callers hold only this base. Values are copied out of the JSON so the request outlives the
// JsonDocument. A request is tied to the app that bound it and is only valid for InvokeAsync
// on that app.
// The constructor is protected: it is called by generated code, not by consumers.
#endregion

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
