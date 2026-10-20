#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj

// Forwarded call latency through an in-process TimeWarp.Nuru.Daemon server (task 508-016):
// one call at a time versus 8 at once, for a no-op route and a route that waits 50 ms.

using TimeWarp.Nuru.Daemon;

const int Rounds = 10;
const int Parallel = 8;

NuruApp app = NuruApp.CreateBuilder()
  .WithName("bench-daemon")
  .Map("bench-noop").WithHandler(() => 1).AsQuery().Done()
  .Map("bench-wait {ms:int}").WithHandler(async (int ms, CancellationToken cancellationToken) =>
  {
    await Task.Delay(ms, cancellationToken);
    return ms;
  }).AsQuery().Done()
  .Build();

NuruDaemonOptions options = new()
{
  SocketPath = Path.Combine(Directory.CreateTempSubdirectory("nuru-bench-").FullName, "bench.sock"),
  IdleTimeout = TimeSpan.Zero
};

using CancellationTokenSource stop = new();
Task<int> server = Task.Run(() => NuruDaemonServer.RunAsync(app, options, stop.Token));
NuruDaemonClient client = new(options);
for (int i = 0; i < 100 && await client.PingAsync(app) is null; i++)
  await Task.Delay(50);

string[][] routes = [["bench-noop"], ["bench-wait", "50"]];
foreach (string[] route in routes)
{
  for (int i = 0; i < 5; i++)
    await client.RunAsync(app, route); // Warm up.

  List<double> single = [];
  for (int i = 0; i < Rounds * Parallel; i++)
    single.Add(await TimeAsync(route));

  List<double> parallel = [];
  List<double> batches = [];
  for (int round = 0; round < Rounds; round++)
  {
    Stopwatch batch = Stopwatch.StartNew();
    double[] calls = await Task.WhenAll(Enumerable.Range(0, Parallel).Select(_ => TimeAsync(route)));
    batches.Add(batch.Elapsed.TotalMilliseconds);
    parallel.AddRange(calls);
  }

  string name = string.Join(" ", route);
  await TimeWarp.Terminal.Terminal.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
    $"{name,-14} single mean {single.Average(),7:F2} ms | {Parallel} parallel: per-call mean {parallel.Average(),7:F2} ms, max {parallel.Max(),7:F2} ms, batch mean {batches.Average(),7:F2} ms"));
}

await stop.CancelAsync();
await server;
return 0;

async Task<double> TimeAsync(string[] callArgs)
{
  Stopwatch stopwatch = Stopwatch.StartNew();
  NuruForwardResult result = await client.RunAsync(app, callArgs);
  if (result.ExitCode != 0)
    throw new InvalidOperationException(result.ErrorOutput);
  return stopwatch.Elapsed.TotalMilliseconds;
}
