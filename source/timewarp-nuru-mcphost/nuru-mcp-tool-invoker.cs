#region Purpose
// Run one MCP tools/call against a Nuru app: bind with the generated binder, invoke, render the CLI text.
#endregion

#region Design
// Bind failures and handler failures are tool results with isError, not JSON-RPC errors, so an agent sees
// the same message --json-args prints and can correct its arguments. Calls run concurrently: each runs in
// its own NuruCallContext (TimeWarp.Nuru core) with its own TestTerminal, exit code and service scope, and
// with CaptureConsoleOutput its Console.Out/Error writes are routed to that terminal per call; nothing on
// the call path swaps the console or touches Environment.ExitCode. Routes marked [Exclusive] (and every call
// with NuruMcpHostOptions.ExclusiveCalls) take the app's exclusive gate. The MCP SDK cancels the handler's
// token on notifications/cancelled, so only that call stops. RenderAsync supplies the CLI's text and exit
// code; it continues the invoke's context, so the exit code a handler set (NuruCallContext.CurrentExitCode)
// survives and a renderer resolves paths against the call's directory. A non-zero exit code (problem details
// or the handler's) is a tool error.
// A served app (AddServeRoute(), TimeWarp.Nuru.Daemon) attaches NuruApp.RouteForwarder; a call to a route
// that is not local-only goes to that forwarder first, so the MCP host and the CLI share one warm server.
// The server binds the same JSON object with the same public binder and returns the same texts.
// When the app has an IResponseRenderer<SharedProblemDetails>, a bind failure (NuruProblems.FromBindError, Type
// Invalid) and an escaped exception (NuruProblems.FromException, Type Faulted) are rendered with it, so a model
// sees one error format; the server does the same for a forwarded call (task 508-018).
#endregion

namespace TimeWarp.Nuru.McpHost;

/// <summary>
/// Runs <c>tools/call</c> requests against a Nuru app through <see cref="NuruApp.TryBind"/>,
/// <see cref="NuruApp.InvokeAsync"/>, and <see cref="NuruApp.RenderAsync"/>.
/// </summary>
public sealed class NuruMcpToolInvoker
{
  private readonly NuruApp App;
  private readonly NuruMcpHostOptions Options;

  /// <summary>
  /// Creates an invoker for <paramref name="app"/>.
  /// </summary>
  /// <param name="app">The app whose routes the tools call.</param>
  /// <param name="options">Host options; null uses the defaults.</param>
  public NuruMcpToolInvoker(NuruApp app, NuruMcpHostOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(app);
    App = app;
    Options = options?.Clone() ?? new NuruMcpHostOptions();
  }

  /// <summary>
  /// Binds <paramref name="arguments"/> to <paramref name="endpoint"/>, runs it, and returns the result.
  /// </summary>
  /// <param name="endpoint">The catalog endpoint the tool maps to.</param>
  /// <param name="arguments">The <c>tools/call</c> arguments; null binds none.</param>
  /// <param name="cancellationToken">Cancels the call.</param>
  /// <returns>
  /// The CLI text as content. A bind error, a handler exception, or a non-zero CLI exit code
  /// returns <see cref="CallToolResult.IsError"/> true with the message as content.
  /// </returns>
  public async Task<CallToolResult> CallAsync(
    EndpointCapability endpoint,
    IDictionary<string, JsonElement>? arguments,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(endpoint);

    using JsonDocument argumentsDocument = ToJsonObject(arguments);

    if (endpoint.LocalOnly != true && App.RouteForwarder is INuruRouteForwarder forwarder)
    {
      NuruForwardResult? forwarded = await forwarder
        .ForwardAsync(App, NuruForwardRequest.ForInvoke(endpoint.Pattern, argumentsDocument.RootElement), cancellationToken)
        .ConfigureAwait(false);
      if (forwarded is not null)
        return ForwardedResult(endpoint, forwarded);
    }

    if (!App.TryBind(endpoint.Pattern, argumentsDocument.RootElement, out NuruBoundRequest? request, out NuruBindError? error))
    {
      return ErrorResult(error is null
        ? $"Error: could not bind arguments to '{endpoint.Pattern}'."
        : RenderProblemOr(NuruProblems.FromBindError(error), error.Message));
    }

    using TestTerminal terminal = CreateCallTerminal();
    NuruCallContext context = new(terminal) { CaptureConsole = Options.CaptureConsoleOutput };
    NuruExclusiveLease exclusive = Options.ExclusiveCalls
      ? await NuruExclusiveLease.EnterAsync(App, context, cancellationToken).ConfigureAwait(false)
      : NuruExclusiveLease.None;
    await using (exclusive.ConfigureAwait(false))
    {
      return await InvokeAsync(endpoint, request, context, terminal, cancellationToken).ConfigureAwait(false);
    }
  }

