// Emits handler invocation code based on handler kind.
// Supports delegate, command, and method-based handlers.
// For delegate handlers, transforms lambda bodies into local functions.
// For command handlers, instantiates nested Handler class and calls Handle().

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Determines how handler results should be output to the terminal.
/// </summary>
internal enum OutputStrategy
{
  /// <summary>No output (Unit, void, null).</summary>
  None,
  /// <summary>Raw ToString() output (primitives, strings).</summary>
  Raw,
  /// <summary>ISO 8601 formatted output (DateTime, DateTimeOffset, DateOnly, TimeOnly).</summary>
  Iso8601,
  /// <summary>Culture-invariant formatted output (TimeSpan).</summary>
  Invariant,
  /// <summary>JSON serialized output (complex objects, collections).</summary>
  Json
}

/// <summary>
/// Emits code to invoke a route's handler.
/// Handles different handler kinds: delegate, command, and method.
/// </summary>
internal static class HandlerInvokerEmitter
{
  /// <summary>
  /// Emits handler invocation code for a route.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route containing the handler to invoke.</param>
  /// <param name="routeIndex">The index of this route (used for unique local function names).</param>
  /// <param name="services">Registered services from ConfigureServices.</param>
  /// <param name="indent">Number of spaces for indentation.</param>
  /// <param name="commandAlreadyCreated">If true, skip command creation (already done by behavior emitter).</param>
  /// <param name="loggerFactoryFieldName">Logger factory source (static field name or "app.LoggerFactory"), or null if no logging.</param>
  /// <param name="useRuntimeDI">When true, uses GetServiceProvider().GetRequiredService&lt;T&gt;() instead of static instantiation.</param>
  /// <param name="runtimeDISuffix">Suffix for per-app runtime DI methods (e.g., "_0" for multi-app assemblies).</param>
  /// <param name="captureResultInto">
  /// When set, the handler result is assigned to this <c>object?</c> expression instead of being written
  /// to the terminal. <c>NuruApp.InvokeAsync</c> uses it; <see cref="EmitRender"/> writes the text later.
  /// </param>
  /// <param name="verbatimRendererOutput">When true (<c>UseVerbatimRendererOutput()</c>), renderer text is written with no added newline.</param>
  public static void Emit(StringBuilder sb, RouteDefinition route, int routeIndex, ImmutableArray<ServiceDefinition> services, int indent = 6, bool commandAlreadyCreated = false, string? loggerFactoryFieldName = null, bool useRuntimeDI = false, string runtimeDISuffix = "", ImmutableArray<HttpClientConfiguration> httpClientConfigurations = default, bool useDirectInvoke = false, bool hasGeneratedMediator = false, bool jsonArgsPath = false, string? captureResultInto = null, bool verbatimRendererOutput = false)
  {
    string indentStr = new(' ', indent);
    HandlerDefinition handler = route.Handler;

    // First, resolve any required services
    if (handler.RequiresServiceProvider)
    {
      ServiceResolverEmitter.Emit(sb, handler, services, indent, useRuntimeDI, runtimeDISuffix);
    }

    // Emit handler invocation based on kind
    switch (handler.HandlerKind)
    {
      case HandlerKind.Delegate:
        EmitDelegateInvocation(sb, route, routeIndex, indentStr, captureResultInto);
        break;

      case HandlerKind.Command:
        EmitCommandInvocation(sb, route, routeIndex, services, indentStr, commandAlreadyCreated, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix, httpClientConfigurations, useDirectInvoke, hasGeneratedMediator, jsonArgsPath, captureResultInto, verbatimRendererOutput);
        break;

      case HandlerKind.Method:
        EmitMethodInvocation(sb, route, indentStr, captureResultInto);
        break;
    }
  }

  /// <summary>
  /// Emits invocation for a delegate-based handler.
  /// Transforms the lambda into a local function and invokes it.
  /// </summary>
  private static void EmitDelegateInvocation(StringBuilder sb, RouteDefinition route, int routeIndex, string indent, string? captureResultInto)
  {
    HandlerDefinition handler = route.Handler;

    // If no lambda body was captured, emit an error comment
    if (string.IsNullOrEmpty(handler.LambdaBodySource))
    {
      sb.AppendLine(
        $"{indent}// ERROR: Lambda body was not captured for this handler");
      sb.AppendLine(
        $"{indent}throw new System.NotSupportedException(\"Handler code was not captured at compile time.\");");
      return;
    }

    string handlerName = $"__handler_{routeIndex}";

    if (handler.IsExpressionBody)
    {
      // Expression body: () => "pong" or (string name) => $"Hello {name}"
      EmitExpressionBodyHandler(sb, route, routeIndex, handler, handlerName, indent, captureResultInto);
    }
    else
    {
      // Block body: () => { DoWork(); return "done"; }
      EmitBlockBodyHandler(sb, route, routeIndex, handler, handlerName, indent, captureResultInto);
    }
  }

  /// <summary>
  /// Emits a local function for an expression-body lambda.
  /// </summary>
  private static void EmitExpressionBodyHandler(StringBuilder sb, RouteDefinition route, int routeIndex, HandlerDefinition handler, string handlerName, string indent, string? captureResultInto)
  {
    string returnTypeName = GetReturnTypeName(handler);
    string asyncModifier = handler.IsAsync ? "async " : "";
    string awaitKeyword = handler.IsAsync ? "await " : "";

    // An async lambda's expression body already awaits whatever it needs (e.g. `async s => await s.Send(q)`),
    // so it is emitted verbatim. Only a non-async Task-returning body (e.g. `() => Task.FromResult(1)`) is awaited.
    string bodyAwaitKeyword = handler.IsAsync && !handler.HasAsyncModifier ? "await " : "";

    // Build parameter list for the local function (preserves handler parameter names)
    string paramList = BuildParameterList(handler);
    string argList = BuildArgumentListFromRoute(route, routeIndex, handler);

    if (handler.ReturnType.HasValue)
    {
      // Expression with return value
      // Generate: ReturnType __handler_N(Type param1, ...) => expression;
      sb.AppendLine(
        $"{indent}{asyncModifier}{returnTypeName} {handlerName}({paramList}) => {bodyAwaitKeyword}{handler.LambdaBodySource};");

      // Invoke and capture result
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      sb.AppendLine(
        $"{indent}{GetUnwrappedReturnTypeName(handler)} result = {awaitKeyword}{handlerName}({argList});");

      EmitResultOrCapture(sb, indent, resultTypeName, captureResultInto);
    }
    else
    {
      // Void expression (side-effect only)
      // Generate: void __handler_N(Type param1, ...) => expression;
      if (handler.IsAsync)
      {
        sb.AppendLine(
          $"{indent}async Task {handlerName}({paramList}) => {bodyAwaitKeyword}{handler.LambdaBodySource};");
        sb.AppendLine(
          $"{indent}await {handlerName}({argList});");
      }
      else
      {
        sb.AppendLine(
          $"{indent}void {handlerName}({paramList}) => {handler.LambdaBodySource};");
        sb.AppendLine(
          $"{indent}{handlerName}({argList});");
      }
    }
  }

