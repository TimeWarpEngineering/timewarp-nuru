#region Purpose
// Generator model for whether a handler result can be null, read from the semantic model so the
// invoker emitter does not have to guess from a type name.
#endregion

#region Design
// Kanban 583: the emitter knew only BCL value types by name, so a user enum or struct result got an
// `is not null` guard (CS0037) and a `?.ToString()` fallback (CS0023). The extractors classify the result
// type once from its ITypeSymbol and carry the kind as a plain enum, which keeps the model equatable for
// the incremental pipeline. Nullable<T> is a value type at the CLR level but can be null, so it is MaybeNull.
//
// Kanban 585: C# cannot name an anonymous type, so generated code that declares `<anonymous type: ...> result`
// or registers it in [JsonSerializable(typeof(...))] does not compile, and System.Text.Json source generation
// cannot serialize it. A result whose type is or contains an anonymous type (new { ... }, List<anon>, anon[])
// is Anonymous: the extractor erases its type name to object, the invoker prints it through ToString(), and the
// JSON context task skips it. Anonymous types are reference types, so Anonymous results get the null guard.
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Whether a handler result value can be null, and whether it is an enum.
/// </summary>
public enum ResultValueKind
{
  /// <summary>A reference type, <c>Nullable&lt;T&gt;</c>, a type parameter, or a type the extractor could not read.</summary>
  MaybeNull,

  /// <summary>A non-nullable value type other than an enum (a struct or a BCL primitive).</summary>
  ValueType,

  /// <summary>A non-nullable enum.</summary>
  Enum,

  /// <summary>
  /// A type that is or contains an anonymous type. Generated code holds the value as <c>object</c> and prints
  /// it through <c>ToString()</c>, because no code, and no JSON context, can name the type (kanban 585).
  /// </summary>
  Anonymous
}

/// <summary>
/// Reads a <see cref="ResultValueKind"/> from a type symbol.
/// </summary>
internal static class ResultValueKinds
{
  /// <summary>
  /// Classifies <paramref name="type"/>. A null symbol is <see cref="ResultValueKind.MaybeNull"/>.
  /// </summary>
  public static ResultValueKind Of(ITypeSymbol? type)
  {
    if (type is not null && ContainsAnonymousType(type))
      return ResultValueKind.Anonymous;

    if (type?.IsValueType != true)
      return ResultValueKind.MaybeNull;

    if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
      return ResultValueKind.MaybeNull;

    return type.TypeKind == TypeKind.Enum ? ResultValueKind.Enum : ResultValueKind.ValueType;
  }

  /// <summary>
  /// Whether a result of this kind can be null in generated code, so output needs a null guard.
  /// </summary>
  public static bool CanBeNull(ResultValueKind kind) =>
    kind is ResultValueKind.MaybeNull or ResultValueKind.Anonymous;

  /// <summary>
  /// Returns true when <paramref name="type"/> is an anonymous type or has one as a type argument
  /// or array element, at any depth.
  /// </summary>
  public static bool ContainsAnonymousType(ITypeSymbol type) => type switch
  {
    { IsAnonymousType: true } => true,
    IArrayTypeSymbol array => ContainsAnonymousType(array.ElementType),
    INamedTypeSymbol named => named.TypeArguments.Any(ContainsAnonymousType)
      || (named.ContainingType is INamedTypeSymbol container && ContainsAnonymousType(container)),
    _ => false
  };
}
