namespace TimeWarp.Nuru;

/// <summary>
/// Registers session-scoped and command-scoped services.
/// <c>AddSessionScoped</c> keeps one instance for a REPL session (or for one CLI invocation) and disposes it
/// when that scope ends. <c>AddCommandScoped</c> keeps one instance per command.
/// In the REPL, <c>AddScoped</c> follows command scope. A single CLI invocation still treats <c>AddScoped</c>
/// as one instance for the process, matching the source-generated resolver.
/// </summary>
public static class NuruServiceCollectionExtensions
{
  /// <summary>
  /// Adds a session-scoped service of the type specified in <typeparamref name="TService"/> with an
  /// implementation type specified in <typeparamref name="TImplementation"/>.
  /// </summary>
  public static IServiceCollection AddSessionScoped<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>
  (
    this IServiceCollection services
  )
    where TService : class
    where TImplementation : class, TService
  {
    ArgumentNullException.ThrowIfNull(services);
    return AddSessionCached<TService>
    (
      services,
      static root => ActivatorUtilities.CreateInstance<TImplementation>(root)
    );
  }

  /// <summary>
  /// Adds a session-scoped service of the type specified in <typeparamref name="TService"/> with a
  /// factory specified in <paramref name="implementationFactory"/>.
  /// </summary>
  public static IServiceCollection AddSessionScoped<TService>
  (
    this IServiceCollection services,
    Func<IServiceProvider, TService> implementationFactory
  )
    where TService : class
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(implementationFactory);
    return AddSessionCached(services, implementationFactory);
  }

  /// <summary>
  /// Adds a session-scoped service of the type specified in <typeparamref name="TService"/> with a
  /// factory that returns <typeparamref name="TImplementation"/>.
  /// </summary>
  public static IServiceCollection AddSessionScoped<TService, TImplementation>
  (
    this IServiceCollection services,
    Func<IServiceProvider, TImplementation> implementationFactory
  )
    where TService : class
    where TImplementation : class, TService
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(implementationFactory);
    return AddSessionCached<TService>(services, implementationFactory);
  }

  /// <summary>
  /// Adds a session-scoped service of the type specified in <typeparamref name="TService"/>.
  /// </summary>
  public static IServiceCollection AddSessionScoped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>
  (
    this IServiceCollection services
  )
    where TService : class
  {
    ArgumentNullException.ThrowIfNull(services);
    return AddSessionScoped<TService, TService>(services);
  }

  /// <summary>
  /// Adds a session-scoped service of the type specified in <paramref name="serviceType"/> with an
  /// implementation of type <paramref name="implementationType"/>.
  /// </summary>
  public static IServiceCollection AddSessionScoped
  (
    this IServiceCollection services,
    Type serviceType,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType
  )
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(serviceType);
    ArgumentNullException.ThrowIfNull(implementationType);
    services.TryAddSingleton<NuruSessionCache>();
    services.AddSingleton(new NuruSessionServiceType(serviceType));
    ObjectFactory objectFactory = ActivatorUtilities.CreateFactory(implementationType, Type.EmptyTypes);
    services.AddTransient
    (
      serviceType,
      serviceProvider =>
      {
        NuruSessionCache cache = serviceProvider.GetRequiredService<NuruSessionCache>();
        return cache.GetOrCreate(serviceType, () => objectFactory(cache.Root, null)!);
      }
    );
    return services;
  }

  /// <summary>
  /// Adds a session-scoped service of the type specified in <paramref name="serviceType"/> with a factory.
  /// </summary>
  public static IServiceCollection AddSessionScoped
  (
    this IServiceCollection services,
    Type serviceType,
    Func<IServiceProvider, object> implementationFactory
  )
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(serviceType);
    ArgumentNullException.ThrowIfNull(implementationFactory);
    services.TryAddSingleton<NuruSessionCache>();
    services.AddSingleton(new NuruSessionServiceType(serviceType));
    services.AddTransient
    (
      serviceType,
      serviceProvider =>
      {
        NuruSessionCache cache = serviceProvider.GetRequiredService<NuruSessionCache>();
        return cache.GetOrCreate(serviceType, () => implementationFactory(cache.Root));
      }
    );
    return services;
  }

  /// <summary>
  /// Adds a session-scoped service using <paramref name="implementationInstance"/> as the instance
  /// for the current session.
  /// </summary>
  public static IServiceCollection AddSessionScoped<TService>
  (
    this IServiceCollection services,
    TService implementationInstance
  )
    where TService : class
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(implementationInstance);
    return AddSessionCached<TService>(services, _ => implementationInstance);
  }

  /// <summary>
  /// Adds a command-scoped service of the type specified in <typeparamref name="TService"/> with an
  /// implementation type specified in <typeparamref name="TImplementation"/>.
  /// </summary>
  public static IServiceCollection AddCommandScoped<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>
  (
    this IServiceCollection services
  )
    where TService : class
    where TImplementation : class, TService
  {
    ArgumentNullException.ThrowIfNull(services);
    return AddCommandCached<TService>
    (
      services,
      static provider => ActivatorUtilities.CreateInstance<TImplementation>(provider)
    );
  }

  /// <summary>
  /// Adds a command-scoped service of the type specified in <typeparamref name="TService"/> with a factory.
  /// </summary>
  public static IServiceCollection AddCommandScoped<TService>
  (
    this IServiceCollection services,
    Func<IServiceProvider, TService> implementationFactory
  )
    where TService : class
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(implementationFactory);
    return AddCommandCached(services, implementationFactory);
  }

  /// <summary>
  /// Adds a command-scoped service of the type specified in <typeparamref name="TService"/> with a
  /// factory that returns <typeparamref name="TImplementation"/>.
  /// </summary>
  public static IServiceCollection AddCommandScoped<TService, TImplementation>
  (
    this IServiceCollection services,
    Func<IServiceProvider, TImplementation> implementationFactory
  )
    where TService : class
    where TImplementation : class, TService
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(implementationFactory);
    return AddCommandCached<TService>(services, implementationFactory);
  }

  /// <summary>
  /// Adds a command-scoped service of the type specified in <typeparamref name="TService"/>.
  /// </summary>
  public static IServiceCollection AddCommandScoped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>
  (
    this IServiceCollection services
  )
    where TService : class
  {
    ArgumentNullException.ThrowIfNull(services);
    return AddCommandScoped<TService, TService>(services);
  }

  /// <summary>
  /// Adds a command-scoped service of the type specified in <paramref name="serviceType"/> with an
  /// implementation of type <paramref name="implementationType"/>.
  /// </summary>
  public static IServiceCollection AddCommandScoped
  (
    this IServiceCollection services,
    Type serviceType,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType
  )
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(serviceType);
    ArgumentNullException.ThrowIfNull(implementationType);
    services.TryAddSingleton<NuruCommandCache>();
    ObjectFactory objectFactory = ActivatorUtilities.CreateFactory(implementationType, Type.EmptyTypes);
    services.AddTransient
    (
      serviceType,
      serviceProvider =>
      {
        NuruCommandCache cache = serviceProvider.GetRequiredService<NuruCommandCache>();
        return cache.GetOrCreate(serviceType, serviceProvider, provider => objectFactory(provider, null)!);
      }
    );
    return services;
  }

  /// <summary>
  /// Adds a command-scoped service of the type specified in <paramref name="serviceType"/> with a factory.
  /// </summary>
  public static IServiceCollection AddCommandScoped
  (
    this IServiceCollection services,
    Type serviceType,
    Func<IServiceProvider, object> implementationFactory
  )
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(serviceType);
    ArgumentNullException.ThrowIfNull(implementationFactory);
    services.TryAddSingleton<NuruCommandCache>();
    services.AddTransient
    (
      serviceType,
      serviceProvider =>
      {
        NuruCommandCache cache = serviceProvider.GetRequiredService<NuruCommandCache>();
        return cache.GetOrCreate(serviceType, serviceProvider, implementationFactory);
      }
    );
    return services;
  }

  /// <summary>
  /// Adds <typeparamref name="TImplementation"/> as a session-scoped <typeparamref name="TService"/>
  /// when <typeparamref name="TService"/> is not already registered.
  /// </summary>
  public static void TryAddSessionScoped<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>
  (
    this IServiceCollection services
  )
    where TService : class
    where TImplementation : class, TService
  {
    ArgumentNullException.ThrowIfNull(services);
    if (services.Any(static descriptor => descriptor.ServiceType == typeof(TService)))
      return;

    services.AddSessionScoped<TService, TImplementation>();
  }

  /// <summary>
  /// Adds <typeparamref name="TService"/> as session-scoped when it is not already registered.
  /// </summary>
  public static void TryAddSessionScoped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>
  (
    this IServiceCollection services
  )
    where TService : class
  {
    ArgumentNullException.ThrowIfNull(services);
    services.TryAddSessionScoped<TService, TService>();
  }

  /// <summary>
  /// Adds <typeparamref name="TImplementation"/> as a command-scoped <typeparamref name="TService"/>
  /// when <typeparamref name="TService"/> is not already registered.
  /// </summary>
  public static void TryAddCommandScoped<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>
  (
    this IServiceCollection services
  )
    where TService : class
    where TImplementation : class, TService
  {
    ArgumentNullException.ThrowIfNull(services);
    if (services.Any(static descriptor => descriptor.ServiceType == typeof(TService)))
      return;

    services.AddCommandScoped<TService, TImplementation>();
  }

  /// <summary>
  /// Adds <typeparamref name="TService"/> as command-scoped when it is not already registered.
  /// </summary>
  public static void TryAddCommandScoped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>
  (
    this IServiceCollection services
  )
    where TService : class
  {
    ArgumentNullException.ThrowIfNull(services);
    services.TryAddCommandScoped<TService, TService>();
  }

  private static IServiceCollection AddSessionCached<TService>
  (
    IServiceCollection services,
    Func<IServiceProvider, TService> implementationFactory
  )
    where TService : class
  {
    services.TryAddSingleton<NuruSessionCache>();
    services.AddSingleton(new NuruSessionServiceType(typeof(TService)));
    services.AddTransient<TService>
    (
      serviceProvider =>
      {
        NuruSessionCache cache = serviceProvider.GetRequiredService<NuruSessionCache>();
        return cache.GetOrCreate(typeof(TService), () => implementationFactory(cache.Root));
      }
    );
    return services;
  }

  private static IServiceCollection AddCommandCached<TService>
  (
    IServiceCollection services,
    Func<IServiceProvider, TService> implementationFactory
  )
    where TService : class
  {
    services.TryAddSingleton<NuruCommandCache>();
    services.AddTransient<TService>
    (
      serviceProvider =>
      {
        NuruCommandCache cache = serviceProvider.GetRequiredService<NuruCommandCache>();
        return cache.GetOrCreate(typeof(TService), serviceProvider, implementationFactory);
      }
    );
    return services;
  }
}
