#!/usr/bin/env -S dotnet --
#region Purpose
// Proves dev exports one command span, exports nothing without an endpoint,
// and that each workflow stage helper span is a child of the workflow span.
#endregion

#region Design
// The short command is the real dev CLI: workflow --mode not-a-mode throws
// inside the handler, so TelemetryBehavior exports a single workflow span.
// Stage children use WorkflowStageSpan, the same helper as dev workflow,
// from a small in-process app. A full pipeline is not required.
// The sink is a loopback OTLP/HTTP listener. TRACEPARENT is a fixed header.
#endregion

if (args.Length > 0 && string.Equals(args[0], "stages", StringComparison.Ordinal))
{
  return await RunStagesAppAsync();
}

if (args.Length > 0 && string.Equals(args[0], "aot", StringComparison.Ordinal))
{
  return await RunAotHarnessAsync();
}

return await RunHarnessAsync();

static async Task<int> RunHarnessAsync()
{
  string repoRoot = Directory.GetCurrentDirectory();
  string devPath = Path.Combine(repoRoot, "tools", "dev-cli", "dev.cs");
  string proofPath = Path.Combine(repoRoot, "tools", "dev-cli", "telemetry-proof", "proof.cs");
  if (!File.Exists(devPath) || !File.Exists(proofPath))
  {
    Terminal.WriteErrorLine("Run this proof from the repository root.");
    return 1;
  }

  const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
  const string parentSpanId = "00f067aa0ba902b7";
  string traceparent = $"00-{traceId}-{parentSpanId}-01";

  using ProofSink sink = new();
  sink.TakeTraces();
  ProofRun command = await RunCommandAsync(
    repoRoot,
    "dotnet",
    [devPath, "workflow", "--mode", "not-a-mode"],
    new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["OTEL_EXPORTER_OTLP_ENDPOINT"] = sink.Prefix,
      ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
      ["TRACEPARENT"] = traceparent,
    },
    TimeSpan.FromMinutes(10));

  if (command.ExitCode == 0)
  {
    Terminal.WriteErrorLine("dev workflow --mode not-a-mode exited 0.");
    Terminal.WriteErrorLine(Tail(command.Stderr));
    return 1;
  }

  List<byte[]> commandBodies = sink.TakeTraces();
  if (commandBodies.Count == 0)
  {
    Terminal.WriteErrorLine("endpoint set: no /v1/traces post");
    Terminal.WriteErrorLine(Tail(command.Stdout));
    Terminal.WriteErrorLine(Tail(command.Stderr));
    return 1;
  }

  List<ProofSpan> commandSpans = DecodeAll(commandBodies);
  ProofSpan workflow = ExpectSingle(commandSpans, "workflow");
  Expect(string.Equals(workflow.TraceId, traceId, StringComparison.Ordinal), $"trace id {workflow.TraceId}");
  Expect(string.Equals(workflow.ParentSpanId, parentSpanId, StringComparison.Ordinal), $"parent span id {workflow.ParentSpanId}");
  Expect(string.Equals(workflow.ServiceName, "timewarp-nuru-dev", StringComparison.Ordinal), $"service {workflow.ServiceName}");
  Expect(workflow.StatusCode == 2, $"command status {workflow.StatusCode}");
  Expect(string.Equals(Attribute(workflow, "error.type"), "ArgumentException", StringComparison.Ordinal), "error.type");
  Expect(string.IsNullOrEmpty(workflow.StatusMessage), "command status message");
  RejectSecrets(workflow);
  Terminal.WriteLine("command span: one workflow span, parented to TRACEPARENT, error.type ArgumentException");

  ProofRun silent = await RunCommandAsync(
    repoRoot,
    "dotnet",
    [devPath, "workflow", "--mode", "not-a-mode"],
    new Dictionary<string, string>(StringComparer.Ordinal),
    TimeSpan.FromMinutes(3));
  if (silent.ExitCode == 0)
  {
    Terminal.WriteErrorLine("unset-endpoint run exited 0.");
    return 1;
  }

  List<byte[]> silentBodies = sink.TakeTraces();
  if (silentBodies.Count != 0)
  {
    Terminal.WriteErrorLine($"unset endpoint exported {silentBodies.Count} trace post(s).");
    return 1;
  }

  Terminal.WriteLine("unset endpoint: no trace export");

  ProofRun stages = await RunCommandAsync(
    repoRoot,
    "dotnet",
    [proofPath, "stages"],
    new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["OTEL_EXPORTER_OTLP_ENDPOINT"] = sink.Prefix,
      ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
    },
    TimeSpan.FromMinutes(10));
  if (stages.ExitCode != 0)
  {
    Terminal.WriteErrorLine("stage app failed");
    Terminal.WriteErrorLine(Tail(stages.Stdout));
    Terminal.WriteErrorLine(Tail(stages.Stderr));
    return 1;
  }

  List<byte[]> stageBodies = sink.TakeTraces();
  if (stageBodies.Count == 0)
  {
    Terminal.WriteErrorLine("stage app exported no traces");
    Terminal.WriteErrorLine(Tail(stages.Stdout));
    Terminal.WriteErrorLine(Tail(stages.Stderr));
    return 1;
  }

  List<ProofSpan> stageSpans = DecodeAll(stageBodies);
  ProofSpan pipeline = ExpectSingle(stageSpans, "workflow");
  Expect(string.Equals(pipeline.ServiceName, "timewarp-nuru-dev", StringComparison.Ordinal), $"stage service {pipeline.ServiceName}");
  Expect(string.Equals(Attribute(pipeline, "ProcessorCount"), Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), "ProcessorCount");
  Expect(pipeline.StatusCode == 1, $"pipeline status {pipeline.StatusCode}");

  string[] prStages = ["attestation", "clean", "build", "verify-samples", "test"];
  string[] releaseStages = ["tag-gate", "check-version", "locate-run", "download-artifact", "verify", "push"];
  Expect(stageSpans.Count == 1 + prStages.Length + releaseStages.Length, $"span count {stageSpans.Count}");
  ExpectChildren(stageSpans, pipeline, "pr", prStages, failingStage: null);
  ExpectChildren(stageSpans, pipeline, "release", releaseStages, failingStage: "push");
  Terminal.WriteLine("stage spans: workflow parent plus one child per pr and release stage");
  Terminal.WriteLine("proof ok");
  return 0;
}

