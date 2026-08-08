namespace TimeWarp.Nuru.Generators;

using System.Collections;
using System.Runtime.CompilerServices;

/// <summary>
/// An immutable array wrapper with value (sequence) equality, so records that contain it
/// participate correctly in incremental-generator caching. Plain <see cref="ImmutableArray{T}"/>
/// uses reference equality, so every pipeline transform produces a fresh array that compares
/// unequal and forces the expensive emit stage to re-run on every keystroke (kanban 454-010 / M5).
/// </summary>
/// <remarks>
/// Designed to be a drop-in for <see cref="ImmutableArray{T}"/> at the ~110 consumer sites:
/// exposes <see cref="Length"/>, an indexer, <see cref="IsDefault"/>/<see cref="IsDefaultOrEmpty"/>,
/// implements <see cref="IReadOnlyList{T}"/> (so all foreach/LINQ keep working with no edits), and
/// carries a <c>[CollectionBuilder]</c> plus implicit conversion from <see cref="ImmutableArray{T}"/>
/// so existing <c>[..]</c> collection expressions and array-building extractor code retarget cleanly.
/// </remarks>
[CollectionBuilder(typeof(EquatableArray), nameof(EquatableArray.Create))]
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
  where T : IEquatable<T>
{
  private readonly ImmutableArray<T> Array;

  public EquatableArray(ImmutableArray<T> array)
  {
    Array = array;
  }

  public static readonly EquatableArray<T> Empty = new([]);

  /// <summary>True if this is the uninitialized default (mirrors <see cref="ImmutableArray{T}.IsDefault"/>).</summary>
  public bool IsDefault => Array.IsDefault;

  /// <summary>True if default or empty (mirrors <see cref="ImmutableArray{T}.IsDefaultOrEmpty"/>).</summary>
  public bool IsDefaultOrEmpty => Array.IsDefaultOrEmpty;

  public int Length => Array.IsDefault ? 0 : Array.Length;

  int IReadOnlyCollection<T>.Count => Length;

  public T this[int index] => Array[index];

  /// <summary>Returns the underlying array (never default — an uninitialized value yields empty).</summary>
  public ImmutableArray<T> ToImmutableArray() => Array.IsDefault ? [] : Array;

  public EquatableArray<T> AddRange(IEnumerable<T> items) => new(ToImmutableArray().AddRange(items));

  public bool Equals(EquatableArray<T> other)
  {
    ReadOnlySpan<T> left = Array.IsDefault ? default : Array.AsSpan();
    ReadOnlySpan<T> right = other.Array.IsDefault ? default : other.Array.AsSpan();
    return left.SequenceEqual(right);
  }

  public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

  public override int GetHashCode()
  {
    if (Array.IsDefaultOrEmpty)
      return 0;

    HashCode hash = new();
    foreach (T item in Array)
    {
      hash.Add(item);
    }

    return hash.ToHashCode();
  }

  /// <summary>Non-boxing enumerator so foreach over an <see cref="EquatableArray{T}"/> allocates nothing.</summary>
  public ImmutableArray<T>.Enumerator GetEnumerator() => ToImmutableArray().GetEnumerator();

  IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)ToImmutableArray()).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)ToImmutableArray()).GetEnumerator();

  // Two-way implicit conversion so EquatableArray is a true drop-in: array-building
  // extractor code assigns straight into EquatableArray fields, and model fields flow into
  // the many emit/validation helpers still typed ImmutableArray<T> with no call-site churn.
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA2225:Operator overloads have named alternates", Justification = "ToImmutableArray() is the named alternate for both directions; the implicit conversions are deliberately frictionless.")]
  public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);

  public static implicit operator ImmutableArray<T>(EquatableArray<T> array) => array.Array.IsDefault ? [] : array.Array;

  public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

  public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}

/// <summary>
/// Collection-builder factory for <see cref="EquatableArray{T}"/> (enables <c>[..]</c> / <c>[x, y]</c>
/// collection expressions targeting the wrapper type).
/// </summary>
public static class EquatableArray
{
  public static EquatableArray<T> Create<T>(ReadOnlySpan<T> items) where T : IEquatable<T> =>
    new(ImmutableArray.Create(items));
}
