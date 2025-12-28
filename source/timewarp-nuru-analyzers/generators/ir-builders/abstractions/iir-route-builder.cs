// Marker interface for route configuration builders.
//
// Routes are configured with handler, description, message type, etc.
// and return to their parent (app or group) via Done().

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Route configuration builder that returns to parent via Done().
/// </summary>
public interface IIrRouteBuilder
{
  /// <summary>
  /// Sets the handler for this route.
  /// </summary>
  /// <param name="handler">The handler definition.</param>
  /// <returns>This builder for chaining.</returns>
  IIrRouteBuilder WithHandler(HandlerDefinition handler);

  /// <summary>
  /// Sets the description for this route.
  /// </summary>
  /// <param name="description">The route description.</param>
  /// <returns>This builder for chaining.</returns>
  IIrRouteBuilder WithDescription(string description);

  /// <summary>
  /// Adds an alias for this route.
  /// </summary>
  /// <param name="aliasPattern">The alias pattern.</param>
  /// <returns>This builder for chaining.</returns>
  IIrRouteBuilder WithAlias(string aliasPattern);

  /// <summary>
  /// Marks this route as a Query (read-only, safe to retry).
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  IIrRouteBuilder AsQuery();

  /// <summary>
  /// Marks this route as a Command (state-changing, not idempotent).
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  IIrRouteBuilder AsCommand();

  /// <summary>
  /// Marks this route as an IdempotentCommand (state-changing but safe to retry).
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  IIrRouteBuilder AsIdempotentCommand();

  /// <summary>
  /// Completes the route configuration and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder (boxed as object for polymorphic dispatch).</returns>
  object Done();
}
