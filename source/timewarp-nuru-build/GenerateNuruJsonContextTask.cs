// MSBuild task that generates a JsonSerializerContext for user-defined return types.
// Runs before CoreCompile so System.Text.Json source generator can process the output.
//
// Uses the same IR infrastructure as the source generator (DslInterpreter, EndpointExtractor)
// to extract return types from BOTH delegate routes AND endpoints.

namespace TimeWarp.Nuru.Build;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TimeWarp.Nuru.Generators;

/// <summary>
/// MSBuild task that generates a JsonSerializerContext with [JsonSerializable] attributes
/// for user-defined return types discovered from both delegate routes and [NuruRoute] classes.
/// </summary>
/// <remarks>
/// This task uses the same IR infrastructure as the source generator:
/// - DslInterpreter for delegate routes (.Map(...).WithHandler(...))
/// - EndpointExtractor for [NuruRoute] endpoint classes
/// This ensures consistent extraction logic and complete coverage of all route types.
/// </remarks>
public class GenerateNuruJsonContextTask : Task
{
  /// <summary>
  /// Source files to analyze for routes.
  /// </summary>
  [Required]
  public ITaskItem[] SourceFiles { get; set; } = [];

  /// <summary>
  /// The intermediate output path where the generated file will be written.
  /// </summary>
  [Required]
  public string IntermediateOutputPath { get; set; } = "";

  /// <summary>
  /// Assembly references needed for semantic analysis.
  /// </summary>
  public ITaskItem[] References { get; set; } = [];

  /// <summary>
  /// The generated files that should be included in compilation.
  /// </summary>
  [Output]
  public ITaskItem[] GeneratedFiles { get; set; } = [];

  /// <inheritdoc/>
  public override bool Execute()
  {
    try
    {
      return ExecuteCore();
    }
    catch (Exception ex)
    {
      // Unexpected extraction/generation bugs must not silently fall back to
      // ToString() (470 M24). Expected "no DSL in this compilation unit" cases
      // are handled inside ExecuteCore / ExtractFromDelegateRoutes as fail-soft
      // LogMessage paths — they do not reach this catch.
      Log.LogError($"TimeWarp.Nuru.Build: Failed to generate JSON context: {ex.GetType().Name}: {ex.Message}");
      Log.LogMessage(MessageImportance.High, $"TimeWarp.Nuru.Build: Stack trace: {ex.StackTrace}");
      GeneratedFiles = [];
      return false;
    }
  }

  private bool ExecuteCore()
  {
    if (SourceFiles.Length == 0)
    {
      Log.LogMessage(MessageImportance.Low, "TimeWarp.Nuru.Build: No source files to analyze");
      GeneratedFiles = [];
      return true;
    }

    // 1. Parse source files into syntax trees
    List<SyntaxTree> syntaxTrees = [];
    foreach (ITaskItem sourceFile in SourceFiles)
    {
      string filePath = sourceFile.ItemSpec;
      if (!File.Exists(filePath))
        continue;

      try
      {
        string sourceText = File.ReadAllText(filePath);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceText, path: filePath);
        syntaxTrees.Add(tree);
      }
      catch (Exception ex)
      {
        Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Failed to parse {filePath}: {ex.Message}");
      }
    }

    if (syntaxTrees.Count == 0)
    {
      Log.LogMessage(MessageImportance.Low, "TimeWarp.Nuru.Build: No syntax trees parsed");
      GeneratedFiles = [];
      return true;
    }

    // 2. Create metadata references
    List<MetadataReference> references = [];
    foreach (ITaskItem reference in References)
    {
      string refPath = reference.ItemSpec;
      if (File.Exists(refPath))
      {
        try
        {
          references.Add(MetadataReference.CreateFromFile(refPath));
        }
        catch (Exception ex)
        {
          Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Failed to load reference {refPath}: {ex.Message}");
        }
      }
    }

