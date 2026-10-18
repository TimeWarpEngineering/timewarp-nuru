namespace TimeWarp.Nuru;

/// <summary>
/// CLI binding overrides for an <c>[Operation]</c> request.
/// </summary>
/// <remarks>
/// <para>
/// TimeWarp.Contracts <c>CliOperationBinding</c> carries <c>Pattern</c> only.
/// The group path is a Nuru binding: a containing <see cref="NuruRouteGroupAttribute"/> wins,
/// otherwise <see cref="Group"/>, otherwise the endpoint is ungrouped.
/// </para>
/// <para>
/// <see cref="Group"/> is one route-group prefix. Several literals are separated by spaces,
/// the same way a <see cref="NuruRouteGroupAttribute"/> prefix is. Nested prefixes stay on
/// <see cref="NuruRouteGroupAttribute"/> base classes.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class NuruCliBindingAttribute : Attribute
{
  /// <summary>
  /// Optional group path. A single literal, or several literals separated by spaces.
  /// Ignored when the request inherits a <see cref="NuruRouteGroupAttribute"/>.
  /// </summary>
  public string? Group { get; set; }
}