static Task<int> RunStagesAppAsync()
{
  NuruApp app = NuruApp.CreateBuilder()
    .UseTelemetry(options =>
    {
      options.ServiceName = "timewarp-nuru-dev";
    })
    .Map("workflow")
      .WithHandler(static () =>
      {
        DevCli.WorkflowStageSpan.TagPipeline(Activity.Current);
        StageProof.Emit("pr", ["attestation", "clean", "build", "verify-samples", "test"], failingStage: null);
        StageProof.Emit("release", ["tag-gate", "check-version", "locate-run", "download-artifact", "verify", "push"], failingStage: "push");
      })
      .AsCommand()
      .Done()
    .Build();

  return app.RunAsync(["workflow"]);
}

static void ExpectChildren(List<ProofSpan> spans, ProofSpan pipeline, string mode, string[] stages, string? failingStage)
{
  foreach (string stage in stages)
  {
    ProofSpan span = spans.Single(candidate =>
      string.Equals(candidate.Name, stage, StringComparison.Ordinal)
      && string.Equals(Attribute(candidate, "ci.mode"), mode, StringComparison.Ordinal));
    Expect(string.Equals(span.TraceId, pipeline.TraceId, StringComparison.Ordinal), $"{stage} trace");
    Expect(string.Equals(span.ParentSpanId, pipeline.SpanId, StringComparison.Ordinal), $"{stage} parent");
    Expect(string.Equals(Attribute(span, "ci.stage"), stage, StringComparison.Ordinal), $"{stage} ci.stage");
    RejectSecrets(span);
    if (string.Equals(stage, failingStage, StringComparison.Ordinal))
    {
      Expect(span.StatusCode == 2, $"{stage} status");
      Expect(string.Equals(Attribute(span, "error.type"), "PipelineAbort", StringComparison.Ordinal), $"{stage} error.type");
      Expect(string.IsNullOrEmpty(span.StatusMessage), $"{stage} status message");
    }
    else
    {
      Expect(span.StatusCode == 1, $"{stage} status {span.StatusCode}");
      Expect(!span.Attributes.ContainsKey("error.type"), $"{stage} error.type");
    }
  }
}