  /// <summary>
  /// Emits a local function for a block-body lambda.
  /// </summary>
  private static void EmitBlockBodyHandler(StringBuilder sb, RouteDefinition route, int routeIndex, HandlerDefinition handler, string handlerName, string indent, string? captureResultInto)
  {
    string returnTypeName = GetReturnTypeName(handler);
    string asyncModifier = handler.IsAsync ? "async " : "";
    string awaitKeyword = handler.IsAsync ? "await " : "";

    // Build parameter list for the local function (preserves handler parameter names)
    string paramList = BuildParameterList(handler);
    string argList = BuildArgumentListFromRoute(route, routeIndex, handler);

    // Emit the local function signature
    sb.AppendLine(
      $"{indent}{asyncModifier}{returnTypeName} {handlerName}({paramList})");

    // Re-indent the block body to match target indentation
    string reindentedBody = ReindentBlockBody(handler.LambdaBodySource ?? "{}", indent);
    sb.Append(reindentedBody);

    if (handler.ReturnType.HasValue)
    {
      // Invoke and capture result
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      sb.AppendLine(
        $"{indent}{GetUnwrappedReturnTypeName(handler)} result = {awaitKeyword}{handlerName}({argList});");

      EmitResultOrCapture(sb, indent, resultTypeName, captureResultInto);
    }
    else
    {
      // Void - just invoke
      sb.AppendLine(
        $"{indent}{awaitKeyword}{handlerName}({argList});");
    }
  }

  /// <summary>
  /// Re-indents a block body to match the target indentation level.
  /// Strips the original indentation and applies new indentation to each line.
  /// </summary>
  private static string ReindentBlockBody(string blockBody, string targetIndent)
  {
    if (string.IsNullOrEmpty(blockBody))
      return $"{targetIndent}{{}}\n";

    string[] lines = blockBody.Split('\n');
    if (lines.Length == 0)
      return $"{targetIndent}{{}}\n";

    // Find the minimum indentation (excluding empty lines)
    int minIndent = int.MaxValue;
    foreach (string line in lines)
    {
      if (string.IsNullOrWhiteSpace(line))
        continue;

      int lineIndent = 0;
      foreach (char c in line)
      {
        if (c == ' ')
          lineIndent++;
        else if (c == '\t')
          lineIndent += 2; // Treat tab as 2 spaces
        else
          break;
      }

      if (lineIndent < minIndent)
        minIndent = lineIndent;
    }

    if (minIndent == int.MaxValue)
      minIndent = 0;

    // Re-indent each line
    StringBuilder result = new();
    foreach (string line in lines)
    {
      if (string.IsNullOrWhiteSpace(line))
      {
        result.AppendLine();
        continue;
      }

      // Strip original indentation and apply target indentation
      string trimmedLine = line.Length > minIndent ? line[minIndent..] : line.TrimStart();
      result.AppendLine($"{targetIndent}{trimmedLine}");
    }

    return result.ToString();
  }

  /// <summary>
  /// Gets the return type name for the local function signature.
  /// </summary>
  private static string GetReturnTypeName(HandlerDefinition handler)
  {
    if (handler.ReturnType.IsVoid && !handler.IsAsync)
      return "void";

    if (handler.IsAsync)
    {
      if (handler.ReturnType.HasValue)
        return handler.ReturnType.ShortTypeName; // Task<T>
      return "Task";
    }

    return handler.ReturnType.ShortTypeName;
  }

  /// <summary>
  /// Gets the unwrapped return type name (T from Task&lt;T&gt;, or the type itself if not async).
  /// </summary>
  private static string GetUnwrappedReturnTypeName(HandlerDefinition handler)
  {
    if (handler.IsAsync && handler.ReturnType.UnwrappedTypeName is not null)
    {
      string unwrapped = handler.ReturnType.UnwrappedTypeName;

      // Preserve fully qualified name for Unit to avoid ambiguity
      // when other packages (e.g. MediatR) also define a Unit type.
      if (unwrapped == "global::TimeWarp.Mediator.Unit")
        return unwrapped;

      // For Task<T>, extract short name from fully qualified unwrapped type
      int lastDot = unwrapped.LastIndexOf('.');
      return lastDot >= 0 ? unwrapped[(lastDot + 1)..] : unwrapped;
    }

    return handler.ReturnType.ShortTypeName;
  }

