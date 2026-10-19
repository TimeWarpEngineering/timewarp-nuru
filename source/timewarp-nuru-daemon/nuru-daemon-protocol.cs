#region Purpose
// Wire format between a served app's CLI (or MCP host) and its server.
#endregion

#region Design
// One connection carries one request line and one response line, both UTF-8 JSON objects ended by
// '\n'. Written and read with Utf8JsonWriter / JsonDocument, so there is no reflection (AOT). The client
// keeps its side open until the response arrives; the server treats end of input before that as the
// client going away (Ctrl+C) and cancels the call.
//
// Requests: {"v":1,"op":"ping"} | {"v":1,"op":"stop"}
//   | {"v":1,"op":"run","args":[...],"cwd":"...","build":"..."}                      argv, matched by generated code
//   | {"v":1,"op":"invoke","route":"...","arguments":{...},"cwd":"...","build":"..."} JSON object for the public binder
// "build" is optional: when present the server refuses the call unless it is the server's own build id,
// so a call never runs on a server from another build even if one took the socket after the ping.
// Responses: {"ok":true,"exitCode":n,"stdout":"...","stderr":"...","rendered":"..."}
//   | {"ok":true,"name":"...","version":"...","build":"...","pid":n,"socket":"..."} (ping)
//   | {"ok":false,"error":"..."}
#endregion

namespace TimeWarp.Nuru.Daemon;

/// <summary>
/// What a running server reports about itself.
/// </summary>
/// <param name="Name">The app name from the catalog.</param>
/// <param name="Version">The app version from the catalog.</param>
/// <param name="BuildId">The server's <see cref="NuruDaemonPaths.GetBuildId"/>.</param>
/// <param name="ProcessId">The server's process id.</param>
/// <param name="SocketPath">The socket the server listens on.</param>
public sealed record NuruDaemonStatus(string Name, string Version, string BuildId, int ProcessId, string SocketPath);

/// <summary>Protocol operations.</summary>
internal static class NuruDaemonOperations
{
  public const string Ping = "ping";
  public const string Stop = "stop";
  public const string Run = "run";
  public const string Invoke = "invoke";
}

/// <summary>A decoded request line.</summary>
internal sealed record NuruDaemonRequest(string Operation, string? Route, string[]? Args, string? Arguments, string? WorkingDirectory, string? BuildId);

/// <summary>Encodes and decodes protocol lines.</summary>
internal static class NuruDaemonProtocol
{
  public const int Version = 1;

  public static byte[] EncodeSimpleRequest(string operation) => Encode(writer =>
  {
    writer.WriteNumber("v", Version);
    writer.WriteString("op", operation);
  });

  public static byte[] EncodeRunRequest(IReadOnlyList<string> args, string workingDirectory, string? buildId = null) => Encode(writer =>
  {
    writer.WriteNumber("v", Version);
    writer.WriteString("op", NuruDaemonOperations.Run);
    writer.WriteStartArray("args");
    foreach (string arg in args)
      writer.WriteStringValue(arg);
    writer.WriteEndArray();
    writer.WriteString("cwd", workingDirectory);
    if (buildId is not null)
      writer.WriteString("build", buildId);
  });

  public static byte[] EncodeInvokeRequest(string route, string argumentsJson, string workingDirectory, string? buildId = null) => Encode(writer =>
  {
    writer.WriteNumber("v", Version);
    writer.WriteString("op", NuruDaemonOperations.Invoke);
    writer.WriteString("route", route);
    writer.WritePropertyName("arguments");
    using (JsonDocument arguments = JsonDocument.Parse(argumentsJson))
      arguments.RootElement.WriteTo(writer);
    writer.WriteString("cwd", workingDirectory);
    if (buildId is not null)
      writer.WriteString("build", buildId);
  });

  public static byte[] EncodeResult(NuruForwardResult result) => Encode(writer =>
  {
    writer.WriteBoolean("ok", true);
    writer.WriteNumber("exitCode", result.ExitCode);
    writer.WriteString("stdout", result.Output);
    writer.WriteString("stderr", result.ErrorOutput);
    writer.WriteString("rendered", result.RenderedOutput);
  });

  public static byte[] EncodeStatus(NuruDaemonStatus status) => Encode(writer =>
  {
    writer.WriteBoolean("ok", true);
    writer.WriteString("name", status.Name);
    writer.WriteString("version", status.Version);
    writer.WriteString("build", status.BuildId);
    writer.WriteNumber("pid", status.ProcessId);
    writer.WriteString("socket", status.SocketPath);
  });

