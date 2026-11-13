#region Purpose
// Options type discovery, validator lookup and service binding creation (HandlerExtractor partial).
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class HandlerExtractor
{
  /// <summary>
  /// Checks if a type is IOptions&lt;T&gt; and extracts the configuration section key and validator type.
  /// </summary>
  /// <param name="typeSymbol">The type symbol to check.</param>
  /// <param name="compilation">The compilation to search for validators.</param>
  /// <param name="configurationKey">The extracted configuration key (from [ConfigurationKey] or convention).</param>
  /// <param name="validatorTypeName">The validator type implementing IValidateOptions&lt;T&gt;, if found.</param>
  /// <returns>True if the type is IOptions&lt;T&gt;, false otherwise.</returns>
  private static bool TryGetOptionsInfo(
    ITypeSymbol typeSymbol,
    Compilation? compilation,
    out string? configurationKey,
    out string? validatorTypeName)
  {
    configurationKey = null;
    validatorTypeName = null;

    // Check if it's IOptions<T>
    if (typeSymbol is not INamedTypeSymbol namedType)
      return false;

    string typeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // Match IOptions<T> patterns
    if (!typeName.StartsWith("global::Microsoft.Extensions.Options.IOptions<", StringComparison.Ordinal))
      return false;

    // Get the inner type T from IOptions<T>
    if (namedType.TypeArguments.Length != 1)
      return false;

    ITypeSymbol innerType = namedType.TypeArguments[0];

    // Look for [ConfigurationKey("...")] attribute on the inner type
    foreach (AttributeData attribute in innerType.GetAttributes())
    {
      string? attrName = attribute.AttributeClass?.Name;
      if (attrName is "ConfigurationKeyAttribute" or "ConfigurationKey")
      {
        // Get the key from the first constructor argument
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string key)
        {
          configurationKey = key;
          break;
        }
      }
    }

    // Fall back to convention: strip "Options" suffix from class name
    if (configurationKey is null)
    {
      string innerTypeName = innerType.Name;
      const string OptionsSuffix = "Options";
      if (innerTypeName.EndsWith(OptionsSuffix, StringComparison.Ordinal) && innerTypeName.Length > OptionsSuffix.Length)
      {
        configurationKey = innerTypeName[..^OptionsSuffix.Length];
      }
      else
      {
        configurationKey = innerTypeName;
      }
    }

    // Search for IValidateOptions<T> implementations
    if (compilation is not null)
    {
      validatorTypeName = FindValidatorForOptionsType(innerType, compilation);
    }

    return true;
  }

  /// <summary>
  /// Searches the compilation for a type implementing IValidateOptions&lt;T&gt; for the given options type.
  /// </summary>
  /// <param name="optionsType">The options type T.</param>
  /// <param name="compilation">The compilation to search.</param>
  /// <returns>The fully qualified validator type name, or null if not found.</returns>
  /// <exception cref="InvalidOperationException">Thrown if multiple validators are found for the same options type.</exception>
  private static string? FindValidatorForOptionsType(ITypeSymbol optionsType, Compilation compilation)
  {
    List<INamedTypeSymbol> validators = [];

    // Search all types in the compilation
    foreach (INamedTypeSymbol type in GetAllTypes(compilation.GlobalNamespace))
    {
      // Skip abstract types and interfaces
      if (type.IsAbstract || type.TypeKind == TypeKind.Interface)
        continue;

      // Check if it implements IValidateOptions<T>
      foreach (INamedTypeSymbol iface in type.AllInterfaces)
      {
        if (iface.Name == "IValidateOptions" &&
            iface.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.Options" &&
            iface.TypeArguments.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], optionsType))
        {
          validators.Add(type);
          break;
        }
      }
    }

    if (validators.Count == 0)
      return null;

    if (validators.Count > 1)
    {
      string optionsTypeName = optionsType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
      string validatorNames = string.Join(", ", validators.Select(v => v.Name));
      throw new InvalidOperationException(
        $"Multiple validators found for {optionsTypeName}: {validatorNames}. " +
        $"Only one IValidateOptions<{optionsTypeName}> implementation is allowed.");
    }

    return validators[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
  }

  /// <summary>
  /// Recursively gets all types in a namespace and its nested namespaces.
  /// </summary>
  private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
  {
    foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
    {
      yield return type;

      // Include nested types
      foreach (INamedTypeSymbol nestedType in GetNestedTypes(type))
      {
        yield return nestedType;
      }
    }

    foreach (INamespaceSymbol childNamespace in namespaceSymbol.GetNamespaceMembers())
    {
      foreach (INamedTypeSymbol type in GetAllTypes(childNamespace))
      {
        yield return type;
      }
    }
  }

  /// <summary>
  /// Recursively gets all nested types within a type.
  /// </summary>
  private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
  {
    foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
    {
      yield return nestedType;

      foreach (INamedTypeSymbol deeplyNested in GetNestedTypes(nestedType))
      {
        yield return deeplyNested;
      }
    }
  }

  /// <summary>
  /// Creates a service parameter binding, detecting IOptions&lt;T&gt; and extracting configuration key and validator.
  /// </summary>
  /// <param name="param">The parameter symbol.</param>
  /// <param name="typeName">The fully qualified type name.</param>
  /// <param name="compilation">Optional compilation for validator discovery.</param>
  private static ParameterBinding CreateServiceBinding(IParameterSymbol param, string typeName, Compilation? compilation = null)
  {
    string? configurationKey = null;
    string? validatorTypeName = null;

    // Check if it's IOptions<T> and extract the configuration key and validator
    if (TryGetOptionsInfo(param.Type, compilation, out string? key, out string? validator))
    {
      configurationKey = key;
      validatorTypeName = validator;
    }

    return ParameterBinding.FromService(param.Name, typeName, configurationKey: configurationKey, validatorTypeName: validatorTypeName);
  }
}