  /// <summary>
  /// Emits invocation for a command/query handler class.
  /// Creates the command object (unless already created), resolves handler dependencies, and invokes Handle().
  /// </summary>
  private static void EmitCommandInvocation(StringBuilder sb, RouteDefinition route, int routeIndex, ImmutableArray<ServiceDefinition> services, string indent, bool commandAlreadyCreated = false, string? loggerFactoryFieldName = null, bool useRuntimeDI = false, string runtimeDISuffix = "", ImmutableArray<HttpClientConfiguration> httpClientConfigurations = default, bool useDirectInvoke = false, bool hasGeneratedMediator = false, bool jsonArgsPath = false, string? captureResultInto = null, bool verbatimRendererOutput = false)
  {
    HandlerDefinition handler = route.Handler;
    string commandTypeName = handler.FullTypeName ?? "UnknownCommand";
    string handlerTypeName = handler.NestedHandlerTypeName ?? $"{commandTypeName}.Handler";
    // Nested handlers keep the 3.0 direct call when the app opts out or when no generated mediator exists.
    bool directInvoke = handler.NestedHandlerTypeName is not null
      && (useDirectInvoke || !hasGeneratedMediator);

    // 1. Create the command object with property initializers (skip if already created by behavior emitter)
    if (!commandAlreadyCreated)
      CommandConstructionEmitter.Emit(sb, route, routeIndex, indent, jsonArgsPath);

    if (directInvoke)
    {
      // 2. Resolve handler constructor dependencies
      if (handler.ConstructorDependencies.Length > 0)
      {
        sb.AppendLine($"{indent}// Resolve handler constructor dependencies");
        foreach (ParameterBinding dep in handler.ConstructorDependencies)
        {
          string resolution = ResolveServiceForCommand(dep.ParameterTypeName, services, httpClientConfigurations, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix);
          sb.AppendLine(
            $"{indent}{dep.ParameterTypeName} __{dep.ParameterName} = {resolution};");
        }

        sb.AppendLine();
      }

      // 3. Create handler instance
      sb.AppendLine($"{indent}// Create handler and invoke");
      string constructorArgs = string.Join(", ", handler.ConstructorDependencies.Select(d => $"__{d.ParameterName}"));
      sb.AppendLine(
        $"{indent}{handlerTypeName} __handler = new({constructorArgs});");

      if (handler.ReturnType.HasValue)
      {
        string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
        sb.AppendLine(
          $"{indent}{resultTypeName} result = await __handler.Handle(__command, cancellationToken);");
        EmitCommandResultOrCapture(sb, indent, handler, services, httpClientConfigurations, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix, captureResultInto, verbatimRendererOutput);
      }
      else
      {
        sb.AppendLine(
          $"{indent}await __handler.Handle(__command, cancellationToken);");
      }

      return;
    }

    // No generated mediator: InterceptorEmitter.EmitMissingHandlerOnDispatch already
    // returned 1 before the behavior pipeline. Emit nothing here, so a behavior lambda
    // never holds a `return 1;` and __GetMediator() is never referenced.
    if (!useRuntimeDI && !hasGeneratedMediator)
    {
      sb.AppendLine($"{indent}// Unreachable: exited 1 before dispatch (no generated mediator).");
      return;
    }

    // Mediator dispatch. INuruBehavior already wraps this invocation.
    string mediatorExpression = useRuntimeDI
      ? $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::TimeWarp.Mediator.IMediator>(GetResolutionProvider{runtimeDISuffix}(app))"
      : "__GetMediator()";
    sb.AppendLine($"{indent}global::TimeWarp.Mediator.IMediator __mediator = {mediatorExpression};");
    if (handler.ReturnType.HasValue)
    {
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      sb.AppendLine(
        $"{indent}{resultTypeName} result = await __mediator.Send(__command, cancellationToken);");
      EmitCommandResultOrCapture(sb, indent, handler, services, httpClientConfigurations, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix, captureResultInto, verbatimRendererOutput);
    }
    else
    {
      sb.AppendLine(
        $"{indent}await __mediator.Send(__command, cancellationToken);");
    }
  }

  /// <summary>
  /// Writes a command result, or assigns it to <paramref name="captureResultInto"/> when set.
  /// </summary>
  private static void EmitCommandResultOrCapture(
    StringBuilder sb,
    string indent,
    HandlerDefinition handler,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<HttpClientConfiguration> httpClientConfigurations,
    string? loggerFactoryFieldName,
    bool useRuntimeDI,
    string runtimeDISuffix,
    string? captureResultInto,
    bool verbatimRendererOutput)
  {
    if (captureResultInto is not null)
    {
      sb.AppendLine($"{indent}{captureResultInto} = result;");
      return;
    }

    EmitCommandResult(sb, indent, handler, services, httpClientConfigurations, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix, verbatimRendererOutput: verbatimRendererOutput);
  }

  /// <summary>
  /// Writes a command result. A registered <c>IResponseRenderer&lt;T&gt;</c> wins over
  /// <see cref="OutputStrategy"/>. <c>SharedProblemDetails</c> goes to stderr, through an
  /// <c>IResponseRenderer&lt;SharedProblemDetails&gt;</c> when one is available.
  /// <paramref name="verbatimRendererOutput"/> writes renderer text with no added newline.
  /// </summary>
  private static void EmitCommandResult(
    StringBuilder sb,
    string indent,
    HandlerDefinition handler,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<HttpClientConfiguration> httpClientConfigurations,
    string? loggerFactoryFieldName,
    bool useRuntimeDI,
    string runtimeDISuffix,
    string terminal = "app.Terminal",
    bool verbatimRendererOutput = false)
  {
    if (!handler.ReturnType.HasValue)
      return;

    string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
    string valueName = "result";
    string valueType = resultTypeName;
    RendererLookup lookup = new(services, httpClientConfigurations, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix);

    // Problem details set ExitCode and skip success rendering. The branch must not
    // `return` a value: INuruBehavior proceeds with Func<ValueTask>, and the caller
    // returns Environment.ExitCode after the behavior pipeline.
    string bodyIndent = indent;
    bool problemPair = false;
    if (handler.ReturnsProblemPair && handler.SuccessTypeName is string successType)
    {
      problemPair = true;
      sb.AppendLine($"{indent}if (result.IsT1)");
      sb.AppendLine($"{indent}{{");
      sb.AppendLine($"{indent}  {ProblemDetailsTypeName} __problem = result.AsT1;");
      string writeProblem = verbatimRendererOutput ? $"{terminal}.Error.Write" : $"{terminal}.WriteErrorLine";
      EmitRendererOrFallback(
        sb,
        indent + "  ",
        lookup,
        ProblemDetailsTypeName,
        handler.ProblemRendererTypeName,
        "__problem",
        "__problemRenderer",
        writeProblem,
        fallbackIndent =>
        {
          sb.AppendLine($"{fallbackIndent}{terminal}.WriteErrorLine($\"{{__problem.Type}} {{__problem.Title}}\");");
          sb.AppendLine($"{fallbackIndent}if (!string.IsNullOrEmpty(__problem.Detail))");
          sb.AppendLine($"{fallbackIndent}  {terminal}.WriteErrorLine(__problem.Detail);");
        });
      sb.AppendLine($"{indent}  int __status = __problem.Status is >= 1 and <= 255 ? __problem.Status.Value : 1;");
      sb.AppendLine($"{indent}  global::TimeWarp.Nuru.NuruCallContext.CurrentExitCode = __status;");
      sb.AppendLine($"{indent}}}");
      sb.AppendLine($"{indent}else");
      sb.AppendLine($"{indent}{{");
      bodyIndent = indent + "  ";
      sb.AppendLine($"{bodyIndent}{successType} __success = result.AsT0;");
      valueName = "__success";
      valueType = successType;
    }

    string writeSuccess = verbatimRendererOutput ? $"{terminal}.Write" : $"{terminal}.WriteLine";
    EmitRendererOrFallback(
      sb,
      bodyIndent,
      lookup,
      valueType,
      handler.RendererTypeName,
      valueName,
      "__renderer",
      writeSuccess,
      fallbackIndent => EmitResultOutput(sb, fallbackIndent, valueType, valueName, terminal));

    if (problemPair)
      sb.AppendLine($"{indent}}}");
  }

