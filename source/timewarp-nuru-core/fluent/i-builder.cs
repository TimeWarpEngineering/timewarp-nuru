namespace TimeWarp.Nuru;

/// <summary>
/// Interface for nested builders supporting pop-back to parent.
/// Enables fluent API patterns where child configurators can return to the parent builder.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to.</typeparam>
/// <remarks>
/// <para>
/// This pattern allows methods like <see cref="NuruCoreAppBuilder.Map"/> to return a
/// configuration object that can be further configured, then return to the parent builder
/// for continued chaining:
/// </para>
/// <code>
/// app.Map("status", handler)     // Returns EndpointBuilder&lt;TBuilder&gt;
///    .AsQuery()                   // Returns EndpointBuilder&lt;TBuilder&gt;
///    .Done()                      // Returns TBuilder (preserves derived type!)
///    .AddReplSupport()            // Extension method works!
///    .Build();
/// </code>
/// </remarks>
public interface IBuilder<out TParent> where TParent : class
{
  /// <summary>
  /// Completes the nested builder configuration and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder for continued chaining.</returns>
  TParent Done();
}
