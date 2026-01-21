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
/// - EndpointExtractor for [NuruRoute] attributed classes
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
      Log.LogWarning($"TimeWarp.Nuru.Build: Failed to generate JSON context: {ex.Message}");
      Log.LogMessage(MessageImportance.Low, $"Stack trace: {ex.StackTrace}");
      GeneratedFiles = [];
      return true; // Don't fail the build - fallback to ToString() will work
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
    CancellationToken cancellationToken = CancellationToken.None;

    foreach (SyntaxTree tree in syntaxTrees)
    {
      SemanticModel semanticModel = compilation.GetSemanticModel(tree);
      CompilationUnitSyntax root = tree.GetCompilationUnitRoot(cancellationToken);

      // 4a. Extract from delegate routes using DslInterpreter
      ExtractFromDelegateRoutes(root, semanticModel, jsonTypes, cancellationToken);

      // 4b. Extract from [NuruRoute] attributed classes
      ExtractFromEndpoints(root, semanticModel, jsonTypes, cancellationToken);
    }

    // 5. If no types need JSON serialization, skip generation
    if (jsonTypes.Count == 0)
    {
      Log.LogMessage(MessageImportance.Normal, "TimeWarp.Nuru.Build: No user-defined types need JSON serialization");
      GeneratedFiles = [];
      return true;
    }

    // 6. Generate the JsonSerializerContext file
    string outputFile = Path.Combine(IntermediateOutputPath, "NuruUserTypesJsonContext.g.cs");
    string content = GenerateContextFile(jsonTypes);

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
  /// Extracts return types from [NuruRoute] attributed classes using EndpointExtractor.
  /// </summary>
  private void ExtractFromEndpoints(
    CompilationUnitSyntax root,
    SemanticModel semanticModel,
    HashSet<string> jsonTypes,
    CancellationToken cancellationToken)
  {
    // Find all class declarations with [NuruRoute] attribute
    IEnumerable<ClassDeclarationSyntax> classDeclarations = root
      .DescendantNodes()
      .OfType<ClassDeclarationSyntax>()
      .Where(HasNuruRouteAttribute);

    foreach (ClassDeclarationSyntax classDecl in classDeclarations)
    {
      try
      {
        // Use EndpointExtractor to get the full RouteDefinition
        RouteDefinition? route = EndpointExtractor.Extract(classDecl, semanticModel, cancellationToken);
        if (route is not null)
        {
          string? returnTypeName = GetReturnTypeName(route.Handler.ReturnType);
          if (!string.IsNullOrEmpty(returnTypeName) && ShouldSerializeAsJson(returnTypeName))
          {
            jsonTypes.Add(returnTypeName);
            Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Found JSON type from [NuruRoute]: {returnTypeName}");
          }
        }
      }
      catch (Exception ex)
      {
        Log.LogMessage(MessageImportance.Low, $"TimeWarp.Nuru.Build: Failed to extract endpoint from {classDecl.Identifier}: {ex.Message}");
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
  /// Checks if a class declaration has a [NuruRoute] attribute.
  /// </summary>
  private static bool HasNuruRouteAttribute(ClassDeclarationSyntax classDecl)
  {
    return classDecl.AttributeLists
      .SelectMany(al => al.Attributes)
      .Any(attr =>
      {
        string name = attr.Name.ToString();
        return name is "NuruRoute" or "NuruRouteAttribute"
          || name.EndsWith(".NuruRoute", StringComparison.Ordinal)
          || name.EndsWith(".NuruRouteAttribute", StringComparison.Ordinal);
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
      "global::TimeWarp.Nuru.Unit" or "TimeWarp.Nuru.Unit" or "Unit" => false,

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
  private static string GenerateContextFile(HashSet<string> types)
  {
    StringBuilder sb = new();

    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("// Generated by TimeWarp.Nuru.Build for AOT-compatible JSON serialization");
    sb.AppendLine("#pragma warning disable");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine("namespace TimeWarp.Nuru.Generated;");
    sb.AppendLine();

    // Emit [JsonSerializable] attribute for each type
    foreach (string typeName in types.OrderBy(t => t, StringComparer.Ordinal))
    {
      string fullTypeName = EnsureGlobalPrefix(typeName);
      sb.AppendLine($"[global::System.Text.Json.Serialization.JsonSerializable(typeof({fullTypeName}))]");
    }

    // Emit the context class
    sb.AppendLine("[global::System.Text.Json.Serialization.JsonSourceGenerationOptions(");
    sb.AppendLine("  PropertyNamingPolicy = global::System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase)]");
    sb.AppendLine("internal partial class NuruUserTypesJsonContext");
    sb.AppendLine("  : global::System.Text.Json.Serialization.JsonSerializerContext;");

    return sb.ToString();
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
