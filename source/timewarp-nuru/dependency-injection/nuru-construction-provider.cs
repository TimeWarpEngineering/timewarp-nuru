namespace TimeWarp.Nuru;

/// <summary>
/// Marks a service type registered with <c>AddSessionScoped</c> so command construction
/// resolves it from the root provider. A REPL command scope must not dispose it.
/// </summary>
internal sealed class NuruSessionServiceType
{
  public NuruSessionServiceType(Type serviceType)
  {
    ArgumentNullException.ThrowIfNull(serviceType);
    ServiceType = serviceType;
  }

  public Type ServiceType { get; }
}

/// <summary>
/// Forwards session-scoped services to the root provider and everything else to the command provider.
/// </summary>
internal sealed class NuruConstructionProvider : IServiceProvider
{
  private readonly IServiceProvider Root;
  private readonly IServiceProvider Inner;
  private HashSet<Type>? SessionTypes;

  public NuruConstructionProvider(IServiceProvider root, IServiceProvider inner)
  {
    ArgumentNullException.ThrowIfNull(root);
    ArgumentNullException.ThrowIfNull(inner);
    Root = root;
    Inner = inner;
  }

  public object? GetService(Type serviceType)
  {
    ArgumentNullException.ThrowIfNull(serviceType);

    SessionTypes ??=
    [
      .. Root.GetServices<NuruSessionServiceType>().Select(static marker => marker.ServiceType)
    ];

    if (SessionTypes.Contains(serviceType))
      return Root.GetService(serviceType);

    return Inner.GetService(serviceType);
  }
}
