namespace TimeWarp.Terminal;

/// <summary>
/// Provides an ambient context for <see cref="TestTerminal"/> that enables zero-configuration testing
/// of Nuru CLI applications.
/// </summary>
/// <remarks>
/// <para>
/// This class uses <see cref="AsyncLocal{T}"/> to provide a test terminal that flows with the
/// async execution context. This means each test gets its own isolated terminal even when
/// running tests in parallel.
/// </para>
/// <para>
/// Resolution order when determining which terminal to use:
/// <list type="number">
///   <item><description><see cref="Current"/> (if set)</description></item>
///   <item><description><c>ITerminal</c> from DI (if registered)</description></item>
///   <item><description><see cref="TimeWarpTerminal.Default"/> (fallback)</description></item>
/// </list>
/// </para>
/// <example>
/// <code>
/// public static async Task Should_display_greeting()
/// {
///     using TestTerminal terminal = new();
///     TestTerminalContext.Current = terminal;
///     
///     await Program.Main(["greet", "World"]);
///     
///     terminal.OutputContains("Hello, World!").ShouldBeTrue();
/// }
/// </code>
/// </example>
/// </remarks>
public static class TestTerminalContext
{
  private static readonly AsyncLocal<TestTerminal?> Context = new();

  /// <summary>
  /// Gets or sets the current <see cref="TestTerminal"/> for the async execution context.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Setting this property to a non-null value causes all Nuru terminal resolution
  /// to use the provided <see cref="TestTerminal"/> instead of the real terminal.
  /// </para>
  /// <para>
  /// The value is scoped to the current async execution context, so parallel tests
  /// each have their own isolated value.
  /// </para>
  /// </remarks>
  /// <value>
  /// The <see cref="TestTerminal"/> for the current context, or <c>null</c> if not set.
  /// </value>
  public static TestTerminal? Current
  {
    get => Context.Value;
    set => Context.Value = value;
  }

  /// <summary>
  /// Gets a value indicating whether a <see cref="TestTerminal"/> is set for the current context.
  /// </summary>
  public static bool HasValue => Context.Value is not null;

  /// <summary>
  /// Resolves a terminal using the standard resolution order:
  /// TestTerminalContext.Current → provided terminal → fallback.
  /// </summary>
  /// <param name="terminal">The terminal to use if no context is set.</param>
  /// <param name="fallback">The fallback terminal if both context and terminal are null.</param>
  /// <returns>The resolved terminal.</returns>
  public static ITerminal Resolve(ITerminal? terminal, ITerminal fallback)
    => Current ?? terminal ?? fallback;

  /// <summary>
  /// Resolves a terminal using the standard resolution order with TimeWarpTerminal.Default as fallback:
  /// TestTerminalContext.Current → provided terminal → TimeWarpTerminal.Default.
  /// </summary>
  /// <param name="terminal">The terminal to use if no context is set.</param>
  /// <returns>The resolved terminal.</returns>
  public static ITerminal Resolve(ITerminal? terminal)
    => Current ?? terminal ?? TimeWarpTerminal.Default;
}
