namespace TimeWarp.Nuru;

/// <summary>
/// Interface for top-level builders that create objects via <see cref="Build"/>.
/// </summary>
/// <typeparam name="TBuilt">The type of object this builder creates.</typeparam>
/// <remarks>
/// <para>
/// This interface is for standalone builders that produce a final object.
/// For nested builders that return to a parent context, see <see cref="INestedBuilder{TParent}"/>.
/// </para>
/// <code>
/// // Standalone builder creates CompiledRoute
/// CompiledRoute route = new CompiledRouteBuilder()
///     .WithLiteral("deploy")
///     .WithParameter("env")
///     .Build();  // Returns CompiledRoute
/// </code>
/// </remarks>
public interface IBuilder<out TBuilt>
{
  /// <summary>
  /// Builds and returns the configured object.
  /// </summary>
  /// <returns>The built object of type <typeparamref name="TBuilt"/>.</returns>
  TBuilt Build();
}
