#region Purpose
// Route and endpoint JSON type extraction helpers of GenerateNuruJsonContextTask.
#endregion

#region Design
// Extraction goes through DslInterpreter and EndpointExtractor, the same code the source generator
// uses, so delegate routes, [NuruRoute] classes and referenced-assembly endpoints (opt-in via
// [assembly: NuruEndpointAssembly] or Map<T>()) resolve by one set of rules. Failures while
// extracting are logged at low importance and extraction continues, so one unreadable file does not
// lose the types found in others.
#endregion

#region Open Questions
// Q1 (2026-11-14, claude): The method-body loop in ExtractFromDelegateRoutes ends in a bare catch
//     that swallows every exception, while Execute's comment says unexpected extraction bugs must
//     not fall back silently (470 M24). Is that loop meant to stay fail-soft for "method has no DSL",
//     or should it catch only the expected case and let real bugs fail the build?
#endregion

namespace TimeWarp.Nuru.Build;

using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TimeWarp.Nuru.Generators;

public partial class GenerateNuruJsonContextTask
{
  /// <summary>
  /// Extracts return types from delegate routes using DslInterpreter.
  /// </summary>
  private void ExtractFromDelegateRoutes(
    CompilationUnitSyntax root,
    SemanticModel semanticModel,
    HashSet<string> jsonTypes,
    HashSet<string> jsonMemberTypes,
    CancellationToken cancellationToken)
  {
    try
    {
      // Use DslInterpreter for top-level statements
      DslInterpreter interpreter = new(semanticModel, cancellationToken);
      IReadOnlyList<AppModel> models = interpreter.InterpretTopLevelStatements(root);

      foreach (AppModel model in models)
      {
        foreach (RouteDefinition route in model.Routes)
        {
          string? returnTypeName = GetReturnTypeName(route.Handler.ReturnType);
          if (!string.IsNullOrEmpty(returnTypeName) && ShouldSerializeAsJson(returnTypeName))
          {
            jsonTypes.Add(returnTypeName);
            Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Found JSON type from delegate route: {returnTypeName}");
          }

          AddJsonMemberTypes(route, jsonMemberTypes);
        }
      }

      // Also check for method bodies containing DSL code
      foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
      {
        if (method.Body is not null)
        {
          try
          {
            IReadOnlyList<AppModel> methodModels = interpreter.Interpret(method.Body);
            foreach (AppModel model in methodModels)
            {
              foreach (RouteDefinition route in model.Routes)
              {
                AddJsonMemberTypes(route, jsonMemberTypes);
                string? returnTypeName = GetReturnTypeName(route.Handler.ReturnType);
                if (!string.IsNullOrEmpty(returnTypeName) && ShouldSerializeAsJson(returnTypeName))
                {
                  jsonTypes.Add(returnTypeName);
                  Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Found JSON type from delegate route in method: {returnTypeName}");
                }
              }
            }
          }
          catch
          {
            // Method doesn't contain DSL code - that's fine, continue
          }
        }
      }
    }
    catch (Exception ex)
    {
      // DSL interpretation failed - log and continue with endpoints
      Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: DSL interpretation skipped: {ex.Message}");
    }
  }

  /// <summary>
  /// Extracts return types from [NuruRoute] and [Operation] endpoint classes using EndpointExtractor.
  /// </summary>
  private void ExtractFromEndpoints(
    CompilationUnitSyntax root,
    SemanticModel semanticModel,
    HashSet<string> jsonTypes,
    HashSet<string> jsonMemberTypes,
    CancellationToken cancellationToken)
  {
    IEnumerable<TypeDeclarationSyntax> typeDeclarations = root
      .DescendantNodes()
      .OfType<TypeDeclarationSyntax>()
      .Where(static declaration => declaration is ClassDeclarationSyntax
        || (declaration is RecordDeclarationSyntax record
            && !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)))
      .Where(HasContractAttribute);

