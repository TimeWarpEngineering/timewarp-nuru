namespace TimeWarp.Nuru;

/// <summary>
/// Marker interface for all CLI messages (queries, commands).
/// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces - marker interface for message categorization
public interface IMessage;
#pragma warning restore CA1040

/// <summary>
/// A query that returns a result. Queries are read-only and idempotent (safe to retry).
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
#pragma warning disable CA1040 // Avoid empty interfaces - marker interface inheriting from IIdempotent
public interface IQuery<TResult> : IIdempotent;
#pragma warning restore CA1040

/// <summary>
/// A command that mutates state. Commands may have side effects and are NOT safe to retry.
/// </summary>
/// <typeparam name="TResult">The result type (use Unit for void).</typeparam>
#pragma warning disable CA1040 // Avoid empty interfaces - marker interface for non-idempotent commands
public interface ICommand<TResult> : IMessage;
#pragma warning restore CA1040

/// <summary>
/// An idempotent command - mutates state but safe to retry (e.g., PUT/DELETE operations).
/// </summary>
/// <typeparam name="TResult">The result type (use Unit for void).</typeparam>
#pragma warning disable CA1040 // Avoid empty interfaces - marker interface for idempotent commands
public interface IIdempotentCommand<TResult> : IIdempotent;
#pragma warning restore CA1040

/// <summary>
/// Represents no result (void equivalent for async handlers).
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types - Unit is a singleton marker type
public readonly struct Unit : IEquatable<Unit>
#pragma warning restore CA1815
{
  /// <summary>
  /// The singleton unit value.
  /// </summary>
  public static Unit Value => default;

  /// <summary>
  /// Determines whether the specified Unit is equal to this instance.
  /// </summary>
  public bool Equals(Unit other) => true;

  /// <summary>
  /// Determines whether the specified object is equal to this instance.
  /// </summary>
  public override bool Equals(object? obj) => obj is Unit;

  /// <summary>
  /// Returns a hash code for this instance.
  /// </summary>
  public override int GetHashCode() => 0;

  /// <summary>
  /// Equality operator.
  /// </summary>
  public static bool operator ==(Unit _, Unit __) => true;

  /// <summary>
  /// Inequality operator.
  /// </summary>
  public static bool operator !=(Unit _, Unit __) => false;
}
