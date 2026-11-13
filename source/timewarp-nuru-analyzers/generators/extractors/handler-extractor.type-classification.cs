#region Purpose
// Type classification helpers: service, collection and route-bindable types (HandlerExtractor partial).
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class HandlerExtractor
{
  /// <summary>
  /// Checks if a type name represents CancellationToken.
  /// </summary>
  private static bool IsCancellationTokenType(string typeName)
  {
    return typeName == "global::System.Threading.CancellationToken" ||
           typeName == "CancellationToken" ||
           typeName == "System.Threading.CancellationToken";
  }

  /// <summary>
  /// Checks whether a type binds with <c>EnumTypeConverter</c>: a direct enum,
  /// <c>Nullable&lt;TEnum&gt;</c>, an array of either, or a collection interface
  /// (<c>IEnumerable&lt;T&gt;</c>/<c>IList&lt;T&gt;</c>/<c>ICollection&lt;T&gt;</c>) whose
  /// element type is enum-bindable. Used for both single and repeated options.
  /// </summary>
  internal static bool IsEnumBindableType(ITypeSymbol type)
  {
    // Unwrap Nullable<T>
    if (type is INamedTypeSymbol nullableType &&
        nullableType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
        nullableType.TypeArguments.Length == 1)
    {
      type = nullableType.TypeArguments[0];
    }

    if (type.TypeKind == TypeKind.Enum)
      return true;

    if (type is IArrayTypeSymbol arrayType)
      return IsEnumBindableType(arrayType.ElementType);

    if (type is INamedTypeSymbol namedType &&
        IsCollectionInterface(namedType) &&
        namedType.TypeArguments.Length == 1)
    {
      return IsEnumBindableType(namedType.TypeArguments[0]);
    }

    return false;
  }

  /// <summary>
  /// Checks if a type is a service (interface or known service types), using symbol
  /// inspection rather than name heuristics. Preferred over the string overload below;
  /// used at every call site where a symbol is available.
  /// Excludes built-in route-bindable types like IPAddress, IList&lt;T&gt;, etc.
  /// </summary>
  private static bool IsServiceType(ITypeSymbol type)
  {
    // First, check if it's a built-in route-bindable type
    // These should NEVER be treated as services even if they are interfaces (e.g. IList<T>)
    if (IsBuiltInRouteBindableType(type))
      return false;

    // Collection interfaces bind from the route when the element is not itself a service
    // (e.g. IEnumerable<MyEnum>, IList<string>). Only treat as DI when the element is
    // service-like (e.g. IEnumerable<IGreeter>).
    if (type is INamedTypeSymbol namedCollection &&
        IsCollectionInterface(namedCollection) &&
        namedCollection.TypeArguments.Length == 1)
    {
      return IsServiceType(namedCollection.TypeArguments[0]);
    }

    // Known service namespaces (Microsoft.Extensions.*), checked by symbol namespace
    // rather than string prefix matching. Covers non-interface DI types too.
    string? namespaceName = type.ContainingNamespace?.ToDisplayString();
    if (namespaceName?.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) == true)
      return true;

    // ILogger/ILogger<T>/IServiceProvider, matched by symbol name/OriginalDefinition,
    // whether implemented directly or via an interface in the type's interface list.
    if (IsLoggerOrServiceProvider(type))
      return true;

    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (IsLoggerOrServiceProvider(iface))
        return true;
    }

    // Any other interface is treated as a user-defined service (IGreeter, IData, etc.).
    // Scalar route-bindable types (IPAddress, primitives) were excluded above via
    // IsBuiltInRouteBindableType.
    return type.TypeKind == TypeKind.Interface;
  }

  /// <summary>
  /// Checks whether a type is IEnumerable&lt;T&gt;, IList&lt;T&gt;, or ICollection&lt;T&gt;
  /// from System.Collections.Generic, matched by OriginalDefinition.
  /// </summary>
  private static bool IsCollectionInterface(INamedTypeSymbol type)
  {
    if (type.TypeKind != TypeKind.Interface)
      return false;

    if (type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
      return true;

    if (type.OriginalDefinition.ContainingNamespace?.ToDisplayString() != "System.Collections.Generic")
      return false;

    return type.OriginalDefinition.Name is "IList" or "ICollection";
  }

  /// <summary>
  /// Checks whether a type symbol is Microsoft.Extensions.Logging.ILogger(&lt;T&gt;) or
  /// System.IServiceProvider, matched by OriginalDefinition name/namespace rather than
  /// a fragile string.Contains check.
  /// </summary>
  private static bool IsLoggerOrServiceProvider(ITypeSymbol type)
  {
    if (type is not INamedTypeSymbol namedType)
      return false;

    INamedTypeSymbol original = namedType.OriginalDefinition;
    return original.Name switch
    {
      "ILogger" => original.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.Logging",
      "IServiceProvider" => original.ContainingNamespace?.ToDisplayString() == "System",
      _ => false
    };
  }

  /// <summary>
  /// Checks if a type name appears to be a service (interface or known service types).
  /// Excludes built-in route-bindable types like IPAddress, IList, etc.
  /// Fallback for the two syntax-only lambda paths (ExtractFromLambda/ExtractFromAnonymousMethod)
  /// which run only when GetSymbolInfo does not resolve to an IMethodSymbol — i.e.
  /// partial/uncompilable code. Valid code always resolves a symbol and uses the
  /// IsServiceType(ITypeSymbol) overload above.
  /// </summary>
  private static bool IsServiceType(string typeName)
  {
    // First, check if it's a built-in route-bindable type
    // These should NEVER be treated as services even if they look like interfaces
    if (IsBuiltInRouteBindableType(typeName))
      return false;

    // Common service patterns
    if (typeName.StartsWith("global::Microsoft.Extensions.", StringComparison.Ordinal))
      return true;

    if (typeName.Contains("ILogger", StringComparison.Ordinal))
      return true;

    if (typeName.Contains("IServiceProvider", StringComparison.Ordinal))
      return true;

    // Check if it's an interface (starts with I followed by uppercase letter)
    // This handles user-defined service interfaces like IGreeter, IFormatter, etc.
    string shortName = GetShortTypeName(typeName);
    if (shortName.Length >= 2 && shortName[0] == 'I' && char.IsUpper(shortName[1]))
      return true;

    return false;
  }

  /// <summary>
  /// Checks if a type is a built-in route-bindable type from TypeConversionMap, using
  /// SpecialType for primitives/string and falling back to fully-qualified name matching
  /// (against the same set the string overload hardcodes) for the remaining CLR types
  /// that have no SpecialType (Guid, DateTime, Uri, IPAddress, etc.).
  /// </summary>
  private static bool IsBuiltInRouteBindableType(ITypeSymbol type)
  {
    switch (type.SpecialType)
    {
      case SpecialType.System_Int32:
      case SpecialType.System_Int64:
      case SpecialType.System_Int16:
      case SpecialType.System_Byte:
      case SpecialType.System_SByte:
      case SpecialType.System_UInt16:
      case SpecialType.System_UInt32:
      case SpecialType.System_UInt64:
      case SpecialType.System_Single:
      case SpecialType.System_Double:
      case SpecialType.System_Decimal:
      case SpecialType.System_Boolean:
      case SpecialType.System_Char:
      case SpecialType.System_String:
        return true;
    }

    string fullyQualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    return IsBuiltInRouteBindableType(fullyQualifiedName);
  }

  /// <summary>
  /// Checks if a type is a built-in route-bindable type from TypeConversionMap.
  /// These types (like IPAddress, IList) should be bound from route parameters, not injected as services.
  /// </summary>
  private static bool IsBuiltInRouteBindableType(string typeName)
  {
    // Map fully-qualified type names to their built-in status
    // This list must stay in sync with TypeConversionMap.GetClrTypeName()
    return typeName switch
    {
      // Primitive types (unlikely to be confused with services, but included for completeness)
      "global::System.Int32" or "int" => true,
      "global::System.Int64" or "long" => true,
      "global::System.Int16" or "short" => true,
      "global::System.Byte" or "byte" => true,
      "global::System.SByte" or "sbyte" => true,
      "global::System.UInt16" or "ushort" => true,
      "global::System.UInt32" or "uint" => true,
      "global::System.UInt64" or "ulong" => true,
      "global::System.Single" or "float" => true,
      "global::System.Double" or "double" => true,
      "global::System.Decimal" or "decimal" => true,
      "global::System.Boolean" or "bool" => true,
      "global::System.Char" or "char" => true,
      "global::System.String" or "string" => true,

      // System value types
      "global::System.Guid" => true,
      "global::System.DateTime" => true,
      "global::System.DateTimeOffset" => true,
      "global::System.TimeSpan" => true,
      "global::System.DateOnly" => true,
      "global::System.TimeOnly" => true,

      // Reference types that could be confused with interfaces/services
      "global::System.Uri" => true,
      "global::System.Version" => true,
      "global::System.IO.FileInfo" => true,
      "global::System.IO.DirectoryInfo" => true,
      "global::System.Net.IPAddress" => true,  // Key one! Starts with 'I' but is a class

      _ => false
    };
  }

  /// <summary>
  /// Gets the short type name from a fully qualified type name.
  /// </summary>
  private static string GetShortTypeName(string typeName)
  {
    // Remove global:: prefix
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      typeName = typeName[8..];

    // Get last segment after the final dot
    int lastDot = typeName.LastIndexOf('.', StringComparison.Ordinal);
    return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
  }

  /// <summary>
  /// Normalizes a type name to fully qualified form.
  /// </summary>
  private static string NormalizeTypeName(string typeName)
  {
    return typeName switch
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
      "string" => "global::System.String",
      "object" => "global::System.Object",
      "void" => "void",
      _ when typeName.StartsWith("global::", StringComparison.Ordinal) => typeName,
      _ => $"global::{typeName}"
    };
  }
}
