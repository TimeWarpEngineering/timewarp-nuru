// Model for interface implementations declared via .Implements<T>() on delegate routes.
// See kanban task #316 for design.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents an interface implementation declared via <c>.Implements&lt;T&gt;()</c> on a delegate route.
/// The generator uses this to emit a command class that implements the specified interface.
/// </summary>
/// <param name="FullInterfaceTypeName">
/// Fully qualified type name of the interface (e.g., "global::MyApp.IRequireAuthorization").
/// </param>
/// <param name="Properties">
/// Property assignments extracted from the configuration expression.
/// </param>
public sealed record InterfaceImplementationDefinition(
  string FullInterfaceTypeName,
  ImmutableArray<PropertyAssignment> Properties)
{
  /// <summary>
  /// Gets the short interface name for display (e.g., "IRequireAuthorization").
  /// </summary>
  public string ShortInterfaceName
  {
    get
    {
      string typeName = FullInterfaceTypeName;
      if (typeName.StartsWith("global::", StringComparison.Ordinal))
        typeName = typeName[8..];

      int lastDot = typeName.LastIndexOf('.');
      return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
    }
  }
}

/// <summary>
/// Represents a property assignment extracted from an <c>.Implements&lt;T&gt;()</c> expression.
/// </summary>
/// <param name="PropertyName">The name of the property being assigned (e.g., "RequiredPermission").</param>
/// <param name="PropertyType">The fully qualified type of the property (e.g., "string").</param>
/// <param name="ValueExpression">
/// The value expression as source code (e.g., "\"admin:execute\"" or "42").
/// This is emitted directly into the generated property getter.
/// </param>
public sealed record PropertyAssignment(
  string PropertyName,
  string PropertyType,
  string ValueExpression);
