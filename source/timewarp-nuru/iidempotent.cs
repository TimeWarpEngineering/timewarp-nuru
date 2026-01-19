namespace TimeWarp.Nuru;

/// <summary>
/// Marker interface indicating that a command is idempotent - can be safely retried without side effects.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface alongside <c>ICommand&lt;T&gt;</c> to indicate that the command
/// can be executed multiple times with the same result (e.g., set, enable, disable, upsert).
/// </para>
/// <para>
/// The source generator uses this interface to automatically set <see cref="MessageType.IdempotentCommand"/>
/// for attributed routes:
/// </para>
/// <code>
/// // Derived as IdempotentCommand because it implements both ICommand and IIdempotent
/// [NuruRoute("config set {key} {value}")]
/// public sealed class SetConfigRequest : ICommand&lt;Response&gt;, IIdempotent { }
/// </code>
/// <para>
/// For delegate-based routes, use the fluent API instead:
/// </para>
/// <code>
/// app.Map("config set {key} {value}", handler).AsIdempotentCommand();
/// </code>
/// </remarks>
/// <seealso cref="MessageType"/>
#pragma warning disable CA1040 // Avoid empty interfaces - marker interface for idempotency indication
public interface IIdempotent : IMessage;
#pragma warning restore CA1040
