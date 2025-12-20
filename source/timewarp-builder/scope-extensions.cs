namespace TimeWarp.Builder;

/// <summary>
/// Kotlin-inspired scope functions for fluent object manipulation.
/// </summary>
public static class ScopeExtensions
{
  /// <summary>
  /// Executes an action on the object and returns the original object.
  /// Useful for side effects during method chaining.
  /// </summary>
  /// <typeparam name="T">The type of the object.</typeparam>
  /// <param name="obj">The object to operate on.</param>
  /// <param name="action">The action to execute.</param>
  /// <returns>The original object.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
  /// <example>
  /// <code>
  /// var builder = new NuruCoreAppBuilder()
  ///     .Also(b => Console.WriteLine("Building app..."));
  /// </code>
  /// </example>
  public static T Also<T>(this T obj, Action<T> action)
  {
    ArgumentNullException.ThrowIfNull(action);
    action(obj);
    return obj;
  }

  /// <summary>
  /// Configures the object and returns the original object.
  /// Semantically similar to <see cref="Also{T}"/> but with clearer intent for configuration.
  /// </summary>
  /// <typeparam name="T">The type of the object.</typeparam>
  /// <param name="obj">The object to configure.</param>
  /// <param name="action">The configuration action.</param>
  /// <returns>The original object.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
  /// <example>
  /// <code>
  /// app.Map("status", handler)
  ///    .Apply(r => r.AsQuery());  // Inline configure and continue
  /// </code>
  /// </example>
  public static T Apply<T>(this T obj, Action<T> action)
  {
    ArgumentNullException.ThrowIfNull(action);
    action(obj);
    return obj;
  }

  /// <summary>
  /// Transforms the object to a different type.
  /// </summary>
  /// <typeparam name="T">The type of the input object.</typeparam>
  /// <typeparam name="TResult">The type of the result.</typeparam>
  /// <param name="obj">The object to transform.</param>
  /// <param name="transform">The transformation function.</param>
  /// <returns>The transformed result.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="transform"/> is null.</exception>
  /// <example>
  /// <code>
  /// var length = "hello".Let(s => s.Length);  // 5
  /// </code>
  /// </example>
  public static TResult Let<T, TResult>(this T obj, Func<T, TResult> transform)
  {
    ArgumentNullException.ThrowIfNull(transform);
    return transform(obj);
  }

  /// <summary>
  /// Executes an action on the object with no return value.
  /// Terminal operation in a method chain.
  /// </summary>
  /// <typeparam name="T">The type of the object.</typeparam>
  /// <param name="obj">The object to operate on.</param>
  /// <param name="action">The action to execute.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
  /// <example>
  /// <code>
  /// app.Build().Run(a => a.RunAsync(args));
  /// </code>
  /// </example>
  public static void Run<T>(this T obj, Action<T> action)
  {
    ArgumentNullException.ThrowIfNull(action);
    action(obj);
  }
}
