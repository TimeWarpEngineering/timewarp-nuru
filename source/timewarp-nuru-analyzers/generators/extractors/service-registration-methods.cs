#region Purpose
// Name-based classification of IServiceCollection APIs the source-gen DI model can lower.
#endregion

#region Design
// Whitelist primitives (Add{Lifetime} / TryAdd{Lifetime}), never user method names.
// AddLogging / AddHttpClient stay special-cased elsewhere and are not lowered here.
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Classifies IServiceCollection registration APIs that source-gen DI can lower to
/// <see cref="ServiceDefinition"/> entries.
/// </summary>
internal static class ServiceRegistrationMethods
{
  /// <summary>
  /// Standard lifetime registration methods with bodies we already analyze.
  /// </summary>
  internal static readonly HashSet<string> LifetimeAdds =
    new(StringComparer.Ordinal) { "AddTransient", "AddScoped", "AddSingleton" };

  /// <summary>
  /// TryAdd variants replayed in order against the accumulated model.
  /// </summary>
  internal static readonly HashSet<string> TryAdds =
    new(StringComparer.Ordinal) { "TryAddTransient", "TryAddScoped", "TryAddSingleton" };

  /// <summary>
  /// Existing special-cases that scrape lambdas; not lowered as collection scripts.
  /// </summary>
  internal static readonly HashSet<string> SpecialCased =
    new(StringComparer.Ordinal) { "AddLogging", "AddHttpClient" };

  internal static bool IsLifetimeAdd(string methodName) => LifetimeAdds.Contains(methodName);

  internal static bool IsTryAdd(string methodName) => TryAdds.Contains(methodName);

  internal static bool IsSpecialCased(string methodName) => SpecialCased.Contains(methodName);

  internal static bool TryGetLifetime(string methodName, out ServiceLifetime lifetime, out bool isTryAdd)
  {
    isTryAdd = IsTryAdd(methodName);
    if (!IsLifetimeAdd(methodName) && !isTryAdd)
    {
      lifetime = ServiceLifetime.Transient;
      return false;
    }

    if (methodName.Contains("Singleton", StringComparison.Ordinal))
      lifetime = ServiceLifetime.Singleton;
    else if (methodName.Contains("Scoped", StringComparison.Ordinal))
      lifetime = ServiceLifetime.Scoped;
    else
      lifetime = ServiceLifetime.Transient;

    return true;
  }
}