  private const string ProblemDetailsTypeName = "global::TimeWarp.Foundation.Types.SharedProblemDetails";

  /// <summary>
  /// The service context <see cref="EmitRendererOrFallback"/> resolves a renderer from.
  /// </summary>
  private readonly record struct RendererLookup(
    ImmutableArray<ServiceDefinition> Services,
    ImmutableArray<HttpClientConfiguration> HttpClientConfigurations,
    string? LoggerFactoryFieldName,
    bool UseRuntimeDI,
    string RuntimeDISuffix);

  /// <summary>
  /// Writes <paramref name="valueName"/> with an <c>IResponseRenderer&lt;T&gt;</c> for <paramref name="valueType"/>:
  /// runtime DI first, then a source-generated registration, then the visible renderer
  /// <paramref name="visibleRendererTypeName"/>. With none, <paramref name="emitFallback"/> writes it.
  /// </summary>
  /// <param name="write">Method that writes the rendered text, for example <c>app.Terminal.WriteLine</c>.</param>
  private static void EmitRendererOrFallback(
    StringBuilder sb,
    string indent,
    RendererLookup lookup,
    string valueType,
    string? visibleRendererTypeName,
    string valueName,
    string rendererLocal,
    string write,
    Action<string> emitFallback)
  {
    string rendererService = $"global::TimeWarp.Contracts.IResponseRenderer<{valueType}>";
    if (lookup.UseRuntimeDI)
    {
      sb.AppendLine($"{indent}{rendererService}? {rendererLocal} = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<{rendererService}>(GetResolutionProvider{lookup.RuntimeDISuffix}(app));");
      sb.AppendLine($"{indent}if ({rendererLocal} is not null)");
      sb.AppendLine($"{indent}{{");
      sb.AppendLine($"{indent}  {write}({rendererLocal}.Render({valueName}));");
      sb.AppendLine($"{indent}}}");
      sb.AppendLine($"{indent}else");
      sb.AppendLine($"{indent}{{");
      if (visibleRendererTypeName is not null)
        sb.AppendLine($"{indent}  {write}(new {visibleRendererTypeName}().Render({valueName}));");
      else
        emitFallback(indent + "  ");
      sb.AppendLine($"{indent}}}");
      return;
    }

    string? registeredRenderer = TryResolveRegisteredService(
      rendererService,
      lookup.Services,
      lookup.HttpClientConfigurations,
      lookup.LoggerFactoryFieldName,
      useRuntimeDI: false,
      lookup.RuntimeDISuffix);
    if (registeredRenderer is not null)
      sb.AppendLine($"{indent}{write}({registeredRenderer}.Render({valueName}));");
    else if (visibleRendererTypeName is not null)
      sb.AppendLine($"{indent}{write}(new {visibleRendererTypeName}().Render({valueName}));");
    else
      emitFallback(indent);
  }

  /// <summary>
  /// Resolves a service that ConfigureServices registered. Returns null when nothing matches,
  /// so a missing <c>IResponseRenderer&lt;T&gt;</c> can fall through to a visible renderer.
  /// </summary>
  private static string? TryResolveRegisteredService(
    string serviceTypeName,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<HttpClientConfiguration> httpClientConfigurations,
    string? loggerFactoryFieldName,
    bool useRuntimeDI,
    string runtimeDISuffix)
  {
    if (useRuntimeDI || services.IsDefaultOrEmpty)
      return null;

    string resolved = ResolveServiceForCommand(
      serviceTypeName,
      services,
      httpClientConfigurations,
      loggerFactoryFieldName,
      useRuntimeDI: false,
      runtimeDISuffix);
    if (resolved.Contains("ERROR: Service", StringComparison.Ordinal))
      return null;

    return resolved;
  }

