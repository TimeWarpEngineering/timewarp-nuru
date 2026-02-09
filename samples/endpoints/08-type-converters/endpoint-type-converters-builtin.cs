#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - BUILT-IN TYPE CONVERTERS ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates all built-in type converters using Endpoint DSL:
// - int, long, double, decimal, bool, DateTime, Guid, TimeSpan
// - Uri, FileInfo, DirectoryInfo, IPAddress, DateOnly, TimeOnly
//
// DSL: Endpoint with typed parameters
//
// Type converters automatically convert string arguments to the target type.
// They validate input and provide helpful error messages.
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// NUMERIC TYPES
// =============================================================================

[NuruRoute("add", Description = "Add two integers")]
public sealed class AddCommand : IQuery<int>
{
  [Parameter(Description = "First number")]
  public int X { get; set; }

  [Parameter(Description = "Second number")]
  public int Y { get; set; }

  public sealed class Handler : IQueryHandler<AddCommand, int>
  {
    public ValueTask<int> Handle(AddCommand c, CancellationToken ct) =>
      new ValueTask<int>(c.X + c.Y);
  }
}

[NuruRoute("multiply", Description = "Multiply two doubles")]
public sealed class MultiplyCommand : IQuery<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : IQueryHandler<MultiplyCommand, double>
  {
    public ValueTask<double> Handle(MultiplyCommand c, CancellationToken ct) =>
      new ValueTask<double>(c.X * c.Y);
  }
}

[NuruRoute("big-number", Description = "Work with large numbers (long)")]
public sealed class BigNumberCommand : IQuery<long>
{
  [Parameter] public long N { get; set; }

  public sealed class Handler : IQueryHandler<BigNumberCommand, long>
  {
    public ValueTask<long> Handle(BigNumberCommand c, CancellationToken ct) =>
      new ValueTask<long>(c.N * c.N);
  }
}

[NuruRoute("price", Description = "Calculate with decimal precision")]
public sealed class PriceCommand : IQuery<decimal>
{
  [Parameter] public decimal Amount { get; set; }
  [Parameter] public decimal TaxRate { get; set; } = 0.08m;

  public sealed class Handler : IQueryHandler<PriceCommand, decimal>
  {
    public ValueTask<decimal> Handle(PriceCommand c, CancellationToken ct) =>
      new ValueTask<decimal>(c.Amount * (1 + c.TaxRate));
  }
}

// =============================================================================
// BOOLEAN AND TEMPORAL TYPES
// =============================================================================

[NuruRoute("toggle", Description = "Toggle a boolean feature")]
public sealed class ToggleCommand : IQuery<bool>
{
  [Parameter] public bool State { get; set; }

  public sealed class Handler : IQueryHandler<ToggleCommand, bool>
  {
    public ValueTask<bool> Handle(ToggleCommand c, CancellationToken ct) =>
      new ValueTask<bool>(!c.State);
  }
}

[NuruRoute("schedule", Description = "Schedule for a specific date/time")]
public sealed class ScheduleCommand : ICommand<Unit>
{
  [Parameter] public DateTime Date { get; set; }

  public sealed class Handler : ICommandHandler<ScheduleCommand, Unit>
  {
    public ValueTask<Unit> Handle(ScheduleCommand c, CancellationToken ct)
    {
      WriteLine($"Scheduled for: {c.Date:yyyy-MM-dd HH:mm:ss}");
      return default;
    }
  }
}

[NuruRoute("daily", Description = "Daily report for a specific date")]
public sealed class DailyCommand : ICommand<Unit>
{
  [Parameter] public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

  public sealed class Handler : ICommandHandler<DailyCommand, Unit>
  {
    public ValueTask<Unit> Handle(DailyCommand c, CancellationToken ct)
    {
      DateOnly date = DateOnly.Parse(c.Date);
      WriteLine($"Daily report for: {date:yyyy-MM-dd}");
      return default;
    }
  }
}

[NuruRoute("alarm", Description = "Set an alarm time")]
public sealed class AlarmCommand : ICommand<Unit>
{
  [Parameter] public string Time { get; set; } = "09:00";

