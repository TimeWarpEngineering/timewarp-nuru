// Builds and analyzes dependency graphs for service resolution.
// Implements topological sorting (Kahn's algorithm) and circular dependency detection (DFS).

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Builds and analyzes dependency graphs for service resolution.
/// Provides topological sorting and circular dependency detection.
/// </summary>
internal static class DependencyGraphBuilder
{
  /// <summary>
  /// Sorts services in dependency order (dependencies first).
  /// Uses Kahn's algorithm for deterministic output.
  /// </summary>
  /// <param name="services">Services to sort.</param>
  /// <returns>Services sorted in dependency order.</returns>
  public static ImmutableArray<ServiceDefinition> TopologicalSort(
    ImmutableArray<ServiceDefinition> services)
  {
    if (services.IsDefaultOrEmpty || services.Length == 1)
      return services;

    // Build lookup: implementation type name → service definition
    Dictionary<string, ServiceDefinition> serviceByImpl = new(StringComparer.Ordinal);
    foreach (ServiceDefinition service in services)
    {
      serviceByImpl[NormalizeTypeName(service.ImplementationTypeName)] = service;
    }

    // Build adjacency list and in-degree count
    Dictionary<string, HashSet<string>> adjacencyList = new(StringComparer.Ordinal);
    Dictionary<string, int> inDegree = new(StringComparer.Ordinal);

    foreach (ServiceDefinition service in services)
    {
      string implName = NormalizeTypeName(service.ImplementationTypeName);
      if (!adjacencyList.ContainsKey(implName))
        adjacencyList[implName] = new HashSet<string>(StringComparer.Ordinal);
      if (!inDegree.ContainsKey(implName))
        inDegree[implName] = 0;

      // Process constructor dependencies
      if (!service.ConstructorDependencyTypes.IsDefaultOrEmpty)
      {
        foreach (string depType in service.ConstructorDependencyTypes)
        {
          string normalizedDep = NormalizeTypeName(depType);

          // Skip built-in types (always available)
          if (IsBuiltInType(normalizedDep))
            continue;

          // Check if dependency is a registered service
          if (serviceByImpl.TryGetValue(normalizedDep, out ServiceDefinition? depService) ||
              TryFindServiceByServiceType(services, normalizedDep, out depService))
          {
            string depImplName = NormalizeTypeName(depService!.ImplementationTypeName);

            // Add edge: dep → service (dep must be instantiated first)
            if (!adjacencyList.ContainsKey(depImplName))
              adjacencyList[depImplName] = new HashSet<string>(StringComparer.Ordinal);

            if (adjacencyList[depImplName].Add(implName))
            {
              inDegree[implName] = inDegree.GetValueOrDefault(implName, 0) + 1;
            }
          }
        }
      }
    }

    // Kahn's algorithm
    Queue<string> queue = new();
    List<ServiceDefinition> result = new(services.Length);

    // Start with nodes that have no dependencies (in-degree = 0)
    foreach (KeyValuePair<string, int> kvp in inDegree)
    {
      if (kvp.Value == 0)
        queue.Enqueue(kvp.Key);
    }

    while (queue.Count > 0)
    {
      string current = queue.Dequeue();
      if (serviceByImpl.TryGetValue(current, out ServiceDefinition? service))
      {
        result.Add(service);
      }

      if (adjacencyList.TryGetValue(current, out HashSet<string>? neighbors))
      {
        foreach (string neighbor in neighbors)
        {
          inDegree[neighbor]--;
          if (inDegree[neighbor] == 0)
            queue.Enqueue(neighbor);
        }
      }
    }

    // If not all nodes processed, there's a cycle - return original order
    if (result.Count != services.Length)
      return services;

    return result.ToImmutableArray();
  }

  /// <summary>
  /// Detects circular dependencies in the service graph.
  /// Returns service type names involved in cycles.
  /// </summary>
  /// <param name="services">Services to analyze.</param>
  /// <returns>Type names involved in circular dependencies.</returns>
  public static ImmutableArray<string> DetectCircularDependencies(
    ImmutableArray<ServiceDefinition> services)
  {
    if (services.IsDefaultOrEmpty)
      return [];

    // Build lookup: implementation type name → service definition
    Dictionary<string, ServiceDefinition> serviceByImpl = new(StringComparer.Ordinal);
    foreach (ServiceDefinition service in services)
    {
      serviceByImpl[NormalizeTypeName(service.ImplementationTypeName)] = service;
    }

    // Build adjacency list
    Dictionary<string, List<string>> graph = new(StringComparer.Ordinal);
    foreach (ServiceDefinition service in services)
    {
      string implName = NormalizeTypeName(service.ImplementationTypeName);
      if (!graph.ContainsKey(implName))
        graph[implName] = new List<string>();

      if (!service.ConstructorDependencyTypes.IsDefaultOrEmpty)
      {
        foreach (string depType in service.ConstructorDependencyTypes)
        {
          string normalizedDep = NormalizeTypeName(depType);

          if (IsBuiltInType(normalizedDep))
            continue;

          if (serviceByImpl.TryGetValue(normalizedDep, out ServiceDefinition? depService) ||
              TryFindServiceByServiceType(services, normalizedDep, out depService))
          {
            string depImplName = NormalizeTypeName(depService!.ImplementationTypeName);
            graph[implName].Add(depImplName);
          }
        }
      }
    }

    // DFS cycle detection
    HashSet<string> visited = new(StringComparer.Ordinal);
    HashSet<string> recursionStack = new(StringComparer.Ordinal);
    HashSet<string> cycleNodes = new(StringComparer.Ordinal);

    foreach (string node in graph.Keys)
    {
      if (!visited.Contains(node))
      {
        DetectCyclesDFS(node, graph, visited, recursionStack, cycleNodes);
      }
    }

    return cycleNodes.ToImmutableArray();
  }