    // 3. Create compilation
    CSharpCompilation compilation = CSharpCompilation.Create(
      "NuruJsonContextAnalysis",
      syntaxTrees,
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    // 4. Extract return types from all routes using the IR infrastructure
    HashSet<string> jsonTypes = [];
    HashSet<string> jsonMemberTypes = [];
    CancellationToken cancellationToken = CancellationToken.None;

    foreach (SyntaxTree tree in syntaxTrees)
    {
      SemanticModel semanticModel = compilation.GetSemanticModel(tree);
      CompilationUnitSyntax root = tree.GetCompilationUnitRoot(cancellationToken);

      // 4a. Extract from delegate routes using DslInterpreter
      ExtractFromDelegateRoutes(root, semanticModel, jsonTypes, jsonMemberTypes, cancellationToken);

      // 4b. Extract from [NuruRoute] and [Operation] endpoint classes
      ExtractFromEndpoints(root, semanticModel, jsonTypes, jsonMemberTypes, cancellationToken);
    }

    // 4c. Endpoints in referenced assemblies: [assembly: NuruEndpointAssembly] and Map<T>()
    ExtractFromReferencedEndpoints(compilation, syntaxTrees, jsonTypes, jsonMemberTypes, cancellationToken);

    // 5. If no types need JSON serialization, skip generation
    if (jsonTypes.Count == 0 && jsonMemberTypes.Count == 0)
    {
      Log.LogMessage(MessageImportance.Normal, "TimeWarp.Nuru.Build: No user-defined types need JSON serialization");
      GeneratedFiles = [];
      return true;
    }

    // 6. Generate the JsonSerializerContext file
    string outputFile = Path.Combine(IntermediateOutputPath, "NuruUserTypesJsonContext.g.cs");
    string content = GenerateContextFile(
      JsonContextTypeNames.Assign(compilation, jsonTypes),
      JsonContextTypeNames.Assign(compilation, jsonMemberTypes));

    // Ensure directory exists
    string? directory = Path.GetDirectoryName(outputFile);
    if (!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    File.WriteAllText(outputFile, content);

    GeneratedFiles = [new TaskItem(outputFile)];
    Log.LogMessage(MessageImportance.Normal,
      $"TimeWarp.Nuru.Build: Generated {outputFile} with {jsonTypes.Count} type(s): {string.Join(", ", jsonTypes)}");

    return true;
  }

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

  /// <summary>
  /// Generates the JsonSerializerContext file content.
  /// </summary>
  private static string GenerateContextFile(
    IReadOnlyList<JsonContextRegistration> types,
    IReadOnlyList<JsonContextRegistration> jsonMemberTypes)
  {
    StringBuilder sb = new();

    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("// Generated by TimeWarp.Nuru.Build for AOT-compatible JSON serialization");
    sb.AppendLine("#pragma warning disable");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine("namespace TimeWarp.Nuru.Generated;");
    sb.AppendLine();

    if (types.Count > 0)
    {
      AppendRegistrations(sb, types);

      // Return values keep the camelCase output used by the existing JSON strategy.
      sb.AppendLine("[global::System.Text.Json.Serialization.JsonSourceGenerationOptions(");
      sb.AppendLine("  PropertyNamingPolicy = global::System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase)]");
      sb.AppendLine("internal partial class NuruUserTypesJsonContext");
      sb.AppendLine("  : global::System.Text.Json.Serialization.JsonSerializerContext;");
      sb.AppendLine();
    }

    if (jsonMemberTypes.Count > 0)
    {
      AppendRegistrations(sb, jsonMemberTypes);

      // JSON members keep the declared property names. --json-args keys are those names.
      // Nested names match ignoring case, so an MCP client's camelCase {"tool":...} binds Tool.
      sb.AppendLine("[global::System.Text.Json.Serialization.JsonSourceGenerationOptions(");
      sb.AppendLine("  PropertyNameCaseInsensitive = true)]");
      sb.AppendLine("internal partial class NuruJsonMemberContext");
      sb.AppendLine("  : global::System.Text.Json.Serialization.JsonSerializerContext;");
    }

    return sb.ToString();
  }

  /// <summary>
  /// Writes one [JsonSerializable] per type. A type whose STJ name collides gets its own
  /// TypeInfoPropertyName (SYSLIB1031).
  /// </summary>
  private static void AppendRegistrations(StringBuilder sb, IReadOnlyList<JsonContextRegistration> registrations)
  {
    foreach (JsonContextRegistration registration in registrations)
    {
      string fullTypeName = EnsureGlobalPrefix(registration.TypeName);
      string propertyName = registration.TypeInfoPropertyName is string name
        ? $", TypeInfoPropertyName = \"{name}\""
        : "";
      sb.AppendLine($"[global::System.Text.Json.Serialization.JsonSerializable(typeof({fullTypeName}){propertyName})]");
    }
  }

  /// <summary>
  /// Ensures a type name has the global:: prefix.
  /// </summary>
  private static string EnsureGlobalPrefix(string typeName)
  {
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      return typeName;

    // If it already has a namespace, add global::
    if (typeName.Contains('.'))
      return $"global::{typeName}";

    // Simple type name - return as is (likely a local type in the project)
    return typeName;
  }
}
