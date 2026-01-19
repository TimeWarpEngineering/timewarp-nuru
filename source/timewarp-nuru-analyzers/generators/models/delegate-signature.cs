namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents the signature of a delegate extracted from a Map() call.
/// Used by the source generator to create typed invokers.
/// </summary>
/// <param name="Parameters">The parameters of the delegate</param>
/// <param name="ReturnType">The return type information</param>
/// <param name="IsAsync">Whether the delegate returns a Task or Task&lt;T&gt;</param>
/// <param name="UniqueIdentifier">A unique identifier for code generation</param>
public sealed record DelegateSignature(
  ImmutableArray<DelegateParameterInfo> Parameters,
  DelegateTypeInfo ReturnType,
  bool IsAsync,
  string UniqueIdentifier)
{
  /// <summary>
  /// Creates a unique identifier based on parameter types and return type.
  /// </summary>
  public static string CreateIdentifier(ImmutableArray<DelegateParameterInfo> parameters, DelegateTypeInfo returnType)
  {
    ArgumentNullException.ThrowIfNull(returnType);
    System.Text.StringBuilder sb = new();

    foreach (DelegateParameterInfo param in parameters)
    {
      if (sb.Length > 0)
        sb.Append('_');

      sb.Append(param.Type.ShortName);
      if (param.IsArray)
        sb.Append("Array");
    }

    if (returnType.FullName != "void")
    {
      sb.Append("_Returns_");
      sb.Append(returnType.ShortName);
    }

    return sb.Length > 0 ? sb.ToString() : "NoParams";
  }
}

/// <summary>
/// Represents a parameter in a delegate signature.
/// </summary>
/// <param name="Name">The parameter name</param>
/// <param name="Type">The type information</param>
/// <param name="IsArray">Whether the parameter is an array type</param>
/// <param name="IsNullable">Whether the parameter is nullable</param>
public sealed record DelegateParameterInfo(
  string Name,
  DelegateTypeInfo Type,
  bool IsArray,
  bool IsNullable);

/// <summary>
/// Represents type information for code generation.
/// Named DelegateTypeInfo to avoid conflict with Microsoft.CodeAnalysis.TypeInfo.
/// </summary>
/// <param name="FullName">The fully qualified type name (e.g., "global::System.String")</param>
/// <param name="ShortName">The short type name for identifiers (e.g., "String")</param>
/// <param name="IsVoid">Whether this is the void type</param>
/// <param name="IsTask">Whether this is Task or Task&lt;T&gt;</param>
/// <param name="TaskResultType">For Task&lt;T&gt;, the T type; null otherwise</param>
public sealed record DelegateTypeInfo(
  string FullName,
  string ShortName,
  bool IsVoid,
  bool IsTask,
  DelegateTypeInfo? TaskResultType)
{
  /// <summary>
  /// Creates DelegateTypeInfo from a Roslyn type symbol.
  /// </summary>
  public static DelegateTypeInfo FromSymbol(ITypeSymbol symbol)
  {
    ArgumentNullException.ThrowIfNull(symbol);
    string fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string shortName = GetShortName(symbol);
    bool isVoid = symbol.SpecialType == SpecialType.System_Void;
    bool isTask = IsTaskType(symbol, out ITypeSymbol? taskResultSymbol);
    DelegateTypeInfo? taskResultType = taskResultSymbol is not null
      ? FromSymbol(taskResultSymbol)
      : null;

    return new DelegateTypeInfo(fullName, shortName, isVoid, isTask, taskResultType);
  }

  private static string GetShortName(ITypeSymbol symbol)
  {
    // Handle array types
    if (symbol is IArrayTypeSymbol arrayType)
      return GetShortName(arrayType.ElementType) + "Array";

    // Handle nullable value types
    if (symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
        symbol is INamedTypeSymbol nullableType &&
        nullableType.TypeArguments.Length > 0)
    {
      return "Nullable" + GetShortName(nullableType.TypeArguments[0]);
    }

    // Handle Task<T>
    if (symbol is INamedTypeSymbol namedType &&
        namedType.Name == "Task" &&
        namedType.TypeArguments.Length == 1)
    {
      return "Task" + GetShortName(namedType.TypeArguments[0]);
    }

    // Map special types to simple names
    return symbol.SpecialType switch
    {
      SpecialType.System_Boolean => "Bool",
      SpecialType.System_Byte => "Byte",
      SpecialType.System_SByte => "SByte",
      SpecialType.System_Int16 => "Short",
      SpecialType.System_UInt16 => "UShort",
      SpecialType.System_Int32 => "Int",
      SpecialType.System_UInt32 => "UInt",
      SpecialType.System_Int64 => "Long",
      SpecialType.System_UInt64 => "ULong",
      SpecialType.System_Single => "Float",
      SpecialType.System_Double => "Double",
      SpecialType.System_Decimal => "Decimal",
      SpecialType.System_Char => "Char",
      SpecialType.System_String => "String",
      SpecialType.System_Object => "Object",
      _ => symbol.Name
    };
  }

  private static bool IsTaskType(ITypeSymbol symbol, out ITypeSymbol? resultType)
  {
    resultType = null;

    if (symbol is not INamedTypeSymbol namedType)
      return false;

    // Check for Task or Task<T> in System.Threading.Tasks namespace
    if (namedType.ContainingNamespace?.ToDisplayString() != "System.Threading.Tasks")
      return false;

    if (namedType.Name == "Task")
    {
      if (namedType.TypeArguments.Length == 1)
      {
        resultType = namedType.TypeArguments[0];
      }

      return true;
    }

    if (namedType.Name == "ValueTask")
    {
      if (namedType.TypeArguments.Length == 1)
      {
        resultType = namedType.TypeArguments[0];
      }

      return true;
    }

    return false;
  }
}

/// <summary>
/// Extended route information that includes delegate signature.
/// </summary>
/// <param name="Pattern">The route pattern string</param>
/// <param name="Location">The source location for diagnostics</param>
/// <param name="Signature">The extracted delegate signature, if available</param>
public sealed record RouteWithSignature(
  string Pattern,
  Location Location,
  DelegateSignature? Signature);
