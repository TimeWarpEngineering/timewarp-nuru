namespace TimeWarp.Nuru;

/// <summary>
/// Source generator that creates Command/Query classes and Handler classes from delegate signatures.
/// Supports AsCommand(), AsIdempotentCommand(), and AsQuery() in the fluent API.
/// Generates a sealed class with properties for each route parameter, and a nested Handler class
/// that wraps the delegate body with parameter rewriting.
/// </summary>
/// <remarks>
/// <para>
/// This class is split into partial classes for maintainability:
/// <list type="bullet">
///   <item><description>nuru-delegate-command-generator.cs: Core initialization and fluent chain detection (this file)</description></item>
///   <item><description>nuru-delegate-command-generator.types.cs: Record types and enum definitions</description></item>
///   <item><description>nuru-delegate-command-generator.route-parsing.cs: Route pattern parsing</description></item>
///   <item><description>nuru-delegate-command-generator.signature.cs: Delegate signature extraction</description></item>
///   <item><description>nuru-delegate-command-generator.handler.cs: Handler info extraction and body rewriting</description></item>
///   <item><description>nuru-delegate-command-generator.codegen.cs: Code generation</description></item>
///   <item><description>nuru-delegate-command-generator.rewriter.cs: ParameterRewriter class</description></item>
/// </list>
/// </para>
/// <para>
/// Message type determines the generated interface:
/// <list type="bullet">
///   <item><description>AsCommand() → ICommand&lt;T&gt; + ICommandHandler</description></item>
///   <item><description>AsIdempotentCommand() → ICommand&lt;T&gt;, IIdempotent + ICommandHandler</description></item>
///   <item><description>AsQuery() → IQuery&lt;T&gt; + IQueryHandler</description></item>
/// </list>
/// </para>
///
/// <para>Example input:</para>
/// <code>
/// app.Map("deploy {env} --force")
///     .WithHandler((string env, bool force, ILogger logger) => {
///         logger.LogInformation("Deploying to {Env}", env);
///     })
///     .AsCommand()
///     .Done();
/// </code>
///
/// <para>Example output:</para>
/// <code>
/// [GeneratedCode("TimeWarp.Nuru.Analyzers", "1.0.0")]
/// public sealed class Deploy_Generated_Command : ICommand&lt;Unit&gt;
/// {
///   public string Env { get; set; } = string.Empty;
///   public bool Force { get; set; }
///
///   public sealed class Handler : ICommandHandler&lt;Deploy_Generated_Command, Unit&gt;
///   {
///     private readonly ILogger Logger;
///
///     public Handler(ILogger logger)
///     {
///       Logger = logger;
///     }
///
///     public ValueTask&lt;Unit&gt; Handle(Deploy_Generated_Command request, CancellationToken cancellationToken)
///     {
///       Logger.LogInformation("Deploying to {Env}", request.Env);
///       return default;
///     }
///   }
/// }
/// </code>
/// </remarks>
[Generator]
public partial class NuruDelegateCommandGenerator : IIncrementalGenerator
{
  private const string SuppressAttributeName = "TimeWarp.Nuru.SuppressNuruCommandGenerationAttribute";

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Step 1: Check for [assembly: SuppressNuruCommandGeneration] attribute
    IncrementalValueProvider<bool> hasSuppressAttribute = context.CompilationProvider
      .Select(static (compilation, _) =>
      {
        foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
        {
          if (attribute.AttributeClass?.ToDisplayString() == SuppressAttributeName)
            return true;
        }

        return false;
      });

    // Step 1b: Check for UseNewGen property
    IncrementalValueProvider<bool> useNewGen = GeneratorHelpers.GetUseNewGenProvider(context);

    // Combine both suppression conditions
    IncrementalValueProvider<bool> shouldSuppress = hasSuppressAttribute
      .Combine(useNewGen)
      .Select(static (pair, _) => pair.Left || pair.Right);