static ProofSpan ExpectSingle(List<ProofSpan> spans, string name)
{
  List<ProofSpan> matches = [.. spans.Where(span => string.Equals(span.Name, name, StringComparison.Ordinal))];
  Expect(matches.Count == 1, $"expected one {name} span, found {matches.Count} among [{string.Join(", ", spans.Select(span => span.Name))}]");
  return matches[0];
}

static string? Attribute(ProofSpan span, string key)
{
  span.Attributes.TryGetValue(key, out string? value);
  return value;
}

static void RejectSecrets(ProofSpan span)
{
  foreach (KeyValuePair<string, string> attribute in span.Attributes)
  {
    Expect(!string.Equals(attribute.Key, "error.message", StringComparison.Ordinal), "error.message");
    Expect(!attribute.Key.Contains("api", StringComparison.OrdinalIgnoreCase), attribute.Key);
    Expect(!attribute.Key.Contains("token", StringComparison.OrdinalIgnoreCase), attribute.Key);
    Expect(!attribute.Key.Contains("argv", StringComparison.OrdinalIgnoreCase), attribute.Key);
    Expect(!attribute.Value.Contains("not-a-mode", StringComparison.Ordinal), attribute.Key);
    Expect(!attribute.Value.Contains("4bf92f3577b34da6a3ce929d0e0e4736", StringComparison.Ordinal), attribute.Key);
  }
}

static void Expect(bool condition, string message)
{
  if (!condition)
  {
    throw new InvalidOperationException(message);
  }
}

static List<ProofSpan> DecodeAll(IReadOnlyList<byte[]> bodies)
{
  List<ProofSpan> spans = [];
  foreach (byte[] body in bodies)
  {
    spans.AddRange(OtlpTraces.Read(body));
  }

  return spans;
}

static async Task<int> RunAotHarnessAsync()
{
  string repoRoot = Directory.GetCurrentDirectory();
  string devBinary = Path.Combine(repoRoot, "bin", "dev");
  if (!File.Exists(devBinary))
  {
    Terminal.WriteErrorLine($"missing {devBinary}");
    return 1;
  }

  const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
  const string parentSpanId = "00f067aa0ba902b7";
  string traceparent = $"00-{traceId}-{parentSpanId}-01";
  using ProofSink sink = new();
  sink.TakeTraces();
  ProofRun command = await RunCommandAsync(
    repoRoot,
    devBinary,
    ["workflow", "--mode", "not-a-mode"],
    new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["OTEL_EXPORTER_OTLP_ENDPOINT"] = sink.Prefix,
      ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
      ["TRACEPARENT"] = traceparent,
    },
    TimeSpan.FromMinutes(2));

  if (command.ExitCode == 0)
  {
    Terminal.WriteErrorLine("bin/dev exited 0.");
    Terminal.WriteErrorLine(Tail(command.Stderr));
    return 1;
  }

  List<ProofSpan> commandSpans = DecodeAll(sink.TakeTraces());
  ProofSpan workflow = ExpectSingle(commandSpans, "workflow");
  Expect(string.Equals(workflow.TraceId, traceId, StringComparison.Ordinal), $"aot trace id {workflow.TraceId}");
  Expect(string.Equals(workflow.ParentSpanId, parentSpanId, StringComparison.Ordinal), $"aot parent span id {workflow.ParentSpanId}");
  Expect(string.Equals(workflow.ServiceName, "timewarp-nuru-dev", StringComparison.Ordinal), $"aot service {workflow.ServiceName}");
  Expect(string.Equals(Attribute(workflow, "error.type"), "ArgumentException", StringComparison.Ordinal), "aot error.type");
  RejectSecrets(workflow);

  ProofRun silent = await RunCommandAsync(
    repoRoot,
    devBinary,
    ["workflow", "--mode", "not-a-mode"],
    new Dictionary<string, string>(StringComparer.Ordinal),
    TimeSpan.FromMinutes(1));
  if (silent.ExitCode == 0 || sink.TakeTraces().Count != 0)
  {
    Terminal.WriteErrorLine("aot unset endpoint exported or exited 0.");
    return 1;
  }

  Terminal.WriteLine("aot bin/dev: one workflow span, parented to TRACEPARENT; unset endpoint exports nothing");
  return 0;
}

