#region Purpose
// Command/query handler invocation, renderer lookup and service resolution for HandlerInvokerEmitter.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class HandlerInvokerEmitter
{
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

    // No generated mediator, or no handler visible at build time: InterceptorEmitter.EmitMissingHandlerOnDispatch
    // already returned 1 before the behavior pipeline. Emit nothing here, so a behavior lambda
    // never holds a `return 1;` and __GetMediator() is never referenced.
    if (!useRuntimeDI && !hasGeneratedMediator)
    {
      sb.AppendLine($"{indent}// Unreachable: exited 1 before dispatch (no generated mediator).");
      return;
    }

    if (InterceptorEmitter.IsMissingHandler(handler))
    {
      sb.AppendLine($"{indent}// Unreachable: exited 1 before dispatch (no visible handler).");
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
    ResultValueKind valueKind = handler.ReturnType.ValueKind;
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
      valueKind = handler.SuccessValueKind;
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
      fallbackIndent => EmitResultOutput(sb, fallbackIndent, valueType, valueKind, valueName, terminal));

    if (problemPair)
      sb.AppendLine($"{indent}}}");
  }

  /// <summary>
  /// True when <paramref name="app"/> may have an <c>IResponseRenderer&lt;SharedProblemDetails&gt;</c>: runtime DI
  /// (decided when the call runs), a registered source-gen service, or the visible renderer
  /// <paramref name="visibleRendererTypeName"/>. <see cref="EmitProblemRendererMethod"/> is emitted exactly then.
  /// </summary>
  internal static bool HasProblemRenderer(AppModel app, string? visibleRendererTypeName) =>
    app.UseMicrosoftDependencyInjection
    || visibleRendererTypeName is not null
    || FindRegisteredService(ProblemRendererServiceTypeName, app.Services) is not null;

  /// <summary>
  /// Emits <c>__RenderProblem{suffix}</c>, the app's <c>NuruApp.ProblemRenderer</c>: renders a problem for a bind
  /// failure or an escaped exception with the renderer a problem pair uses, in the same order (runtime DI, a
  /// registered service, the visible renderer), and returns null when there is none (task 508-018). It runs
  /// outside a command, so a registered renderer is read as a process-lifetime service.
  /// </summary>
  internal static void EmitProblemRendererMethod(StringBuilder sb, AppModel app, string methodSuffix, string? visibleRendererTypeName)
  {
    ImmutableArray<ServiceDefinition> services = app.Services;
    sb.AppendLine($"  private static string? __RenderProblem{methodSuffix}(NuruApp app, {ProblemDetailsTypeName} __problem)");
    sb.AppendLine("  {");
    if (app.UseMicrosoftDependencyInjection)
    {
      sb.AppendLine($"    {ProblemRendererServiceTypeName}? __problemRenderer = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<{ProblemRendererServiceTypeName}>(GetResolutionProvider{methodSuffix}(app));");
      sb.AppendLine("    if (__problemRenderer is not null)");
      sb.AppendLine("      return __problemRenderer.Render(__problem);");
    }
    else if (FindRegisteredService(ProblemRendererServiceTypeName, services) is ServiceDefinition registered)
    {
      string renderer = registered.Lifetime == ServiceLifetime.Transient
        ? ServiceScopeExpressions.Construct(registered, services, ServiceResolveMode.ProcessInit)
        : ServiceScopeExpressions.Read(registered, services, ServiceResolveMode.ProcessInit);
      sb.AppendLine("    _ = app;");
      sb.AppendLine($"    return {renderer}.Render(__problem);");
      sb.AppendLine("  }");
      sb.AppendLine();
      return;
    }
    else
    {
      sb.AppendLine("    _ = app;");
    }

    sb.AppendLine(visibleRendererTypeName is not null
      ? $"    return new {visibleRendererTypeName}().Render(__problem);"
      : "    return null;");
    sb.AppendLine("  }");
    sb.AppendLine();
  }

  /// <summary>
  /// The source-gen registration for <paramref name="serviceTypeName"/>, matched with or without
  /// <c>global::</c>, as <see cref="ResolveServiceForCommand"/> matches it.
  /// </summary>
  private static ServiceDefinition? FindRegisteredService(string serviceTypeName, ImmutableArray<ServiceDefinition> services)
  {
    if (services.IsDefaultOrEmpty)
      return null;

    string normalized = WithoutGlobalPrefix(serviceTypeName);
    return services.FirstOrDefault(service =>
      !service.IsInternalType
      && (service.ServiceTypeName == serviceTypeName || WithoutGlobalPrefix(service.ServiceTypeName) == normalized));
  }

  private static string WithoutGlobalPrefix(string typeName) =>
    typeName.StartsWith("global::", StringComparison.Ordinal) ? typeName[8..] : typeName;

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

    return ServiceResolverEmitter.ResolveServiceExpression(serviceTypeName, services, httpClientConfigurations, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix);
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
      EmitResultOrCapture(sb, indent, resultTypeName, handler.ReturnType.ValueKind, captureResultInto);
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
  private static void EmitResultOrCapture(StringBuilder sb, string indent, string? resultTypeName, ResultValueKind valueKind, string? captureResultInto)
  {
    if (captureResultInto is not null)
    {
      sb.AppendLine($"{indent}{captureResultInto} = result;");
      return;
    }

    EmitResultOutput(sb, indent, resultTypeName, valueKind);
  }

  /// <summary>
  /// Writes a result captured by <see cref="Emit"/> (<c>captureResultInto</c>) the way the CLI writes it:
  /// a registered or declared <c>IResponseRenderer&lt;T&gt;</c>, problem details to stderr, otherwise
  /// <see cref="OutputStrategy"/>. A null value writes nothing, as the CLI does.
  /// </summary>
  /// <param name="valueExpression">The captured <c>object?</c> value.</param>
  /// <param name="terminal">Terminal expression to write to.</param>
  /// <param name="verbatimRendererOutput">When true (<c>UseVerbatimRendererOutput()</c>), renderer text is written with no added newline.</param>
}
