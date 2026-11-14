#region Purpose
// Generator model for whether a handler result can be null, read from the semantic model so the
// invoker emitter does not have to guess from a type name.
#endregion

#region Design
// Kanban 583: the emitter knew only BCL value types by name, so a user enum or struct result got an
// `is not null` guard (CS0037) and a `?.ToString()` fallback (CS0023). The extractors classify the result
// type once from its ITypeSymbol and carry the kind as a plain enum, which keeps the model equatable for
// the incremental pipeline. Nullable<T> is a value type at the CLR level but can be null, so it is MaybeNull.
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
  Enum
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
    if (type?.IsValueType != true)
      return ResultValueKind.MaybeNull;

    if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
      return ResultValueKind.MaybeNull;

    return type.TypeKind == TypeKind.Enum ? ResultValueKind.Enum : ResultValueKind.ValueType;
  }
}