static async Task<ProofRun> RunCommandAsync(
  string repoRoot,
  string executable,
  IReadOnlyList<string> arguments,
  Dictionary<string, string> environment,
  TimeSpan timeout)
{
  ShellBuilder command = Shell.Builder(executable)
    .WithArguments([.. arguments])
    .WithWorkingDirectory(repoRoot)
    .WithNoValidation()
    .WithTimeout(timeout);

  foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
  {
    if (entry.Key is not string key)
    {
      continue;
    }

    if (key.StartsWith("OTEL_", StringComparison.Ordinal) || key is "TRACEPARENT" or "TRACESTATE")
    {
      command = command.WithEnvironmentVariable(key, null);
    }
  }

  foreach (KeyValuePair<string, string> pair in environment)
  {
    command = command.WithEnvironmentVariable(pair.Key, pair.Value);
  }

  CommandOutput output = await command.CaptureAsync();
  if (output.TimedOut)
  {
    throw new InvalidOperationException($"timed out: {executable} {string.Join(' ', arguments)}");
  }

  return new ProofRun(output.ExitCode, output.Stdout, output.Stderr, []);
}

static string Tail(string text)
{
  if (text.Length <= 4000)
  {
    return text;
  }

  return text[^4000..];
}

sealed record ProofRun(int ExitCode, string Stdout, string Stderr, IReadOnlyList<byte[]> Traces);

static class StageProof
{
  internal static void Emit(string mode, string[] stages, string? failingStage)
  {
    foreach (string stage in stages)
    {
      using DevCli.WorkflowStageSpan.Scope scope = DevCli.WorkflowStageSpan.Begin(mode, stage, stage);
      if (string.Equals(stage, failingStage, StringComparison.Ordinal))
      {
        scope.Fail(DevCli.WorkflowStageSpan.PipelineAbort);
        continue;
      }

      scope.Succeed();
    }
  }
}

sealed class ProofSpan
{
  internal required string Name { get; init; }
  internal required string TraceId { get; init; }
  internal required string SpanId { get; init; }
  internal required string ParentSpanId { get; init; }
  internal required string ServiceName { get; init; }
  internal required int StatusCode { get; init; }
  internal required string StatusMessage { get; init; }
  internal required Dictionary<string, string> Attributes { get; init; }
}

sealed class ProofSink : IDisposable
{
  private readonly HttpListener Listener = new();
  private readonly List<byte[]> Bodies = [];
  private readonly Task Loop;

  internal ProofSink()
  {
    using TcpListener probe = new(IPAddress.Loopback, 0);
    probe.Start();
    int port = ((IPEndPoint)probe.LocalEndpoint).Port;
    probe.Stop();
    Prefix = $"http://127.0.0.1:{port}/";
    Listener.Prefixes.Add(Prefix);
    Listener.Start();
    Loop = Task.Run(ServeAsync);
  }

