// Marker interface for IR builders that can create routes and nested groups.
//
// Implemented by both IrAppBuilder (top-level) and IrGroupBuilder (nested).
// Enables unified dispatch in DslInterpreter for Map() and WithGroupPrefix() calls.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Can create routes and nested groups.
/// Implemented by both IrAppBuilder and IrGroupBuilder.
/// </summary>
public interface IIrRouteSource
{
  /// <summary>
  /// Creates a new route with the given pattern.
  /// The implementation handles segment extraction and prefix accumulation.
  /// </summary>
  /// <param name="pattern">The route pattern (e.g., "status" or "get {key}").</param>
  /// <returns>A route builder for configuring the route.</returns>
  IIrRouteBuilder Map(string pattern);

  /// <summary>
  /// Creates a nested group with the given prefix.
  /// Routes within the group will have the prefix prepended.
  /// </summary>
  /// <param name="prefix">The group prefix (e.g., "admin").</param>
  /// <returns>A group builder for the nested group.</returns>
  IIrGroupBuilder WithGroupPrefix(string prefix);
}