  /// <summary>
  /// Resolves a service type to its instantiation expression for command handlers.
  /// </summary>
  private static string ResolveServiceForCommand(string? serviceTypeName, ImmutableArray<ServiceDefinition> services, ImmutableArray<HttpClientConfiguration> httpClientConfigurations, string? loggerFactoryFieldName, bool useRuntimeDI = false, string runtimeDISuffix = "")
  {
    if (string.IsNullOrEmpty(serviceTypeName))
      return "default!";

    // Well-known services get direct resolution (even with runtime DI)
    if (serviceTypeName == "global::TimeWarp.Terminal.ITerminal")
      return "app.Terminal";
    if (serviceTypeName is "global::Microsoft.Extensions.Configuration.IConfiguration"
                        or "global::Microsoft.Extensions.Configuration.IConfigurationRoot")
    {
      return "configuration";
    }

    if (serviceTypeName == "global::TimeWarp.Nuru.NuruApp")
      return "app";

    // ILogger<T> resolution - two paths for AOT optimization (see interceptor-emitter.cs)
    if (serviceTypeName.Contains("ILogger", StringComparison.Ordinal))
    {
      string typeArg = ExtractLoggerTypeArg(serviceTypeName);
      if (loggerFactoryFieldName is not null)
      {
        // Runtime path (app.LoggerFactory): nullable, requires null-coalescing
        // Used for telemetry - OTEL endpoint unknown at compile time
        if (loggerFactoryFieldName.Contains('.', StringComparison.Ordinal))
        {
          return $"({loggerFactoryFieldName}?.CreateLogger<{typeArg}>() ?? global::Microsoft.Extensions.Logging.Abstractions.NullLogger<{typeArg}>.Instance)";
        }

        // Static path (__loggerFactory): non-null, zero runtime overhead
        // Used for explicit AddLogging() - fully deterministic at compile time
        return $"{loggerFactoryFieldName}.CreateLogger<{typeArg}>()";
      }
      // No logging configured - use NullLogger (optimized away by trimmer)
      return $"global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<{typeArg}>()";
    }

    // Runtime DI path: use the command scope when one is active.
    // Session-scoped services stay on the root provider so a command scope does not dispose them.
    if (useRuntimeDI)
    {
      string normalizedServiceTypeName = serviceTypeName.StartsWith("global::", StringComparison.Ordinal)
        ? serviceTypeName[8..]
        : serviceTypeName;
      ServiceDefinition? runtimeService = services.FirstOrDefault(service =>
      {
        if (service.IsInternalType)
          return false;

        string registered = service.ServiceTypeName.StartsWith("global::", StringComparison.Ordinal)
          ? service.ServiceTypeName[8..]
          : service.ServiceTypeName;
        return registered == normalizedServiceTypeName;
      });
      string providerMethod = runtimeService?.Lifetime == ServiceLifetime.SessionScoped
        ? $"GetServiceProvider{runtimeDISuffix}"
        : $"GetResolutionProvider{runtimeDISuffix}";
      return $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{serviceTypeName}>({providerMethod}(app))";
    }

    if (FrameworkServices.IsMediatorServiceType(serviceTypeName))
      return FrameworkServices.MediatorExpression;

    // Source-gen DI path: static instantiation via Lazy<T> fields
    // Find matching service registration
    ServiceDefinition? service = services.FirstOrDefault(s => !s.IsInternalType && s.ServiceTypeName == serviceTypeName);
    if (service is null)
    {
      // Try without global:: prefix
      string normalizedTypeName = serviceTypeName.StartsWith("global::", StringComparison.Ordinal)
        ? serviceTypeName[8..]
        : serviceTypeName;
      service = services.FirstOrDefault(s =>
        !s.IsInternalType &&
        (s.ServiceTypeName == normalizedTypeName ||
         (s.ServiceTypeName.StartsWith("global::", StringComparison.Ordinal) && s.ServiceTypeName[8..] == normalizedTypeName)));
    }

    if (service is not null)
    {
      if (service.Lifetime == ServiceLifetime.Transient)
        return ServiceScopeExpressions.Construct(service, services, ServiceResolveMode.Command);

      return ServiceScopeExpressions.Read(service, ServiceResolveMode.Command);
    }

    // HttpClient typed service resolution: create implementation with static HttpClient field
    if (!httpClientConfigurations.IsDefaultOrEmpty)
    {
      string normalizedTypeName = serviceTypeName.StartsWith("global::", StringComparison.Ordinal)
        ? serviceTypeName[8..]
        : serviceTypeName;

      HttpClientConfiguration? httpClientConfig = httpClientConfigurations.FirstOrDefault(c =>
        c.ServiceTypeName == serviceTypeName ||
        c.ServiceTypeName == normalizedTypeName ||
        c.ServiceTypeName?.StartsWith("global::", StringComparison.Ordinal) == true && c.ServiceTypeName[8..] == normalizedTypeName);

      if (httpClientConfig?.ImplementationTypeName is not null)
      {
        string httpClientField = InterceptorEmitter.GetHttpClientFieldName(httpClientConfig.ServiceTypeName!);
        return $"new {httpClientConfig.ImplementationTypeName}({httpClientField})";
      }
    }

    // Fallback: emit error
    return $"default! /* ERROR: Service {serviceTypeName} not registered */";
  }

  /// <summary>
  /// Resolves a service type to its instantiation expression.
  /// Uses the same static resolution approach as delegate service injection.
  /// </summary>
  private static string ResolveServiceExpression(string? serviceTypeName, ImmutableArray<ServiceDefinition> services)
  {
    // Well-known services get direct resolution
    return serviceTypeName switch
    {
      "global::TimeWarp.Terminal.ITerminal" => "__fw_ITerminal",
      "global::Microsoft.Extensions.Configuration.IConfiguration" => "configuration",
      "global::System.Threading.CancellationToken" => "cancellationToken",
      "global::TimeWarp.Nuru.NuruApp" => "__fw_NuruApp",
      _ => ResolveRegisteredService(serviceTypeName, services)
    };
  }

  /// <summary>
  /// Resolves a registered service from ConfigureServices.
  /// </summary>
  private static string ResolveRegisteredService(string? serviceTypeName, ImmutableArray<ServiceDefinition> services)
  {
    if (string.IsNullOrEmpty(serviceTypeName))
      return "default!";

    // Find matching service registration
    ServiceDefinition? service = services.FirstOrDefault(s => !s.IsInternalType && s.ServiceTypeName == serviceTypeName);
    if (service is not null)
    {
      if (service.Lifetime == ServiceLifetime.Transient)
        return ServiceScopeExpressions.Construct(service, services, ServiceResolveMode.Command);

      return ServiceScopeExpressions.Read(service, ServiceResolveMode.Command);
    }

    // Fallback: try direct instantiation
    return $"new {serviceTypeName}()";
  }

  /// <summary>
  /// Converts a type name to a variable name (camelCase, removes leading I for interfaces).
  /// </summary>
  private static string ToVariableName(string typeName)
  {
    // Remove I prefix from interfaces
    if (typeName.Length > 1 && typeName[0] == 'I' && char.IsUpper(typeName[1]))
      typeName = typeName[1..];

    // Convert to camelCase
    return char.ToLowerInvariant(typeName[0]) + typeName[1..];
  }