  internal string Prefix { get; }

  internal int TracePosts
  {
    get
    {
      lock (Bodies)
      {
        return Bodies.Count;
      }
    }
  }

  public void Dispose()
  {
    Listener.Close();
    try
    {
      Loop.Wait(TimeSpan.FromSeconds(2));
    }
    catch (AggregateException)
    {
    }
  }

  private async Task ServeAsync()
  {
    while (Listener.IsListening)
    {
      HttpListenerContext context;
      try
      {
        context = await Listener.GetContextAsync();
      }
      catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException)
      {
        return;
      }

      byte[] body = await ReadBodyAsync(context.Request);
      if (string.Equals(context.Request.Url?.AbsolutePath, "/v1/traces", StringComparison.Ordinal))
      {
        lock (Bodies)
        {
          Bodies.Add(body);
        }
      }

      context.Response.StatusCode = 200;
      context.Response.Close();
    }
  }

  internal List<byte[]> TakeTraces()
  {
    lock (Bodies)
    {
      List<byte[]> copy = [.. Bodies];
      Bodies.Clear();
      return copy;
    }
  }

  private static async Task<byte[]> ReadBodyAsync(HttpListenerRequest request)
  {
    using MemoryStream raw = new();
    await request.InputStream.CopyToAsync(raw);
    byte[] body = raw.ToArray();
    if (!string.Equals(request.Headers["Content-Encoding"], "gzip", StringComparison.OrdinalIgnoreCase))
    {
      return body;
    }

    using MemoryStream source = new(body);
    using System.IO.Compression.GZipStream gzip = new(source, System.IO.Compression.CompressionMode.Decompress);
    using MemoryStream decoded = new();
    gzip.CopyTo(decoded);
    return decoded.ToArray();
  }
}

