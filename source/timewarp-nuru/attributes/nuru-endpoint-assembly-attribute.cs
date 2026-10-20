namespace TimeWarp.Nuru;

/// <summary>
/// Adds the <c>[Operation]</c> and <see cref="NuruRouteAttribute"/> endpoint types of a referenced
/// assembly to endpoint discovery in this compilation.
/// </summary>
/// <remarks>
/// <para>
/// The generator discovers endpoints in the compiling project's own source. A contracts assembly
/// adds no routes until this attribute names it, so referencing an assembly never adds a surprise
/// command. Every public endpoint type in the named assembly then behaves like one declared in this
/// project: <c>DiscoverEndpoints()</c>, <c>DiscoverEndpoints(typeof(...))</c>, and
/// <c>Map&lt;T&gt;()</c> all see it.
/// </para>
/// <para>
/// <c>Map&lt;T&gt;()</c> on a referenced-assembly endpoint type works without this attribute and adds
/// that endpoint to that app only.
/// </para>
/// <para>
/// A referenced assembly has no property initializer syntax. An omitted option leaves the member
/// to its compiled initializer, and <c>--capabilities</c> shows a default only from
/// <see cref="System.ComponentModel.DefaultValueAttribute"/>. Mark a member the caller must supply
/// <c>required</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [assembly: NuruEndpointAssembly(typeof(Calculator.Contracts.AddQuery))]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class NuruEndpointAssemblyAttribute : Attribute
{
  /// <summary>
  /// Names the assembly that contains <paramref name="assemblyMarker"/>.
  /// </summary>
  /// <param name="assemblyMarker">Any type in the contracts assembly.</param>
  public NuruEndpointAssemblyAttribute(Type assemblyMarker)
  {
    AssemblyMarker = assemblyMarker;
  }

  /// <summary>
  /// A type in the contracts assembly.
  /// </summary>
  public Type AssemblyMarker { get; }
}