  /// <summary>A plain-text terminal for one call: no color, not interactive.</summary>
  private static TestTerminal CreateCallTerminal() => new() { SupportsColor = false, IsInteractive = false };

  private async Task<CallToolResult> InvokeAsync(
    EndpointCapability endpoint,
    NuruBoundRequest request,
    NuruCallContext context,
    TestTerminal terminal,
    CancellationToken cancellationToken)
  {
    string stdout;
    string stderr;
    string rendered;
    int exitCode;
    object? result;

    try
    {
      result = await App.InvokeAsync(request, context, cancellationToken).ConfigureAwait(false);

      using TestTerminal renderTerminal = CreateCallTerminal();
      exitCode = await App.RenderAsync(request, result, renderTerminal, context).ConfigureAwait(false);

      rendered = renderTerminal.Output;
      stdout = terminal.Output + rendered;
      stderr = terminal.ErrorOutput + renderTerminal.ErrorOutput;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      throw;
    }
#pragma warning disable CA1031 // A handler failure is reported to the agent as a tool error, like the CLI's stderr.
    catch (Exception exception)
#pragma warning restore CA1031
    {
      string partial = JoinNonEmpty(terminal.Output, terminal.ErrorOutput);
      return ErrorResult(JoinNonEmpty(partial, RenderProblemOr(NuruProblems.FromException(exception), $"Error: {exception.Message}")));
    }

    if (exitCode != 0)
    {
      string message = JoinNonEmpty(stderr, stdout);
      return ErrorResult(message.Length > 0 ? message : $"Error: '{endpoint.Pattern}' exited with code {exitCode.ToString(CultureInfo.InvariantCulture)}.");
    }

    CallToolResult success = new() { Content = [], IsError = false };

    string text = TrimTrailingNewLine(stdout);
    if (text.Length > 0)
      success.Content.Add(new TextContentBlock { Text = text });

    string diagnostics = TrimTrailingNewLine(stderr);
    if (diagnostics.Length > 0)
      success.Content.Add(new TextContentBlock { Text = diagnostics });

    if (Options.ContentFormat == NuruMcpContentFormat.Json)
      success.StructuredContent = CreateStructuredContent(endpoint, result, TrimTrailingNewLine(rendered));

    return success;
  }

  /// <summary>
  /// A tool result from a call a served app's server ran: the same texts and exit code rules as a
  /// local call. Structured content comes from the rendered text, since the CLR value stayed in the server.
  /// </summary>
  private CallToolResult ForwardedResult(EndpointCapability endpoint, NuruForwardResult forwarded)
  {
    string stdout = forwarded.Output + forwarded.RenderedOutput;
    if (forwarded.ExitCode != 0)
    {
      string message = JoinNonEmpty(forwarded.ErrorOutput, stdout);
      return ErrorResult(message.Length > 0 ? message : $"Error: '{endpoint.Pattern}' exited with code {forwarded.ExitCode.ToString(CultureInfo.InvariantCulture)}.");
    }

    CallToolResult success = new() { Content = [], IsError = false };

    string text = TrimTrailingNewLine(stdout);
    if (text.Length > 0)
      success.Content.Add(new TextContentBlock { Text = text });

    string diagnostics = TrimTrailingNewLine(forwarded.ErrorOutput);
    if (diagnostics.Length > 0)
      success.Content.Add(new TextContentBlock { Text = diagnostics });

    if (Options.ContentFormat == NuruMcpContentFormat.Json)
    {
      string rendered = TrimTrailingNewLine(forwarded.RenderedOutput);
      success.StructuredContent = CreateStructuredContent(endpoint, ParseRenderedScalar(endpoint, rendered), rendered);
    }

    return success;
  }

  /// <summary>
  /// The scalar a forwarded call rendered, typed by the catalog's return type name: numbers and
  /// booleans parse back to their value, everything else stays the rendered text.
  /// </summary>
  private static object? ParseRenderedScalar(EndpointCapability endpoint, string text)
  {
    if (text.Length == 0)
      return null;