  /// <summary>
  /// DFS helper for cycle detection.
  /// </summary>
  private static void DetectCyclesDFS(
    string node,
    Dictionary<string, List<string>> graph,
    HashSet<string> visited,
    HashSet<string> recursionStack,
    HashSet<string> cycleNodes)
  {
    visited.Add(node);
    recursionStack.Add(node);

    if (graph.TryGetValue(node, out List<string>? neighbors))
    {
      foreach (string neighbor in neighbors)
      {
        if (!visited.Contains(neighbor))
        {
          DetectCyclesDFS(neighbor, graph, visited, recursionStack, cycleNodes);
        }
        else if (recursionStack.Contains(neighbor))
        {
          // Found a cycle - mark this node
          cycleNodes.Add(node);
        }
      }
    }

    recursionStack.Remove(node);
  }

  /// <summary>
  /// Checks for lifetime mismatches (Singleton/Scoped depending on Transient).
  /// Returns pairs of (dependent service, dependency service) that have lifetime issues.
  /// </summary>
  /// <param name="services">Services to analyze.</param>
  /// <returns>Pairs of service type names with lifetime mismatches.</returns>
  public static ImmutableArray<(string DependentService, string DependencyService, string DependentLifetime, string DependencyLifetime)> DetectLifetimeMismatches(
    ImmutableArray<ServiceDefinition> services)
  {
    if (services.IsDefaultOrEmpty)
      return [];

    List<(string, string, string, string)> mismatches = new();

    // Build lookup
    Dictionary<string, ServiceDefinition> serviceByImpl = new(StringComparer.Ordinal);
    foreach (ServiceDefinition service in services)
    {
      serviceByImpl[NormalizeTypeName(service.ImplementationTypeName)] = service;
    }

    foreach (ServiceDefinition service in services)
    {
      if (service.ConstructorDependencyTypes.IsDefaultOrEmpty)
        continue;

      // Only check Singleton and Scoped services
      if (service.Lifetime == ServiceLifetime.Transient)
        continue;

      foreach (string depType in service.ConstructorDependencyTypes)
      {
        string normalizedDep = NormalizeTypeName(depType);

        if (IsBuiltInType(normalizedDep))
          continue;

        if (serviceByImpl.TryGetValue(normalizedDep, out ServiceDefinition? depService) ||
            TryFindServiceByServiceType(services, normalizedDep, out depService))
        {
          // Check if dependency is Transient
          if (depService!.Lifetime == ServiceLifetime.Transient)
          {
            mismatches.Add((
              service.ShortImplementationTypeName,
              depService.ShortImplementationTypeName,
              service.Lifetime.ToString(),
              depService.Lifetime.ToString()));
          }
        }
      }
    }

    return mismatches.ToImmutableArray();
  }

  /// <summary>
  /// Normalizes a type name by removing global:: prefix.
  /// </summary>
  private static string NormalizeTypeName(string typeName)
  {
    return typeName.StartsWith("global::", StringComparison.Ordinal)
      ? typeName[8..]
      : typeName;
  }

  /// <summary>
  /// Checks if a type is a built-in service type (always available).
  /// </summary>
  private static bool IsBuiltInType(string normalizedTypeName)
  {
    return normalizedTypeName.StartsWith("Microsoft.Extensions.Configuration.", StringComparison.Ordinal)
        || normalizedTypeName.StartsWith("Microsoft.Extensions.Logging.", StringComparison.Ordinal)
        || normalizedTypeName.StartsWith("TimeWarp.Terminal.", StringComparison.Ordinal)
        || normalizedTypeName.StartsWith("TimeWarp.Nuru.NuruApp", StringComparison.Ordinal)
        || normalizedTypeName == "System.Threading.CancellationToken"
        || normalizedTypeName.StartsWith("Microsoft.Extensions.Options.IOptions", StringComparison.Ordinal)
        || normalizedTypeName.StartsWith("Microsoft.Extensions.Options.IOptionsSnapshot", StringComparison.Ordinal)
        || normalizedTypeName.StartsWith("Microsoft.Extensions.Options.IOptionsMonitor", StringComparison.Ordinal);
  }

  /// <summary>
  /// Tries to find a service by its service type name (interface).
  /// </summary>
  private static bool TryFindServiceByServiceType(
    ImmutableArray<ServiceDefinition> services,
    string serviceTypeName,
    out ServiceDefinition? service)
  {
    service = null;
    string normalized = NormalizeTypeName(serviceTypeName);

    foreach (ServiceDefinition s in services)
    {
      if (NormalizeTypeName(s.ServiceTypeName) == normalized)
      {
        service = s;
        return true;
      }
    }

    return false;
  }
}
