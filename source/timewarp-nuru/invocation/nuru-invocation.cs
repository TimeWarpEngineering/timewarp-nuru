namespace TimeWarp.Nuru;

/// <summary>
/// Generated <c>TryBind</c> entry point that <see cref="NuruApp.JsonBinder"/> holds.
/// </summary>
/// <param name="route">Route pattern, as in <see cref="EndpointCapability.Pattern"/>.</param>
/// <param name="arguments">The JSON object to bind.</param>
/// <param name="request">The bound request when binding succeeds.</param>
/// <param name="error">The failure when binding does not succeed.</param>
/// <returns>True when the arguments bind to the route.</returns>
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate bool NuruJsonBinder(string route, JsonElement arguments, out NuruBoundRequest? request, out NuruBindError? error);

/// <summary>
/// One call into generated route execution from <see cref="NuruApp.InvokeAsync"/> or
/// <see cref="NuruApp.RenderAsync"/>. Generated code reads and writes it.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class NuruInvocation
{
  /// <summary>
  /// Creates an invocation for a bound request.
  /// </summary>
  /// <param name="request">The request to run or render.</param>
  public NuruInvocation(NuruBoundRequest request)
  {
    Request = request;
  }

  /// <summary>The request to run or render.</summary>
  public NuruBoundRequest Request { get; }

  /// <summary>
  /// When set, generated code writes <see cref="Result"/> to this terminal instead of running the handler.
  /// </summary>
  public ITerminal? RenderTerminal { get; init; }

  /// <summary>The handler result: captured by a run, or the value to render.</summary>
  public object? Result { get; set; }

  /// <summary>True once the behaviors and handler have run.</summary>
  public bool Dispatched { get; set; }
}