    string typeName = (endpoint.ReturnType?.Name ?? "").TrimEnd('?');
    if (typeName.StartsWith("System.", StringComparison.Ordinal))
      typeName = typeName["System.".Length..];
    return typeName switch
    {
      "bool" or "Boolean" when bool.TryParse(text, out bool value) => value,
      "byte" or "sbyte" or "short" or "ushort" or "int" or "uint" or "long"
        or "Byte" or "SByte" or "Int16" or "UInt16" or "Int32" or "UInt32" or "Int64"
        when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) => value,
      "ulong" or "UInt64" when ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong value) => value,
      "float" or "double" or "Single" or "Double"
        when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) => value,
      "decimal" or "Decimal" when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) => value,
      "string" or "String" => text,
      _ => null
    };
  }

  /// <summary>
  /// The JSON for <see cref="NuruMcpContentFormat.Json"/>: a JSON object result as is, any other value
  /// as <c>{"result": value}</c>, and nothing for a route that returns no value.
  /// </summary>
  /// <remarks>
  /// Built from <paramref name="result"/> and the text RenderAsync wrote, never from captured console
  /// output: a handler's log lines stay in the text content and do not corrupt the structured value.
  /// </remarks>
  private static JsonElement? CreateStructuredContent(EndpointCapability endpoint, object? result, string text)
  {
    ReturnTypeKind? kind = endpoint.ReturnType?.Kind;
    if (kind == ReturnTypeKind.Unit || (result is null && text.Length == 0))
      return null;

    if (kind == ReturnTypeKind.Json)
    {
      try
      {
        using JsonDocument rendered = JsonDocument.Parse(text);
        return rendered.RootElement.ValueKind == JsonValueKind.Object
          ? rendered.RootElement.Clone()
          : Wrap(writer => rendered.RootElement.WriteTo(writer));
      }
      catch (JsonException)
      {
        // A response renderer wrote text that is not JSON; the text content still carries it.
        return null;
      }
    }

    return Wrap(writer => WriteScalar(writer, result, text));
  }

  private static JsonElement Wrap(Action<Utf8JsonWriter> writeValue)
  {
    ArrayBufferWriter<byte> buffer = new();
    using (Utf8JsonWriter writer = new(buffer))
    {
      writer.WriteStartObject();
      writer.WritePropertyName("result");
      writeValue(writer);
      writer.WriteEndObject();
    }

    using JsonDocument document = JsonDocument.Parse(buffer.WrittenMemory);
    return document.RootElement.Clone();
  }

  private static void WriteScalar(Utf8JsonWriter writer, object? result, string text)
  {
    switch (result)
    {
      case bool value: writer.WriteBooleanValue(value); break;
      case byte value: writer.WriteNumberValue(value); break;
      case sbyte value: writer.WriteNumberValue(value); break;
      case short value: writer.WriteNumberValue(value); break;
      case ushort value: writer.WriteNumberValue(value); break;
      case int value: writer.WriteNumberValue(value); break;
      case uint value: writer.WriteNumberValue(value); break;
      case long value: writer.WriteNumberValue(value); break;
      case ulong value: writer.WriteNumberValue(value); break;
      case float value when float.IsFinite(value): writer.WriteNumberValue(value); break;
      case double value when double.IsFinite(value): writer.WriteNumberValue(value); break;
      case decimal value: writer.WriteNumberValue(value); break;
      case string value: writer.WriteStringValue(value); break;
      // Other scalars (char, Guid, dates) use the CLI's rendering of the value.
      default: writer.WriteStringValue(text); break;
    }
  }

  private static JsonDocument ToJsonObject(IDictionary<string, JsonElement>? arguments)
  {
    ArrayBufferWriter<byte> buffer = new();
    using (Utf8JsonWriter writer = new(buffer))
    {
      writer.WriteStartObject();
      if (arguments is not null)
      {
        foreach (KeyValuePair<string, JsonElement> argument in arguments)
        {
          writer.WritePropertyName(argument.Key);
          if (argument.Value.ValueKind == JsonValueKind.Undefined)
            writer.WriteNullValue();
          else
            argument.Value.WriteTo(writer);
        }
      }

      writer.WriteEndObject();
    }

    return JsonDocument.Parse(buffer.WrittenMemory);
  }

  /// <summary>
  /// <paramref name="problem"/> rendered with the app's problem renderer, or <paramref name="fallback"/> when the
  /// app has none or the renderer throws: the plain text the host wrote before task 508-018.
  /// </summary>
  private string RenderProblemOr(SharedProblemDetails problem, string fallback)
  {
    try
    {
      return App.TryRenderProblem(problem, out string? text) ? text : fallback;
    }
#pragma warning disable CA1031 // A failing renderer must not turn a tool error into a protocol error.
    catch (Exception)
#pragma warning restore CA1031
    {
      return fallback;
    }
  }

  private static CallToolResult ErrorResult(string message) => new()
  {
    Content = [new TextContentBlock { Text = TrimTrailingNewLine(message) }],
    IsError = true
  };

  private static string JoinNonEmpty(string first, string second)
  {
    first = TrimTrailingNewLine(first);
    second = TrimTrailingNewLine(second);
    if (first.Length == 0)
      return second;
    if (second.Length == 0)
      return first;
    return first + "\n" + second;
  }

  private static string TrimTrailingNewLine(string text) => text.TrimEnd('\r', '\n');
}