    // Step 2: Find all message type invocations (AsCommand, AsIdempotentCommand, AsQuery) and extract route info
    IncrementalValuesProvider<DelegateCommandInfo?> commandInfos = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsMessageTypeInvocation(node),
        transform: static (ctx, ct) => ExtractCommandInfo(ctx, ct))
      .Where(static info => info is not null);

    // Step 3: Collect all command infos
    IncrementalValueProvider<ImmutableArray<DelegateCommandInfo?>> collectedInfos = commandInfos.Collect();

    // Step 4: Combine with suppress flag
    IncrementalValueProvider<(ImmutableArray<DelegateCommandInfo?> Commands, bool Suppress)> combined =
      collectedInfos.Combine(shouldSuppress);

    // Step 5: Generate source code
    context.RegisterSourceOutput(combined, static (ctx, data) =>
    {
      if (data.Suppress)
        return;

      ImmutableArray<DelegateCommandInfo?> commands = data.Commands;

      if (commands.IsDefaultOrEmpty)
        return;

      // Filter nulls and get unique commands by class name
      HashSet<string> seenNames = [];
      List<DelegateCommandInfo> uniqueCommands = [];

      foreach (DelegateCommandInfo? cmd in commands)
      {
        if (cmd is null)
          continue;

        if (seenNames.Add(cmd.ClassName))
        {
          uniqueCommands.Add(cmd);
        }
      }

      if (uniqueCommands.Count == 0)
        return;

      string source = GenerateCommandClasses(uniqueCommands);
      ctx.AddSource("GeneratedDelegateCommands.g.cs", source);
    });
  }

  /// <summary>
  /// Detects message type invocations in the fluent API pattern.
  /// Matches AsCommand(), AsIdempotentCommand(), and AsQuery().
  /// </summary>
  private static bool IsMessageTypeInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.Text is "AsCommand" or "AsIdempotentCommand" or "AsQuery";
  }

  /// <summary>
  /// Extracts command info from an AsCommand() invocation by walking back the fluent chain.
  /// </summary>
  private static DelegateCommandInfo? ExtractCommandInfo(
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken)
  {
    if (context.Node is not InvocationExpressionSyntax asCommandInvocation)
      return null;

    // Walk back the fluent chain to find WithHandler and Map
    FluentChainInfo? chainInfo = WalkBackFluentChain(asCommandInvocation);
    if (chainInfo is null)
    {
      // Debug: chain info not found
      return null;
    }

    // Extract delegate signature from the handler
    DelegateSignature? signature = ExtractSignatureFromHandler(
      chainInfo.HandlerExpression,
      context.SemanticModel,
      cancellationToken);

    if (signature is null)
      return null;

    // Parse the route pattern to identify route parameters
    RouteParameterInfo routeParams = ParseRoutePattern(chainInfo.Pattern);

    // Classify all parameters as route params or DI params
    List<ParameterClassification> allParams = [];
    List<CommandPropertyInfo> properties = [];

    foreach (DelegateParameterInfo param in signature.Parameters)
    {
      // Check if this parameter is a route parameter
      RouteParamMatch? match = FindRouteParameter(param.Name, routeParams);
      bool isRouteParam = match is not null;

      allParams.Add(new ParameterClassification(
        Name: param.Name,
        TypeFullName: param.Type.FullName,
        IsRouteParam: isRouteParam,
        IsDiParam: !isRouteParam));

      if (isRouteParam)
      {
        properties.Add(new CommandPropertyInfo(
          Name: ToPascalCase(param.Name),
          TypeName: param.Type.FullName,
          IsNullable: param.IsNullable,
          IsArray: param.IsArray,
          DefaultValue: GetDefaultValue(param)));
      }
    }

    // Generate class name from pattern (suffix varies by message type)
    string className = GenerateClassName(chainInfo.Pattern, chainInfo.MessageType);

    // Determine return type for ICommand<T> or IQuery<T>
    string commandReturnType = GetCommandReturnType(signature);

    // Extract handler info if the handler is a lambda expression
    HandlerInfo? handlerInfo = ExtractHandlerInfo(
      chainInfo.HandlerExpression,
      allParams,
      signature,
      context.SemanticModel,
      cancellationToken);

    // Extract provenance information for debugging
    Location location = context.Node.GetLocation();
    FileLinePositionSpan lineSpan = location.GetLineSpan();
    string? sourceFilePath = !string.IsNullOrEmpty(lineSpan.Path)
      ? System.IO.Path.GetFileName(lineSpan.Path)
      : null;
    int? sourceLineNumber = sourceFilePath is not null
      ? lineSpan.StartLinePosition.Line + 1
      : null;

    // Determine handler description
    string handlerDescription = chainInfo.HandlerExpression switch
    {
      LambdaExpressionSyntax when handlerInfo is not null => "lambda expression",
      LambdaExpressionSyntax => "lambda expression (handler generation skipped - closures detected)",
      IdentifierNameSyntax id when handlerInfo is not null => $"local function: {id.Identifier.Text}",
      IdentifierNameSyntax id => $"method group: {id.Identifier.Text} (handler generation failed)",
      MemberAccessExpressionSyntax ma when handlerInfo is not null => $"static method: {ma.Expression}.{ma.Name.Identifier.Text}",
      MemberAccessExpressionSyntax ma => $"method: {ma.Expression}.{ma.Name.Identifier.Text} (handler generation failed)",
      _ => $"{chainInfo.HandlerExpression.Kind()} (handler generation not supported)"
    };

    return new DelegateCommandInfo(
      ClassName: className,
      Properties: [.. properties],
      ReturnType: commandReturnType,
      MessageType: chainInfo.MessageType,
      Handler: handlerInfo,
      SourceFilePath: sourceFilePath,
      SourceLineNumber: sourceLineNumber,
      PatternText: chainInfo.Pattern,
      HandlerDescription: handlerDescription);
  }

  /// <summary>
  /// Walks back the fluent chain from AsCommand() to find WithHandler() and Map().
  /// </summary>
  /// <remarks>
  /// Chain structure for: app.Map("pattern").WithHandler(handler).AsCommand().Done()
  ///
  /// InvocationExpression (.AsCommand())
  ///   └── Expression: MemberAccessExpression
  ///         ├── Name: "AsCommand"
  ///         └── Expression: InvocationExpression (.WithHandler(handler))
  ///               ├── Expression: MemberAccessExpression
  ///               │     ├── Name: "WithHandler"
  ///               │     └── Expression: InvocationExpression (.Map("pattern"))
  ///               │           ├── Expression: MemberAccessExpression
  ///               │           │     ├── Name: "Map"
  ///               │           │     └── Expression: IdentifierName "app"
  ///               │           └── ArgumentList: ("pattern")
  ///               └── ArgumentList: (handler)
  /// </remarks>
  private static FluentChainInfo? WalkBackFluentChain(InvocationExpressionSyntax messageTypeInvocation)
  {
    ExpressionSyntax? handlerExpression = null;
    string? pattern = null;

    // Extract message type from the invocation (AsCommand, AsIdempotentCommand, AsQuery)
    GeneratedMessageType messageType = GeneratedMessageType.Command;
    if (messageTypeInvocation.Expression is MemberAccessExpressionSyntax msgTypeAccess)
    {
      messageType = msgTypeAccess.Name.Identifier.Text switch
      {
        "AsQuery" => GeneratedMessageType.Query,
        "AsIdempotentCommand" => GeneratedMessageType.IdempotentCommand,
        _ => GeneratedMessageType.Command
      };
    }

    // Start from message type invocation and walk backwards through the chain
    ExpressionSyntax? current = messageTypeInvocation.Expression;

    while (current is MemberAccessExpressionSyntax memberAccess)
    {
      // memberAccess.Expression is what we're calling the method on (the previous invocation result)
      if (memberAccess.Expression is not InvocationExpressionSyntax previousInvocation)
        break;

      // Check what method was called in the previous invocation
      if (previousInvocation.Expression is MemberAccessExpressionSyntax prevMethodAccess)
      {
        string methodName = prevMethodAccess.Name.Identifier.Text;

        if (methodName == "WithHandler")
        {
          // Extract handler from WithHandler(handler)
          if (previousInvocation.ArgumentList.Arguments.Count > 0)
          {
            handlerExpression = previousInvocation.ArgumentList.Arguments[0].Expression;
          }
        }
        else if (methodName == "Map")
        {
          // Extract pattern from Map("pattern")
          if (previousInvocation.ArgumentList.Arguments.Count > 0)
          {
            ArgumentSyntax patternArg = previousInvocation.ArgumentList.Arguments[0];
            if (patternArg.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
              pattern = literal.Token.ValueText;
            }
          }
        }
      }

      // Move to the previous step in the chain
      current = previousInvocation.Expression;
    }

    if (handlerExpression is null || pattern is null)
      return null;

    return new FluentChainInfo(pattern, handlerExpression, messageType);
  }
}