  /// <summary>
  /// Extracts the type argument from ILogger&lt;T&gt;.
  /// </summary>
  private static string ExtractLoggerTypeArg(string loggerTypeName)
  {
    int start = loggerTypeName.IndexOf('<', StringComparison.Ordinal);
    int end = loggerTypeName.LastIndexOf('>');

    if (start >= 0 && end > start)
      return loggerTypeName[(start + 1)..end];

    return "object";
  }

  /// <summary>
  /// Emits invocation for a method-based handler (method group).
  /// Calls the static method directly.
  /// </summary>
  private static void EmitMethodInvocation(StringBuilder sb, RouteDefinition route, string indent, string? captureResultInto)
  {
    HandlerDefinition handler = route.Handler;
    string typeName = handler.FullTypeName ?? "UnknownHandler";
    string methodName = handler.MethodName ?? "Handle";
    string args = BuildArgumentList(handler);
    string awaitKeyword = handler.IsAsync ? "await " : "";

    if (handler.ReturnType.HasValue)
    {
      string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
      string resultType = handler.IsAsync
        ? GetUnwrappedReturnTypeName(handler)
        : handler.ReturnType.ShortTypeName;

      sb.AppendLine(
        $"{indent}{resultType} result = {awaitKeyword}{typeName}.{methodName}({args});");
      EmitResultOrCapture(sb, indent, resultTypeName, captureResultInto);
    }
    else
    {
      sb.AppendLine(
        $"{indent}{awaitKeyword}{typeName}.{methodName}({args});");
    }
  }

  /// <summary>
  /// Writes <c>result</c> to the terminal, or assigns it to <paramref name="captureResultInto"/> when set.
  /// </summary>
  private static void EmitResultOrCapture(StringBuilder sb, string indent, string? resultTypeName, string? captureResultInto)
  {
    if (captureResultInto is not null)
    {
      sb.AppendLine($"{indent}{captureResultInto} = result;");
      return;
    }

    EmitResultOutput(sb, indent, resultTypeName);
  }

  /// <summary>
  /// Writes a result captured by <see cref="Emit"/> (<c>captureResultInto</c>) the way the CLI writes it:
  /// a registered or declared <c>IResponseRenderer&lt;T&gt;</c>, problem details to stderr, otherwise
  /// <see cref="OutputStrategy"/>. A null value writes nothing, as the CLI does.
  /// </summary>
  /// <param name="valueExpression">The captured <c>object?</c> value.</param>
  /// <param name="terminal">Terminal expression to write to.</param>
  /// <param name="verbatimRendererOutput">When true (<c>UseVerbatimRendererOutput()</c>), renderer text is written with no added newline.</param>
  internal static void EmitRender(
    StringBuilder sb,
    RouteDefinition route,
    string valueExpression,
    string terminal,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<HttpClientConfiguration> httpClientConfigurations,
    string? loggerFactoryFieldName,
    bool useRuntimeDI,
    string runtimeDISuffix,
    string indent,
    bool verbatimRendererOutput = false)
  {
    HandlerDefinition handler = route.Handler;
    if (!handler.ReturnType.HasValue)
      return;

    string resultTypeName = handler.ReturnType.UnwrappedTypeName ?? handler.ReturnType.FullTypeName;
    string localType = handler.HandlerKind switch
    {
      HandlerKind.Command => resultTypeName,
      HandlerKind.Method when !handler.IsAsync => handler.ReturnType.ShortTypeName,
      _ => GetUnwrappedReturnTypeName(handler)
    };

    sb.AppendLine($"{indent}if ({valueExpression} is not null)");
    sb.AppendLine($"{indent}{{");
    sb.AppendLine($"{indent}  {localType} result = ({localType}){valueExpression};");
    if (handler.HandlerKind == HandlerKind.Command)
    {
      EmitCommandResult(sb, indent + "  ", handler, services, httpClientConfigurations, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix, terminal, verbatimRendererOutput);
    }
    else
    {
      EmitResultOutput(sb, indent + "  ", resultTypeName, "result", terminal);
    }

    sb.AppendLine($"{indent}}}");
  }

  /// <summary>
  /// Emits code to output the handler result to the terminal.
  /// Uses type-based strategy: Unit/null = no output, primitives = raw, dates = ISO 8601, complex = JSON.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="indent">The indentation string.</param>
  /// <param name="resultTypeName">The result type name to determine output strategy.</param>
  private static void EmitResultOutput(StringBuilder sb, string indent, string? resultTypeName = null, string valueName = "result", string terminal = "app.Terminal")
  {
    OutputStrategy strategy = GetOutputStrategy(resultTypeName);

    // Unit/void = no output at all
    if (strategy == OutputStrategy.None)
      return;

    bool isValueType = IsKnownValueType(resultTypeName);

    if (isValueType)
    {
      // Value types can never be null - output directly
      EmitOutputForStrategy(sb, indent, strategy, resultTypeName, valueName, terminal);
    }
    else
    {
      // Reference types - check for null before outputting
      sb.AppendLine($"{indent}if ({valueName} is not null)");
      sb.AppendLine($"{indent}{{");
      EmitOutputForStrategy(sb, $"{indent}  ", strategy, resultTypeName, valueName, terminal);
      sb.AppendLine($"{indent}}}");
    }
  }

