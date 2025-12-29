namespace TimeWarp.Nuru;

/// <summary>
/// Handler for queries. Queries are read-only and idempotent.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
  /// <summary>
  /// Handles the query and returns a result.
  /// </summary>
  /// <param name="query">The query to handle.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The query result.</returns>
#pragma warning disable RCS1046 // Async suffix - following MediatR/Mediator convention for Handle method
  ValueTask<TResult> Handle(TQuery query, CancellationToken cancellationToken);
#pragma warning restore RCS1046
}

/// <summary>
/// Handler for commands. Commands mutate state and are NOT safe to retry.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
  /// <summary>
  /// Handles the command and returns a result.
  /// </summary>
  /// <param name="command">The command to handle.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The command result.</returns>
#pragma warning disable RCS1046 // Async suffix - following MediatR/Mediator convention for Handle method
  ValueTask<TResult> Handle(TCommand command, CancellationToken cancellationToken);
#pragma warning restore RCS1046
}

/// <summary>
/// Handler for idempotent commands. Idempotent commands mutate state but are safe to retry.
/// </summary>
/// <typeparam name="TCommand">The idempotent command type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public interface IIdempotentCommandHandler<TCommand, TResult> where TCommand : IIdempotentCommand<TResult>
{
  /// <summary>
  /// Handles the idempotent command and returns a result.
  /// </summary>
  /// <param name="command">The command to handle.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The command result.</returns>
#pragma warning disable RCS1046 // Async suffix - following MediatR/Mediator convention for Handle method
  ValueTask<TResult> Handle(TCommand command, CancellationToken cancellationToken);
#pragma warning restore RCS1046
}
