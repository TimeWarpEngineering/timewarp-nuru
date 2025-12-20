namespace TimeWarp.Builder;

/// <summary>
/// Interface for nested builders that return to a parent context via <see cref="Done"/>.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to.</typeparam>
/// <remarks>
/// <para>
/// This interface enables fluent API patterns where child builders can return to the parent
/// for continued chaining. Nested builders typically wrap a standalone <see cref="IBuilder{TBuilt}"/>
/// internally and delegate building to it.
/// </para>
/// <para>
/// The <see cref="Done"/> method performs: Build() + pass result to parent + return parent.
/// </para>
/// <code>
/// // Nested builder returns to parent after building
/// app.Map(r => r                                    // NestedCompiledRouteBuilder&lt;EndpointBuilder&gt;
///     .WithLiteral("deploy")
///     .WithParameter("env")
///     .Done())                                      // Builds route, returns EndpointBuilder
///     .WithHandler(handler)
///     .Done();                                      // Returns to app builder
/// </code>
/// </remarks>
public interface INestedBuilder<out TParent> where TParent : class
{
  /// <summary>
  /// Completes the nested builder configuration and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder for continued chaining.</returns>
  TParent Done();
}
