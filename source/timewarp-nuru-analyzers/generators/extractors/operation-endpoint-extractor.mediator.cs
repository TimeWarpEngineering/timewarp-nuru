#region Purpose
// Mediator handler/renderer scanning and response shape discovery for [Operation] endpoints (EndpointExtractor partial).
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  private static ResponseShape DescribeResponse(Compilation compilation, INamedTypeSymbol requestType)
  {
    string commandTypeName = requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    ITypeSymbol? responseType = MediatorResponseType(requestType);
    string responseTypeName = responseType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
      ?? "global::TimeWarp.Mediator.Unit";

    bool problem = false;
    string successTypeName = responseTypeName;
    if (responseType is INamedTypeSymbol oneOf
        && oneOf.Name == "OneOf"
        && oneOf.ContainingNamespace?.ToDisplayString() == "OneOf"
        && oneOf.TypeArguments.Length == 2
        && oneOf.TypeArguments[1] is INamedTypeSymbol problemType
        && problemType.Name == "SharedProblemDetails"
        && problemType.ContainingNamespace?.ToDisplayString() == "TimeWarp.Foundation.Types")
    {
      problem = true;
      successTypeName = oneOf.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    // TimeWarp.Mediator 15 has one handler interface for every message kind. The kind comes from the
    // message marker (IQuery<T>, ICommand<T>, IIdempotentCommand<T>), not from the handler.
    string handlerInterface = $"global::TimeWarp.Mediator.IRequestHandler<{commandTypeName}, {responseTypeName}>";

    string? renderer = FindRenderer(compilation, successTypeName, requestType);
    string? problemRenderer = problem ? FindRenderer(compilation, ProblemDetailsMetadataTypeName, requestType) : null;
    return new ResponseShape(problem, successTypeName, renderer, handlerInterface, problemRenderer);
  }

  private static ITypeSymbol? MediatorResponseType(INamedTypeSymbol requestType)
  {
    ITypeSymbol? idempotent = null;
    ITypeSymbol? command = null;
    foreach (INamedTypeSymbol iface in requestType.AllInterfaces)
    {
      if (!iface.IsGenericType || !IsMediatorInterface(iface) || iface.TypeArguments.Length == 0)
        continue;

      if (iface.Name == "IQuery")
        return iface.TypeArguments[0];
      if (iface.Name == "IIdempotentCommand")
        idempotent = iface.TypeArguments[0];
      else if (iface.Name == "ICommand")
        command = iface.TypeArguments[0];
    }

    return idempotent ?? command;
  }

  private sealed class MediatorScan
  {
    public required List<(INamedTypeSymbol Handler, INamedTypeSymbol Request)> Handlers { get; init; }

    public required List<(INamedTypeSymbol Renderer, string SuccessTypeName, bool SameAssembly)> Renderers { get; init; }
  }

  private static MediatorScan ScanMediator(Compilation compilation) =>
    MediatorScans.GetValue(compilation, static compilation => BuildMediatorScan(compilation));

  private static MediatorScan BuildMediatorScan(Compilation compilation)
  {
    List<(INamedTypeSymbol Handler, INamedTypeSymbol Request)> handlers = [];
    List<(INamedTypeSymbol Renderer, string SuccessTypeName, bool SameAssembly)> renderers = [];
    CollectMediatorTypes(compilation.Assembly.GlobalNamespace, publicOnly: false, sameAssembly: true, handlers, renderers);
    foreach (MetadataReference reference in compilation.References)
    {
      if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
        continue;

      if (IsFrameworkAssembly(assembly.Name))
        continue;

      CollectMediatorTypes(assembly.GlobalNamespace, publicOnly: true, sameAssembly: false, handlers, renderers);
    }

    return new MediatorScan { Handlers = handlers, Renderers = renderers };
  }

  private static bool IsFrameworkAssembly(string? name) =>
    string.IsNullOrEmpty(name)
    || name.StartsWith("System", StringComparison.Ordinal)
    || name.StartsWith("Microsoft", StringComparison.Ordinal)
    || name is "mscorlib" or "netstandard";

  private static void CollectMediatorTypes(
    INamespaceSymbol ns,
    bool publicOnly,
    bool sameAssembly,
    List<(INamedTypeSymbol Handler, INamedTypeSymbol Request)> handlers,
    List<(INamedTypeSymbol Renderer, string SuccessTypeName, bool SameAssembly)> renderers)
  {
    foreach (INamedTypeSymbol type in ns.GetTypeMembers())
      CollectMediatorType(type, publicOnly, sameAssembly, handlers, renderers);

    foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
      CollectMediatorTypes(child, publicOnly, sameAssembly, handlers, renderers);
  }

  private static void CollectMediatorType(
    INamedTypeSymbol type,
    bool publicOnly,
    bool sameAssembly,
    List<(INamedTypeSymbol Handler, INamedTypeSymbol Request)> handlers,
    List<(INamedTypeSymbol Renderer, string SuccessTypeName, bool SameAssembly)> renderers)
  {
    if (IsAccessibleHandler(type, publicOnly))
    {
      foreach (INamedTypeSymbol iface in type.AllInterfaces)
      {
        if (iface.ContainingNamespace?.ToDisplayString() != MediatorNamespace)
          continue;

        if (!IsRequestHandlerInterface(iface))
          continue;

        if (iface.TypeArguments.Length > 0 && iface.TypeArguments[0] is INamedTypeSymbol request)
          handlers.Add((type, request));
      }
    }

    foreach (string successTypeName in RendererSuccessTypes(type, publicOnly))
      renderers.Add((type, successTypeName, sameAssembly));

    foreach (INamedTypeSymbol nested in type.GetTypeMembers())
      CollectMediatorType(nested, publicOnly, sameAssembly, handlers, renderers);
  }

  private static bool HasVisibleHandler(Compilation compilation, INamedTypeSymbol requestType)
  {
    INamedTypeSymbol? nestedHandler = FindNestedHandler(compilation, requestType);
    if (nestedHandler is not null && ImplementsRequestHandler(nestedHandler, requestType))
      return true;

    foreach ((INamedTypeSymbol _, INamedTypeSymbol request) in ScanMediator(compilation).Handlers)
    {
      if (SymbolEqualityComparer.Default.Equals(request, requestType))
        return true;
    }

    return false;
  }

  /// <summary>
  /// The nested <c>Handler</c> type, when generated code in <paramref name="compilation"/> can name it.
  /// A non-public Handler nested in a referenced-assembly type is not visible.
  /// </summary>
  private static INamedTypeSymbol? FindNestedHandler(Compilation compilation, INamedTypeSymbol requestType)
  {
    INamedTypeSymbol? nestedHandler = requestType.GetTypeMembers("Handler").FirstOrDefault();
    if (nestedHandler is null)
      return null;

    return IsAccessibleFrom(compilation, nestedHandler) ? nestedHandler : null;
  }

  /// <summary>
  /// True when generated code in <paramref name="compilation"/> can use <paramref name="symbol"/>.
  /// A same-assembly symbol keeps the existing internal rule. A referenced-assembly symbol must be
  /// accessible, which honors [InternalsVisibleTo].
  /// </summary>
  internal static bool IsAccessibleFrom(Compilation? compilation, ISymbol symbol)
  {
    if (compilation is null || IsDeclaredIn(compilation, symbol))
      return true;

    return compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly);
  }

  /// <summary>
  /// True when <paramref name="symbol"/> is declared in <paramref name="compilation"/> itself,
  /// not in a referenced assembly.
  /// </summary>
  internal static bool IsDeclaredIn(Compilation compilation, ISymbol symbol) =>
    SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, compilation.Assembly);

  private static bool IsAccessibleHandler(INamedTypeSymbol type, bool publicOnly)
  {
    if (type.TypeKind != TypeKind.Class || type.IsAbstract)
      return false;

    if (publicOnly)
      return type.DeclaredAccessibility == Accessibility.Public;

    return type.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
  }

  /// <summary>
  /// True for <c>TimeWarp.Mediator.IRequestHandler&lt;TRequest, TResponse&gt;</c> and
  /// <c>IRequestHandler&lt;TRequest&gt;</c>. The caller has already checked the namespace.
  /// </summary>
  private static bool IsRequestHandlerInterface(INamedTypeSymbol iface) =>
    iface.Name == "IRequestHandler" && iface.TypeArguments.Length is 1 or 2;

  private static bool ImplementsRequestHandler(INamedTypeSymbol type, INamedTypeSymbol requestType)
  {
    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (iface.ContainingNamespace?.ToDisplayString() != MediatorNamespace)
        continue;

      if (!IsRequestHandlerInterface(iface))
        continue;

      if (iface.TypeArguments.Length > 0
          && SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], requestType))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// The one visible <c>IResponseRenderer&lt;SharedProblemDetails&gt;</c> in <paramref name="compilation"/>, by the
  /// same rule a problem pair uses, or null. Bind failures and escaped exceptions render through it (508-018).
  /// </summary>
  internal static string? FindProblemRenderer(Compilation compilation) =>
    FindRenderer(compilation, ProblemDetailsMetadataTypeName, requestType: null);

  private static string? FindRenderer(Compilation? compilation, string successTypeName, INamedTypeSymbol? requestType)
  {
    _ = requestType;
    if (compilation is null)
      return null;

    List<INamedTypeSymbol> sameAssembly = [];
    List<INamedTypeSymbol> referenced = [];
    foreach ((INamedTypeSymbol renderer, string success, bool same) in ScanMediator(compilation).Renderers)
    {
      if (success != successTypeName)
        continue;

      if (same)
        sameAssembly.Add(renderer);
      else
        referenced.Add(renderer);
    }

    if (sameAssembly.Count == 1)
      return sameAssembly[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    if (sameAssembly.Count > 1)
      return null;

    if (referenced.Count == 1 && HasParameterlessConstructor(referenced[0]))
      return referenced[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    return null;
  }

  private static List<string> RendererSuccessTypes(INamedTypeSymbol type, bool publicOnly)
  {
    List<string> successTypes = [];
    if (type.TypeKind != TypeKind.Class || type.IsAbstract || !HasParameterlessConstructor(type))
      return successTypes;

    if (publicOnly && type.DeclaredAccessibility != Accessibility.Public)
      return successTypes;

    if (!publicOnly && type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
      return successTypes;

    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (iface.Name != "IResponseRenderer" || !iface.IsGenericType || iface.TypeArguments.Length != 1)
        continue;

      if (iface.ContainingNamespace?.ToDisplayString() != "TimeWarp.Contracts")
        continue;

      successTypes.Add(iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    return successTypes;
  }
}