    foreach (TypeDeclarationSyntax classDecl in typeDeclarations)
    {
      try
      {
        EndpointExtractionResult result = EndpointExtractor.Extract(classDecl, semanticModel, cancellationToken);
        if (result.Route is not null)
          AddEndpointJsonTypes(result.Route, jsonTypes, jsonMemberTypes);
      }
      catch (Exception ex)
      {
        Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Failed to extract endpoint from {classDecl.Identifier}: {ex.Message}");
      }
    }
  }

  /// <summary>
  /// Extracts endpoint types that live in referenced assemblies, using the generator's opt-in rules:
  /// every endpoint in an assembly that <c>[assembly: NuruEndpointAssembly]</c> names, and the type
  /// argument of each <c>Map&lt;T&gt;()</c> that is a referenced endpoint type.
  /// </summary>
  private void ExtractFromReferencedEndpoints(
    CSharpCompilation compilation,
    List<SyntaxTree> syntaxTrees,
    HashSet<string> jsonTypes,
    HashSet<string> jsonMemberTypes,
    CancellationToken cancellationToken)
  {
    HashSet<INamedTypeSymbol> types = new(SymbolEqualityComparer.Default);
    foreach ((INamedTypeSymbol type, Location _) in ReferencedEndpointLocator.FromAssemblyAttributes(compilation, cancellationToken))
      types.Add(type);

    foreach (SyntaxTree tree in syntaxTrees)
    {
      SemanticModel semanticModel = compilation.GetSemanticModel(tree);
      foreach (InvocationExpressionSyntax invocation in tree.GetRoot(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>())
      {
        if (ReferencedEndpointLocator.MapTypeArgument(invocation, semanticModel, cancellationToken) is INamedTypeSymbol type)
          types.Add(type);
      }
    }

    foreach (INamedTypeSymbol type in types)
    {
      try
      {
        EndpointExtractionResult result = EndpointExtractor.Extract(type, compilation, Location.None, cancellationToken);
        if (result.Route is not null)
          AddEndpointJsonTypes(result.Route, jsonTypes, jsonMemberTypes);
      }
      catch (Exception ex)
      {
        Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Failed to extract referenced endpoint {type.Name}: {ex.Message}");
      }
    }
  }

  /// <summary>
  /// Registers an endpoint's JSON response type and JSON member types.
  /// </summary>
  private void AddEndpointJsonTypes(RouteDefinition route, HashSet<string> jsonTypes, HashSet<string> jsonMemberTypes)
  {
    string? returnTypeName = GetEndpointJsonTypeName(route.Handler);
    if (!string.IsNullOrEmpty(returnTypeName) && ShouldSerializeAsJson(returnTypeName))
    {
      jsonTypes.Add(returnTypeName);
      Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Found JSON type from endpoint: {returnTypeName}");
    }

    AddJsonMemberTypes(route, jsonMemberTypes);
  }

  /// <summary>
  /// Registers complex members that bind as one JSON value. Property names stay as declared.
  /// </summary>
  private void AddJsonMemberTypes(RouteDefinition route, HashSet<string> jsonMemberTypes)
  {
    foreach (OptionDefinition option in route.Options)
    {
      string? resolvedType = option.ResolvedClrTypeName;
      if (!option.IsJsonValue || string.IsNullOrEmpty(resolvedType))
        continue;

      string typeName = resolvedType.TrimEnd('?');
      if (jsonMemberTypes.Add(typeName))
      {
        Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Found JSON member type: {typeName}");
      }
    }
  }

  /// <summary>
  /// Gets the actual return type name from a HandlerReturnType.
  /// For Task&lt;T&gt;, returns T. For non-async, returns the full type name.
  /// </summary>
  private static string? GetReturnTypeName(HandlerReturnType returnType)
  {
    if (returnType.IsVoid)
      return null;

    // For Task<T>, use the unwrapped type
    if (returnType.IsTask && returnType.UnwrappedTypeName is not null)
      return returnType.UnwrappedTypeName;

    // For non-async with a value
    if (!returnType.IsTask && returnType.FullTypeName != "void")
      return returnType.FullTypeName;

    return null;
  }

  /// <summary>
  /// Gets the type an endpoint writes as JSON. A problem pair writes only its success value,
  /// and a value with a renderer is never serialized.
  /// </summary>
  private static string? GetEndpointJsonTypeName(HandlerDefinition handler)
  {
    if (handler.RendererTypeName is not null)
      return null;

    if (handler.ReturnsProblemPair)
      return handler.SuccessTypeName;

    return GetReturnTypeName(handler.ReturnType);
  }

  /// <summary>
  /// Checks if a class declaration has [NuruRoute] or [Operation].
  /// </summary>
  private static bool HasContractAttribute(TypeDeclarationSyntax classDecl)
  {
    return classDecl.AttributeLists
      .SelectMany(al => al.Attributes)
      .Any(attr =>
      {
        string name = attr.Name.ToString();
        return name is "NuruRoute" or "NuruRouteAttribute" or "Operation" or "OperationAttribute"
          || name.EndsWith(".NuruRoute", StringComparison.Ordinal)
          || name.EndsWith(".NuruRouteAttribute", StringComparison.Ordinal)
          || name.EndsWith(".Operation", StringComparison.Ordinal)
          || name.EndsWith(".OperationAttribute", StringComparison.Ordinal);
      });
  }

  /// <summary>
  /// Determines if a type should be serialized as JSON (not a primitive, Unit, etc.).
  /// </summary>
  private static bool ShouldSerializeAsJson(string typeName)
  {
    return typeName switch
    {
      // Unit = no output (void equivalent)
      "global::TimeWarp.Mediator.Unit" or "TimeWarp.Mediator.Unit" or "Unit" => false,

      // String = raw output
      "global::System.String" or "System.String" or "string" => false,

      // Numeric types = raw ToString()
      "global::System.Int32" or "System.Int32" or "int" => false,
      "global::System.Int64" or "System.Int64" or "long" => false,
      "global::System.Int16" or "System.Int16" or "short" => false,
      "global::System.Byte" or "System.Byte" or "byte" => false,
      "global::System.SByte" or "System.SByte" or "sbyte" => false,
      "global::System.UInt32" or "System.UInt32" or "uint" => false,
      "global::System.UInt64" or "System.UInt64" or "ulong" => false,
      "global::System.UInt16" or "System.UInt16" or "ushort" => false,
      "global::System.Single" or "System.Single" or "float" => false,
      "global::System.Double" or "System.Double" or "double" => false,
      "global::System.Decimal" or "System.Decimal" or "decimal" => false,
      "global::System.Boolean" or "System.Boolean" or "bool" => false,
      "global::System.Char" or "System.Char" or "char" => false,

      // Guid = raw
      "global::System.Guid" or "System.Guid" or "Guid" => false,

      // Date/Time types = ISO 8601
      "global::System.DateTime" or "System.DateTime" or "DateTime" => false,
      "global::System.DateTimeOffset" or "System.DateTimeOffset" or "DateTimeOffset" => false,
      "global::System.DateOnly" or "System.DateOnly" or "DateOnly" => false,
      "global::System.TimeOnly" or "System.TimeOnly" or "TimeOnly" => false,
      "global::System.TimeSpan" or "System.TimeSpan" or "TimeSpan" => false,

      // Everything else = JSON
      _ => true
    };
  }
}