  public sealed class Handler : ICommandHandler<AlarmCommand, Unit>
  {
    public ValueTask<Unit> Handle(AlarmCommand c, CancellationToken ct)
    {
      TimeOnly time = TimeOnly.Parse(c.Time);
      WriteLine($"Alarm set for: {time:HH:mm}");
      return default;
    }
  }
}

[NuruRoute("timeout", Description = "Set a timeout duration")]
public sealed class TimeoutCommand : ICommand<Unit>
{
  [Parameter] public TimeSpan Duration { get; set; }

  public sealed class Handler : ICommandHandler<TimeoutCommand, Unit>
  {
    public ValueTask<Unit> Handle(TimeoutCommand c, CancellationToken ct)
    {
      WriteLine($"Timeout set to: {c.Duration.TotalSeconds} seconds");
      return default;
    }
  }
}

[NuruRoute("identify", Description = "Work with a GUID")]
public sealed class IdentifyCommand : IQuery<string>
{
  [Parameter] public Guid Id { get; set; }

  public sealed class Handler : IQueryHandler<IdentifyCommand, string>
  {
    public ValueTask<string> Handle(IdentifyCommand c, CancellationToken ct) =>
      new ValueTask<string>($"Processed ID: {c.Id}");
  }
}

// =============================================================================
// FILE SYSTEM TYPES
// =============================================================================

[NuruRoute("read", Description = "Read a file")]
public sealed class ReadCommand : ICommand<Unit>
{
  [Parameter] public string File { get; set; } = ".";

  public sealed class Handler : ICommandHandler<ReadCommand, Unit>
  {
    public ValueTask<Unit> Handle(ReadCommand c, CancellationToken ct)
    {
      FileInfo file = new FileInfo(c.File);
      WriteLine($"Reading: {file.FullName}");
      WriteLine($"  Exists: {file.Exists}");
      WriteLine($"  Size: {file.Length} bytes");
      return default;
    }
  }
}

[NuruRoute("list", Description = "List directory contents")]
public sealed class ListCommand : ICommand<Unit>
{
  [Parameter] public string Dir { get; set; } = ".";

  public sealed class Handler : ICommandHandler<ListCommand, Unit>
  {
    public ValueTask<Unit> Handle(ListCommand c, CancellationToken ct)
    {
      DirectoryInfo dir = new DirectoryInfo(c.Dir);
      WriteLine($"Listing: {dir.FullName}");
      WriteLine($"  Exists: {dir.Exists}");

      if (dir.Exists)
      {
        WriteLine($"  Files: {dir.GetFiles().Length}");
        WriteLine($"  Subdirectories: {dir.GetDirectories().Length}");
      }

      return default;
    }
  }
}

// =============================================================================
// NETWORK TYPES
// =============================================================================

[NuruRoute("fetch", Description = "Fetch from a URI")]
public sealed class FetchCommand : ICommand<Unit>
{
  [Parameter] public Uri Url { get; set; } = new Uri("http://localhost");

  public sealed class Handler : ICommandHandler<FetchCommand, Unit>
  {
    public ValueTask<Unit> Handle(FetchCommand c, CancellationToken ct)
    {
      WriteLine($"Fetching: {c.Url}");
      WriteLine($"  Scheme: {c.Url.Scheme}");
      WriteLine($"  Host: {c.Url.Host}");
      WriteLine($"  Port: {c.Url.Port}");
      WriteLine($"  Path: {c.Url.AbsolutePath}");
      return default;
    }
  }
}

[NuruRoute("ping", Description = "Ping an IP address")]
public sealed class PingCommand : ICommand<Unit>
{
  [Parameter] public string Addr { get; set; } = "127.0.0.1";

  public sealed class Handler : ICommandHandler<PingCommand, Unit>
  {
    public ValueTask<Unit> Handle(PingCommand c, CancellationToken ct)
    {
      System.Net.IPAddress addr = System.Net.IPAddress.Parse(c.Addr);
      WriteLine($"Pinging: {addr}");
      WriteLine($"  AddressFamily: {addr.AddressFamily}");
      return default;
    }
  }
}