  public static byte[] EncodeOk() => Encode(writer => writer.WriteBoolean("ok", true));

  public static byte[] EncodeError(string message) => Encode(writer =>
  {
    writer.WriteBoolean("ok", false);
    writer.WriteString("error", message);
  });

  /// <summary>Decodes a request line, or returns an error message.</summary>
  public static NuruDaemonRequest? DecodeRequest(string line, out string? error)
  {
    error = null;
    try
    {
      using JsonDocument document = JsonDocument.Parse(line);
      JsonElement root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object)
      {
        error = "The request is not a JSON object.";
        return null;
      }

      if (!root.TryGetProperty("v", out JsonElement version) || !version.TryGetInt32(out int v) || v != Version)
      {
        error = $"Unsupported protocol version; this server speaks version {Version.ToString(CultureInfo.InvariantCulture)}.";
        return null;
      }

      string? operation = GetString(root, "op");
      if (operation is null)
      {
        error = "The request has no 'op'.";
        return null;
      }

      string[]? args = null;
      if (root.TryGetProperty("args", out JsonElement argsElement) && argsElement.ValueKind == JsonValueKind.Array)
        args = [.. argsElement.EnumerateArray().Select(static a => a.GetString() ?? "")];

      string? arguments = root.TryGetProperty("arguments", out JsonElement argumentsElement)
        ? argumentsElement.GetRawText()
        : null;

      return new NuruDaemonRequest(operation, GetString(root, "route"), args, arguments, GetString(root, "cwd"), GetString(root, "build"));
    }
    catch (JsonException exception)
    {
      error = "The request is not valid JSON: " + exception.Message;
      return null;
    }
  }

  /// <summary>Decodes a run or invoke response.</summary>
  public static NuruForwardResult DecodeResult(string line)
  {
    using JsonDocument document = JsonDocument.Parse(line);
    JsonElement root = document.RootElement;
    ThrowIfError(root);
    return new NuruForwardResult(
      root.GetProperty("exitCode").GetInt32(),
      GetString(root, "stdout") ?? "",
      GetString(root, "stderr") ?? "",
      GetString(root, "rendered") ?? "");
  }

  /// <summary>Decodes a ping response.</summary>
  public static NuruDaemonStatus DecodeStatus(string line)
  {
    using JsonDocument document = JsonDocument.Parse(line);
    JsonElement root = document.RootElement;
    ThrowIfError(root);
    return new NuruDaemonStatus(
      GetString(root, "name") ?? "",
      GetString(root, "version") ?? "",
      GetString(root, "build") ?? "",
      root.TryGetProperty("pid", out JsonElement pid) && pid.TryGetInt32(out int processId) ? processId : 0,
      GetString(root, "socket") ?? "");
  }

  /// <summary>Decodes a response that carries only ok or an error.</summary>
  public static void DecodeOk(string line)
  {
    using JsonDocument document = JsonDocument.Parse(line);
    ThrowIfError(document.RootElement);
  }

  private static void ThrowIfError(JsonElement root)
  {
    if (root.TryGetProperty("ok", out JsonElement ok) && ok.ValueKind == JsonValueKind.True)
      return;

    throw new NuruDaemonException(GetString(root, "error") ?? "The server returned an error.");
  }

  private static string? GetString(JsonElement root, string name) =>
    root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
      ? value.GetString()
      : null;

  private static byte[] Encode(Action<Utf8JsonWriter> writeMembers)
  {
    ArrayBufferWriter<byte> buffer = new();
    using (Utf8JsonWriter writer = new(buffer))
    {
      writer.WriteStartObject();
      writeMembers(writer);
      writer.WriteEndObject();
    }

    buffer.Write("\n"u8);
    return buffer.WrittenSpan.ToArray();
  }
}

/// <summary>
/// A server answered with an error, or the connection ended before a response.
/// </summary>
public sealed class NuruDaemonException : Exception
{
  /// <summary>Creates the exception.</summary>
  public NuruDaemonException()
  {
  }

  /// <summary>Creates the exception with a message.</summary>
  /// <param name="message">What went wrong.</param>
  public NuruDaemonException(string message) : base(message)
  {
  }

  /// <summary>Creates the exception with a message and a cause.</summary>
  /// <param name="message">What went wrong.</param>
  /// <param name="innerException">The cause.</param>
  public NuruDaemonException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
