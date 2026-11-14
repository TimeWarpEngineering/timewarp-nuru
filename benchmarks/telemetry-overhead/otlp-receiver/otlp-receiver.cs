#!/usr/bin/env -S dotnet --
#:sdk Microsoft.NET.Sdk.Web

#region Purpose
// Minimal local OTLP receiver for the telemetry overhead benchmark (task 520), case 3.
// It accepts every export and throws the payload away, so the benchmark measures the
// client's cost against a fast, always-successful collector.
#endregion

#region Design
// Usage: dotnet run otlp-receiver.cs -- [grpcPort] [httpPort]   (defaults 24317, 24318)
// - gRPC (OTLP default protocol): h2c on grpcPort. POST /<service>/Export answers with an
//   empty gRPC message (5-byte frame: not compressed, length 0) and trailer grpc-status: 0.
//   An empty Export*ServiceResponse is valid protobuf, so no generated proto types are needed.
// - http/protobuf: HTTP/1.1 and h2c on httpPort. POST /v1/{traces|metrics|logs} answers
//   200 with an empty application/x-protobuf body.
// - GET /stats on httpPort returns the export counts, so run-benchmark.sh can wait for
//   startup and confirm exports arrived.
// Ports bind to localhost only. Logging is off so the console stays quiet.
#endregion

using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.AspNetCore.Server.Kestrel.Core;

int grpcPort = args.Length > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 24317;
int httpPort = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 24318;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(kestrel =>
{
  kestrel.ListenLocalhost(grpcPort, listen => listen.Protocols = HttpProtocols.Http2);
  kestrel.ListenLocalhost(httpPort, listen => listen.Protocols = HttpProtocols.Http1AndHttp2);
});

WebApplication app = builder.Build();
ConcurrentDictionary<string, long> counts = new(StringComparer.Ordinal);
byte[] emptyGrpcMessage = new byte[5];

app.MapPost("/{service}/Export", async (HttpContext context, string service) =>
{
  await context.Request.Body.CopyToAsync(Stream.Null).ConfigureAwait(false);
  counts.AddOrUpdate("grpc " + service, 1, (_, count) => count + 1);
  context.Response.ContentType = "application/grpc";
  await context.Response.Body.WriteAsync(emptyGrpcMessage).ConfigureAwait(false);
  context.Response.AppendTrailer("grpc-status", "0");
});

app.MapPost("/v1/{signal}", async (HttpContext context, string signal) =>
{
  await context.Request.Body.CopyToAsync(Stream.Null).ConfigureAwait(false);
  counts.AddOrUpdate("http " + signal, 1, (_, count) => count + 1);
  context.Response.ContentType = "application/x-protobuf";
});

app.MapGet("/stats", () =>
  string.Join('\n', counts.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key} {pair.Value}")) + "\n");

Console.WriteLine($"otlp-receiver listening: grpc=http://127.0.0.1:{grpcPort} http=http://127.0.0.1:{httpPort}");
await app.RunAsync().ConfigureAwait(false);
