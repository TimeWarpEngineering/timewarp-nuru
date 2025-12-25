namespace TimeWarp.Nuru.SourceGen;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

/// <summary>
/// V2 source generator - generates compile-time route endpoints.
/// Only runs when UseNewGen=true.
/// </summary>
/// <remarks>
/// This generator:
/// <list type="bullet">
///   <item><description>Detects Map() invocations in fluent chains</description></item>
///   <item><description>Extracts route patterns, handlers, descriptions from the chain</description></item>
///   <item><description>Builds RouteDefinition objects from the extracted data</description></item>
///   <item><description>Emits GeneratedEndpoints with Endpoint[], handlers, and matchers</description></item>
/// </list>
/// </remarks>
[Generator]
public class NuruV2Generator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValueProvider<bool> useNewGen = TimeWarp.Nuru.GeneratorHelpers.GetUseNewGenProvider(context);

    // Step 1: Find all Map() invocations
    IncrementalValuesProvider<MapInvocationInfo?> mapInvocations = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsMapInvocation(node),
        transform: static (ctx, ct) => ExtractMapInfo(ctx, ct))
      .Where(static info => info is not null);

    // Step 2: Collect all map invocations
    IncrementalValueProvider<ImmutableArray<MapInvocationInfo?>> collectedMaps = mapInvocations.Collect();

    // Step 3: Combine with UseNewGen flag
    IncrementalValueProvider<(ImmutableArray<MapInvocationInfo?> Maps, bool Enabled)> combined =
      collectedMaps.Combine(useNewGen);

    // Step 4: Generate source code
    context.RegisterSourceOutput(combined, static (ctx, data) =>
    {
      if (!data.Enabled)
        return;

      ImmutableArray<MapInvocationInfo?> maps = data.Maps;

      if (maps.IsDefaultOrEmpty)
      {
        // Even with no routes, emit the marker
        EmitMarkerOnly(ctx);
        return;
      }

      // Filter nulls and build endpoint emit infos
      List<EndpointEmitInfo> endpoints = [];
      int order = 0;

      foreach (MapInvocationInfo? mapInfo in maps)
      {
        if (mapInfo is null)
          continue;

        EndpointEmitInfo? endpoint = BuildEndpointEmitInfo(mapInfo, order++);
        if (endpoint is not null)
        {
          endpoints.Add(endpoint);
        }
      }

      if (endpoints.Count == 0)
      {
        EmitMarkerOnly(ctx);
        return;
      }

      // Sort by specificity (descending - most specific first)
      endpoints.Sort((a, b) => b.Route.ComputedSpecificity.CompareTo(a.Route.ComputedSpecificity));

      // Emit the generated code
      RuntimeCodeEmitter.EmitOptions options = new(
        Namespace: "TimeWarp.Nuru.Generated",
        ClassName: "GeneratedEndpoints",
        IndentSpaces: 2);

      RuntimeCodeEmitter.EmitResult result = RuntimeCodeEmitter.EmitSourceFile(endpoints, options);
      ctx.AddSource(result.FileName, result.SourceCode);

      // Also emit the marker
      EmitMarker(ctx, endpoints.Count);
    });
  }

  /// <summary>
  /// Detects Map() invocations.
  /// </summary>
  private static bool IsMapInvocation(SyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    // Check for .Map(...) or Map<T>(...)
    return invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess =>
        memberAccess.Name.Identifier.Text == "Map" ||
        (memberAccess.Name is GenericNameSyntax generic && generic.Identifier.Text == "Map"),
      GenericNameSyntax generic => generic.Identifier.Text == "Map",
      IdentifierNameSyntax identifier => identifier.Identifier.Text == "Map",
      _ => false
    };
  }

  /// <summary>
  /// Extracts information from a Map() invocation and its fluent chain.
  /// </summary>
  private static MapInvocationInfo? ExtractMapInfo(
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken)
  {
    if (context.Node is not InvocationExpressionSyntax mapInvocation)
      return null;

    // Check if this is a generic Map<T>() call
    bool isGenericMap = MediatorRouteExtractor.IsGenericMapCall(mapInvocation);

    if (isGenericMap)
    {
      // Handle Map<TRequest>("pattern") - mediator style
      MediatorRouteExtractor.MediatorRouteResult mediatorResult =
        MediatorRouteExtractor.ExtractFromMediatorMapChain(mapInvocation);

      if (mediatorResult.Pattern is null)
        return null;

      return new MapInvocationInfo(
        Pattern: mediatorResult.Pattern,
        Description: mediatorResult.Description,
        MessageType: mediatorResult.MessageType ?? "Unspecified",
        Aliases: mediatorResult.Aliases,
        HandlerLambda: null,
        RequestTypeName: mediatorResult.RequestTypeName,
        IsMediator: true,
        Location: mapInvocation.GetLocation());
    }
    else
    {
      // Handle Map("pattern").WithHandler(lambda) - delegate style
      FluentChainExtractor.FluentChainResult chainResult =
        FluentChainExtractor.ExtractFromMapChain(mapInvocation);

      if (chainResult.Pattern is null)
        return null;

      return new MapInvocationInfo(
        Pattern: chainResult.Pattern,
        Description: chainResult.Description,
        MessageType: chainResult.MessageType ?? "Unspecified",
        Aliases: chainResult.Aliases,
        HandlerLambda: chainResult.HandlerLambda,
        RequestTypeName: null,
        IsMediator: false,
        Location: mapInvocation.GetLocation());
    }
  }

  /// <summary>
  /// Builds an EndpointEmitInfo from extracted map info.
  /// </summary>
  private static EndpointEmitInfo? BuildEndpointEmitInfo(MapInvocationInfo mapInfo, int order)
  {
    // Parse the pattern into segments
    ImmutableArray<SegmentDefinition> segments = ParsePattern(mapInfo.Pattern);

    // Calculate specificity
    int specificity = segments.Sum(s => s.SpecificityContribution);

    // Build handler definition
    HandlerDefinition handler;
    string? handlerCode = null;
    string? commandTypeName = null;

    if (mapInfo.IsMediator && mapInfo.RequestTypeName is not null)
    {
      handler = new HandlerDefinitionBuilder()
        .AsMediator(mapInfo.RequestTypeName)
        .Build();
      commandTypeName = mapInfo.RequestTypeName;
    }
    else
    {
      // For delegate handlers, extract the lambda code
      HandlerDefinitionBuilder handlerBuilder = new HandlerDefinitionBuilder().AsDelegate();

      // Add parameters from route pattern
      foreach (SegmentDefinition segment in segments)
      {
        if (segment is ParameterDefinition param)
        {
          handlerBuilder.WithParameter(
            name: param.Name,
            typeName: param.ResolvedClrTypeName ?? "string",
            segmentName: param.Name,
            isOptional: param.IsOptional);
        }
      }

      // Determine return type - default to void for now
      handlerBuilder.ReturnsVoid();

      handler = handlerBuilder.Build();

      // Extract the lambda code to emit directly
      if (mapInfo.HandlerLambda is not null)
      {
        handlerCode = mapInfo.HandlerLambda.ToFullString().Trim();
      }
    }

    // Build the route definition
    RouteDefinition route = new RouteDefinitionBuilder()
      .WithPattern(mapInfo.Pattern)
      .WithSegments(segments)
      .WithMessageType(mapInfo.MessageType)
      .WithDescription(mapInfo.Description)
      .WithHandler(handler)
      .WithAliases(mapInfo.Aliases)
      .WithSpecificity(specificity)
      .WithOrder(order)
      .Build();

    return new EndpointEmitInfo(
      Route: route,
      HandlerCode: handlerCode,
      CommandTypeName: commandTypeName);
  }

  /// <summary>
  /// Parses a route pattern into segments.
  /// </summary>
  private static ImmutableArray<SegmentDefinition> ParsePattern(string pattern)
  {
    List<SegmentDefinition> segments = [];
    int position = 0;

    // Split by whitespace
    string[] parts = pattern.Split([' '], StringSplitOptions.RemoveEmptyEntries);

    foreach (string part in parts)
    {
      SegmentDefinition? segment = ParseSegment(part, position);
      if (segment is not null)
      {
        segments.Add(segment);
        position++;
      }
    }

    return [.. segments];
  }

  /// <summary>
  /// Parses a single segment from a pattern part.
  /// </summary>
  private static SegmentDefinition? ParseSegment(string part, int position)
  {
    // Required parameter: {name} or {name:type}
    if (part.StartsWith('{') && part.EndsWith('}'))
    {
      string inner = part[1..^1];
      bool isCatchAll = inner.StartsWith('*');
      if (isCatchAll)
        inner = inner[1..];

      string? typeConstraint = null;
      string name = inner;

      int colonIndex = inner.IndexOf(':', StringComparison.Ordinal);
      if (colonIndex > 0)
      {
        name = inner[..colonIndex];
        typeConstraint = inner[(colonIndex + 1)..];
      }

      return new ParameterDefinition(
        Position: position,
        Name: name,
        TypeConstraint: typeConstraint,
        Description: null,
        IsOptional: false,
        IsCatchAll: isCatchAll,
        ResolvedClrTypeName: ResolveClrType(typeConstraint),
        DefaultValue: null);
    }

    // Optional parameter: [name] or [name:type]
    if (part.StartsWith('[') && part.EndsWith(']'))
    {
      string inner = part[1..^1];
      string? typeConstraint = null;
      string name = inner;

      int colonIndex = inner.IndexOf(':', StringComparison.Ordinal);
      if (colonIndex > 0)
      {
        name = inner[..colonIndex];
        typeConstraint = inner[(colonIndex + 1)..];
      }

      return new ParameterDefinition(
        Position: position,
        Name: name,
        TypeConstraint: typeConstraint,
        Description: null,
        IsOptional: true,
        IsCatchAll: false,
        ResolvedClrTypeName: ResolveClrType(typeConstraint),
        DefaultValue: null);
    }

    // Option: --name or -n
    if (part.StartsWith("--", StringComparison.Ordinal) || (part.StartsWith('-') && part.Length == 2))
    {
      // For now, skip options in basic parsing
      // Full option parsing would need to look at following segments
      return null;
    }

    // Literal
    return new LiteralDefinition(Position: position, Value: part);
  }

  /// <summary>
  /// Resolves a type constraint to a CLR type name.
  /// </summary>
  private static string ResolveClrType(string? typeConstraint)
  {
    return typeConstraint switch
    {
      "int" => "global::System.Int32",
      "long" => "global::System.Int64",
      "short" => "global::System.Int16",
      "byte" => "global::System.Byte",
      "float" => "global::System.Single",
      "double" => "global::System.Double",
      "decimal" => "global::System.Decimal",
      "bool" => "global::System.Boolean",
      "char" => "global::System.Char",
      "string" or null => "global::System.String",
      "Guid" or "guid" => "global::System.Guid",
      "DateTime" or "datetime" => "global::System.DateTime",
      _ => $"global::{typeConstraint}"
    };
  }

  /// <summary>
  /// Emits just the marker type when there are no routes.
  /// </summary>
  private static void EmitMarkerOnly(SourceProductionContext ctx)
  {
    const string source = """
      // <auto-generated/>
      // This file was generated by NuruV2Generator when UseNewGen=true

      namespace TimeWarp.Nuru.Generated;

      /// <summary>
      /// Marker type indicating V2 generator ran successfully.
      /// This type is only generated when the UseNewGen MSBuild property is set to true.
      /// </summary>
      [global::System.CodeDom.Compiler.GeneratedCode("TimeWarp.Nuru.Analyzers.V2", "1.0.0")]
      internal static class NuruV2GeneratorMarker
      {
        /// <summary>
        /// The version of the V2 generator.
        /// </summary>
        public const string Version = "2.0.0";

        /// <summary>
        /// Indicates this is V2 generated code.
        /// </summary>
        public const bool IsV2 = true;

        /// <summary>
        /// Number of routes generated.
        /// </summary>
        public const int RouteCount = 0;
      }
      """;

    ctx.AddSource("NuruV2GeneratorMarker.g.cs", source);
  }

  /// <summary>
  /// Emits the marker type with route count.
  /// </summary>
  private static void EmitMarker(SourceProductionContext ctx, int routeCount)
  {
    string source = $$"""
      // <auto-generated/>
      // This file was generated by NuruV2Generator when UseNewGen=true

      namespace TimeWarp.Nuru.Generated;

      /// <summary>
      /// Marker type indicating V2 generator ran successfully.
      /// This type is only generated when the UseNewGen MSBuild property is set to true.
      /// </summary>
      [global::System.CodeDom.Compiler.GeneratedCode("TimeWarp.Nuru.Analyzers.V2", "1.0.0")]
      internal static class NuruV2GeneratorMarker
      {
        /// <summary>
        /// The version of the V2 generator.
        /// </summary>
        public const string Version = "2.0.0";

        /// <summary>
        /// Indicates this is V2 generated code.
        /// </summary>
        public const bool IsV2 = true;

        /// <summary>
        /// Number of routes generated.
        /// </summary>
        public const int RouteCount = {{routeCount}};
      }
      """;

    ctx.AddSource("NuruV2GeneratorMarker.g.cs", source);
  }

  /// <summary>
  /// Information extracted from a Map() invocation.
  /// </summary>
  private sealed record MapInvocationInfo(
    string Pattern,
    string? Description,
    string MessageType,
    ImmutableArray<string> Aliases,
    LambdaExpressionSyntax? HandlerLambda,
    string? RequestTypeName,
    bool IsMediator,
    Location Location);
}
