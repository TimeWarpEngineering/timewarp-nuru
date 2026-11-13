#region Purpose
// MSBuild task that generates a JsonSerializerContext for user-defined return types before CoreCompile.
// Uses the same IR infrastructure as the source generator (DslInterpreter, EndpointExtractor) to extract
// return types from BOTH delegate routes AND endpoints.
#endregion

namespace TimeWarp.Nuru.Build;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
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
public partial class GenerateNuruJsonContextTask : Task
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