  /// <summary>
  /// Emits the actual output code based on the strategy.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="indent">The indentation string.</param>
  /// <param name="strategy">The output strategy to use.</param>
  /// <param name="typeName">The type name for JSON serialization context.</param>
  private static void EmitOutputForStrategy(StringBuilder sb, string indent, OutputStrategy strategy, string? typeName = null, string valueName = "result", string terminal = "app.Terminal")
  {
    switch (strategy)
    {
      case OutputStrategy.Raw:
        sb.AppendLine($"{indent}{terminal}.WriteLine({valueName}.ToString());");
        break;

      case OutputStrategy.Iso8601:
        // Use "o" format for round-trip ISO 8601
        sb.AppendLine($"{indent}{terminal}.WriteLine({valueName}.ToString(\"o\", global::System.Globalization.CultureInfo.InvariantCulture));");
        break;

      case OutputStrategy.Invariant:
        // Use "c" format for culture-invariant (TimeSpan)
        sb.AppendLine($"{indent}{terminal}.WriteLine({valueName}.ToString(\"c\", global::System.Globalization.CultureInfo.InvariantCulture));");
        break;

      case OutputStrategy.Json:
        // Try JSON serialization with generated contexts, fall back to ToString() for unknown types
        // Priority: 1) NuruUserTypesJsonContext (generated by MSBuild task for user types)
        //           2) NuruJsonSerializerContext (built-in common types)
        //           3) ToString() fallback
        sb.AppendLine($"{indent}try");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  // Try user-types context first (generated by TimeWarp.Nuru.Build MSBuild task)");
        sb.AppendLine($"{indent}  {terminal}.WriteLine(global::System.Text.Json.JsonSerializer.Serialize({valueName}, global::TimeWarp.Nuru.Generated.NuruUserTypesJsonContext.Default.Options));");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine($"{indent}catch");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  try");
        sb.AppendLine($"{indent}  {{");
        sb.AppendLine($"{indent}    // Fall back to built-in context");
        sb.AppendLine($"{indent}    {terminal}.WriteLine(global::System.Text.Json.JsonSerializer.Serialize({valueName}, global::TimeWarp.Nuru.NuruJsonSerializerContext.Default.Options));");
        sb.AppendLine($"{indent}  }}");
        sb.AppendLine($"{indent}  catch");
        sb.AppendLine($"{indent}  {{");
        sb.AppendLine($"{indent}    // Type not in any context - fall back to ToString()");
        sb.AppendLine($"{indent}    {terminal}.WriteLine({valueName}?.ToString() ?? \"\");");
        sb.AppendLine($"{indent}  }}");
        sb.AppendLine($"{indent}}}");
        break;
    }
  }

  /// <summary>
  /// Ensures a type name has the global:: prefix for fully qualified usage.
  /// </summary>
  private static string EnsureGlobalPrefix(string typeName)
  {
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      return typeName;

    // If it's a simple type name (no dots), it's likely a user-defined type in the current namespace
    // We don't add global:: since it may be a local type
    if (!typeName.Contains('.', StringComparison.Ordinal))
      return typeName;

    return $"global::{typeName}";
  }

  /// <summary>
  /// Determines the output strategy based on the result type.
  /// <c>CapabilitiesEmitter.DescribeReturnType</c> uses it for the <c>returnType</c> kind.
  /// </summary>
  internal static OutputStrategy GetOutputStrategy(string? typeName)
  {
    if (string.IsNullOrEmpty(typeName))
      return OutputStrategy.None;

    return typeName switch
    {
      // Unit = no output (void equivalent)
      "global::TimeWarp.Mediator.Unit" or "Unit" => OutputStrategy.None,

      // String = raw output (no quotes)
      "global::System.String" or "string" or "String" => OutputStrategy.Raw,

      // Numeric types = raw ToString()
      "global::System.Int32" or "int" or "Int32" => OutputStrategy.Raw,
      "global::System.Int64" or "long" or "Int64" => OutputStrategy.Raw,
      "global::System.Int16" or "short" or "Int16" => OutputStrategy.Raw,
      "global::System.Byte" or "byte" or "Byte" => OutputStrategy.Raw,
      "global::System.SByte" or "sbyte" or "SByte" => OutputStrategy.Raw,
      "global::System.UInt32" or "uint" or "UInt32" => OutputStrategy.Raw,
      "global::System.UInt64" or "ulong" or "UInt64" => OutputStrategy.Raw,
      "global::System.UInt16" or "ushort" or "UInt16" => OutputStrategy.Raw,
      "global::System.Single" or "float" or "Single" => OutputStrategy.Raw,
      "global::System.Double" or "double" or "Double" => OutputStrategy.Raw,
      "global::System.Decimal" or "decimal" or "Decimal" => OutputStrategy.Raw,
      "global::System.Boolean" or "bool" or "Boolean" => OutputStrategy.Raw,
      "global::System.Char" or "char" or "Char" => OutputStrategy.Raw,

      // Guid = raw (already outputs in standard format)
      "global::System.Guid" or "Guid" => OutputStrategy.Raw,

      // Date/Time types = ISO 8601 format
      "global::System.DateTime" or "DateTime" => OutputStrategy.Iso8601,
      "global::System.DateTimeOffset" or "DateTimeOffset" => OutputStrategy.Iso8601,
      "global::System.DateOnly" or "DateOnly" => OutputStrategy.Iso8601,
      "global::System.TimeOnly" or "TimeOnly" => OutputStrategy.Iso8601,

      // TimeSpan = culture-invariant format (not ISO duration)
      "global::System.TimeSpan" or "TimeSpan" => OutputStrategy.Invariant,

      // Everything else = JSON (complex objects, collections, arrays)
      _ => OutputStrategy.Json
    };
  }

  /// <summary>
  /// Determines if a type name represents a known value type.
  /// Used to decide if null checking is needed.
  /// </summary>
  private static bool IsKnownValueType(string? typeName)
  {
    if (string.IsNullOrEmpty(typeName))
      return false;

    // Note: We can't know if user types are value types without semantic analysis
    // So we only recognize well-known value types
    return typeName switch
    {
      "global::System.Int32" or "int" or "Int32" => true,
      "global::System.Int64" or "long" or "Int64" => true,
      "global::System.Int16" or "short" or "Int16" => true,
      "global::System.Byte" or "byte" or "Byte" => true,
      "global::System.SByte" or "sbyte" or "SByte" => true,
      "global::System.UInt32" or "uint" or "UInt32" => true,
      "global::System.UInt64" or "ulong" or "UInt64" => true,
      "global::System.UInt16" or "ushort" or "UInt16" => true,
      "global::System.Single" or "float" or "Single" => true,
      "global::System.Double" or "double" or "Double" => true,
      "global::System.Decimal" or "decimal" or "Decimal" => true,
      "global::System.Boolean" or "bool" or "Boolean" => true,
      "global::System.Char" or "char" or "Char" => true,
      "global::System.DateTime" or "DateTime" => true,
      "global::System.DateTimeOffset" or "DateTimeOffset" => true,
      "global::System.DateOnly" or "DateOnly" => true,
      "global::System.TimeOnly" or "TimeOnly" => true,
      "global::System.TimeSpan" or "TimeSpan" => true,
      "global::System.Guid" or "Guid" => true,
      "global::TimeWarp.Mediator.Unit" or "Unit" => true,
      _ => false  // Assume reference type for unknown types (safer - adds null check)
    };
  }