static class OtlpTraces
{
  internal static List<ProofSpan> Read(byte[] payload)
  {
    List<ProofSpan> spans = [];
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        ReadResourceSpans(reader.ReadBytes(), spans);
      }
      else
      {
        reader.Skip(wire);
      }
    }

    return spans;
  }

  private static void ReadResourceSpans(byte[] payload, List<ProofSpan> spans)
  {
    string serviceName = "";
    ProtoReader reader = new(payload);
    List<byte[]> scopes = [];
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        serviceName = ReadServiceName(reader.ReadBytes());
      }
      else if (field == 2 && wire == 2)
      {
        scopes.Add(reader.ReadBytes());
      }
      else
      {
        reader.Skip(wire);
      }
    }

    foreach (byte[] scope in scopes)
    {
      ReadScopeSpans(scope, serviceName, spans);
    }
  }

  private static string ReadServiceName(byte[] payload)
  {
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        Dictionary<string, string> attributes = ReadKeyValue(reader.ReadBytes());
        if (string.Equals(attributes.GetValueOrDefault("key"), "service.name", StringComparison.Ordinal))
        {
          return attributes.GetValueOrDefault("value") ?? "";
        }
      }
      else
      {
        reader.Skip(wire);
      }
    }

    return "";
  }

  private static void ReadScopeSpans(byte[] payload, string serviceName, List<ProofSpan> spans)
  {
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 2 && wire == 2)
      {
        spans.Add(ReadSpan(reader.ReadBytes(), serviceName));
      }
      else
      {
        reader.Skip(wire);
      }
    }
  }

  private static ProofSpan ReadSpan(byte[] payload, string serviceName)
  {
    string name = "";
    string traceId = "";
    string spanId = "";
    string parentSpanId = "";
    Dictionary<string, string> attributes = [];
    int statusCode = 0;
    string statusMessage = "";
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      switch (field)
      {
        case 1 when wire == 2:
          traceId = Convert.ToHexString(reader.ReadBytes()).ToLowerInvariant();
          break;
        case 2 when wire == 2:
          spanId = Convert.ToHexString(reader.ReadBytes()).ToLowerInvariant();
          break;
        case 4 when wire == 2:
          parentSpanId = Convert.ToHexString(reader.ReadBytes()).ToLowerInvariant();
          break;
        case 5 when wire == 2:
          name = Encoding.UTF8.GetString(reader.ReadBytes());
          break;
        case 9 when wire == 2:
          Dictionary<string, string> attribute = ReadKeyValue(reader.ReadBytes());
          if (attribute.TryGetValue("key", out string? key) && attribute.TryGetValue("value", out string? value))
          {
            attributes[key] = value;
          }

          break;
        case 15 when wire == 2:
          (statusCode, statusMessage) = ReadStatus(reader.ReadBytes());
          break;
        default:
          reader.Skip(wire);
          break;
      }
    }

    return new ProofSpan
    {
      Name = name,
      TraceId = traceId,
      SpanId = spanId,
      ParentSpanId = parentSpanId,
      ServiceName = serviceName,
      StatusCode = statusCode,
      StatusMessage = statusMessage,
      Attributes = attributes,
    };
  }

  private static (int Code, string Message) ReadStatus(byte[] payload)
  {
    int code = 0;
    string message = "";
    bool sawCode = false;
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 2 && wire == 2)
      {
        message = Encoding.UTF8.GetString(reader.ReadBytes());
      }
      else if (field == 3 && wire == 0)
      {
        code = (int)reader.ReadVarint();
        sawCode = true;
      }
      else if (field == 1 && wire == 0 && !sawCode)
      {
        code = (int)reader.ReadVarint();
      }
      else
      {
        reader.Skip(wire);
      }
    }

    return (code, message);
  }

  private static Dictionary<string, string> ReadKeyValue(byte[] payload)
  {
    Dictionary<string, string> result = [];
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        result["key"] = Encoding.UTF8.GetString(reader.ReadBytes());
      }
      else if (field == 2 && wire == 2)
      {
        result["value"] = ReadAny(reader.ReadBytes());
      }
      else
      {
        reader.Skip(wire);
      }
    }

    return result;
  }

  private static string ReadAny(byte[] payload)
  {
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        return Encoding.UTF8.GetString(reader.ReadBytes());
      }

      if (field == 3 && wire == 0)
      {
        return reader.ReadVarint().ToString(CultureInfo.InvariantCulture);
      }

      if (field == 2 && wire == 0)
      {
        return reader.ReadVarint() == 0 ? "false" : "true";
      }

      reader.Skip(wire);
    }

    return "";
  }
}

sealed class ProtoReader
{
  private readonly byte[] Buffer;
  private int Offset;

  internal ProtoReader(byte[] buffer)
  {
    Buffer = buffer;
  }

  internal int Remaining => Buffer.Length - Offset;

  internal (int Field, int Wire) ReadTag()
  {
    int tag = (int)ReadVarint();
    return (tag >> 3, tag & 7);
  }

  internal ulong ReadVarint()
  {
    ulong result = 0;
    int shift = 0;
    while (true)
    {
      byte next = Buffer[Offset++];
      result |= (ulong)(next & 0x7F) << shift;
      if ((next & 0x80) == 0)
      {
        return result;
      }

      shift += 7;
    }
  }

  internal byte[] ReadBytes()
  {
    int length = (int)ReadVarint();
    byte[] slice = Buffer[Offset..(Offset + length)];
    Offset += length;
    return slice;
  }

  internal void Skip(int wire)
  {
    switch (wire)
    {
      case 0:
        ReadVarint();
        break;
      case 1:
        Offset += 8;
        break;
      case 2:
        ReadBytes();
        break;
      case 5:
        Offset += 4;
        break;
      default:
        throw new InvalidOperationException($"Unsupported protobuf wire type {wire}.");
    }
  }
}