  /// <summary>
  /// Builds the argument list for invoking the local function.
  /// Maps route segment variables to handler parameter positions.
  /// </summary>
  /// <param name="route">The route definition containing parameters and options.</param>
  /// <param name="routeIndex">The route index used for unique variable naming of typed/catch-all parameters.</param>
  /// <param name="handler">The handler definition containing parameter bindings.</param>
  /// <returns>A comma-separated argument list string.</returns>
  private static string BuildArgumentListFromRoute(RouteDefinition route, int routeIndex, HandlerDefinition handler)
  {
    List<string> args = [];

    // Get route parameters and options for positional matching
    List<ParameterDefinition> routeParams = [.. route.Parameters];
    List<OptionDefinition> routeOptions = [.. route.Options];

    int routeParamIndex = 0;

    foreach (ParameterBinding param in handler.Parameters)
    {
      switch (param.Source)
      {
        case BindingSource.Parameter:
        case BindingSource.CatchAll:
          // Match by position to route parameters
          if (routeParamIndex < routeParams.Count)
          {
            ParameterDefinition routeParam = routeParams[routeParamIndex];
            // Variable naming must match route-matcher-emitter.cs:
            // - Typed catch-all with non-string type: escaped original name (type conversion creates this)
            // - Typed catch-all with string type: __{name}_{routeIndex} (no conversion needed, already string[])
            // - Untyped catch-all: __{name}_{routeIndex} (no type conversion)
            // - Typed parameters (HasTypeConstraint): escaped original name (type conversion creates this variable)
            // - Simple parameters: just the escaped camelCase name
            bool isStringTypedCatchAll = routeParam.IsCatchAll &&
              routeParam.HasTypeConstraint &&
              routeParam.TypeConstraint?.Equals("string", StringComparison.OrdinalIgnoreCase) == true;
            string varName = routeParam.IsCatchAll && (!routeParam.HasTypeConstraint || isStringTypedCatchAll)
              ? $"__{routeParam.CamelCaseName}_{routeIndex}"
              : CSharpIdentifierUtils.EscapeIfKeyword(routeParam.CamelCaseName);
            args.Add(varName);
            routeParamIndex++;
          }
          else
          {
            // Fallback to handler param name if no matching route param
            args.Add(CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName));
          }

          break;

        case BindingSource.Option:
        case BindingSource.Flag:
          // For options/flags, find by matching the handler parameter name to option parameter name,
          // long form, short form, or camelCase of long form
          OptionDefinition? matchingOption = routeOptions.FirstOrDefault(o =>
            o.ParameterName?.Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase) == true ||
            o.LongForm?.Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase) == true ||
            o.ShortForm?.Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase) == true ||
            ToCamelCase(o.LongForm ?? "").Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase));

          if (matchingOption is not null)
          {
            // Variable name must match route-matcher-emitter.cs line 492:
            // For value options: use ParameterName (e.g., "mode" from "--config {mode}")
            // For flags: use LongForm or ShortForm (e.g., "verbose" from "--verbose")
            string optVarName = matchingOption.ExpectsValue
              ? ToCamelCase(matchingOption.ParameterName ?? matchingOption.LongForm ?? matchingOption.ShortForm ?? param.ParameterName)
              : ToCamelCase(matchingOption.LongForm ?? matchingOption.ShortForm ?? param.ParameterName);
            args.Add(CSharpIdentifierUtils.EscapeIfKeyword(optVarName));
          }
          else
          {
            args.Add(CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName));
          }

          break;

        case BindingSource.Service:
          // Service parameters use the resolved service variable (named by ParameterName)
          args.Add(CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName));
          break;

        case BindingSource.CancellationToken:
          args.Add("cancellationToken");
          break;
      }
    }

    return string.Join(", ", args);
  }

  /// <summary>
  /// Builds the argument list for handler invocation (used by method handlers).
  /// Escapes keyword identifiers so they match capture variables from route-matcher-emitter.
  /// </summary>
  private static string BuildArgumentList(HandlerDefinition handler)
  {
    List<string> args = [];

    foreach (ParameterBinding param in handler.Parameters)
    {
      switch (param.Source)
      {
        case BindingSource.Parameter:
        case BindingSource.Option:
        case BindingSource.Flag:
        case BindingSource.CatchAll:
          // Route parameters use their captured variable name (escaped if keyword)
          args.Add(CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName));
          break;

        case BindingSource.Service:
          // Service parameters use the resolved service variable (escaped if keyword)
          args.Add(CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName));
          break;

        case BindingSource.CancellationToken:
          args.Add("cancellationToken");
          break;
      }
    }

    return string.Join(", ", args);
  }

  /// <summary>
  /// Builds the parameter list for the local function signature.
  /// Uses the handler parameter names and types so the lambda body works correctly.
  /// </summary>
  private static string BuildParameterList(HandlerDefinition handler)
  {
    List<string> parameters = [];

    foreach (ParameterBinding param in handler.Parameters)
    {
      switch (param.Source)
      {
        case BindingSource.Parameter:
        case BindingSource.Option:
        case BindingSource.Flag:
        case BindingSource.CatchAll:
        case BindingSource.Service:
          // Use the handler's parameter name and type
          string escapedName = CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName);
          parameters.Add($"{param.ParameterTypeName} {escapedName}");
          break;

        case BindingSource.CancellationToken:
          parameters.Add("global::System.Threading.CancellationToken cancellationToken");
          break;
      }
    }

    return string.Join(", ", parameters);
  }

  private static string ToCamelCase(string value) => CSharpIdentifierUtils.ToCamelCase(value);
}
