#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Suppress false positive - NURU_H002 incorrectly detects parameter.Property as 'this' capture
#pragma warning disable NURU_H002

// ============================================================================
// FLUENT DSL - Built-In Type Converters
// ============================================================================
// This sample demonstrates the 15 built-in type converters available in
// TimeWarp.Nuru using Fluent DSL.
//
// DSL: Fluent API with typed parameters ({param:type})
//
// All built-in types support:
//   - Case-insensitive constraint names (fileinfo, FileInfo, FILEINFO all work)
//   - Nullable variants (FileInfo?, Uri?, etc.)
//   - Arrays for catch-all and repeated options
// ============================================================================

using System.Net;
using TimeWarp.Nuru;

NuruAppBuilder builder = NuruApp.CreateBuilder();

// ============================================================================
// Original Built-In Types (v1.0)
// ============================================================================

builder.Map("delay {milliseconds:int}")
  .WithHandler((int milliseconds) => Console.WriteLine($"⏱️  Delaying for {milliseconds}ms"))
  .AsCommand().Done();

builder.Map("price {amount:double} {quantity:int}")
  .WithHandler((double amount, int quantity) => Console.WriteLine($"💰 Total: ${amount * quantity:F2} ({quantity} × ${amount:F2})"))
  .AsQuery().Done();

builder.Map("enabled {feature} {state:bool}")
  .WithHandler((string feature, bool state) => Console.WriteLine($"🎚️  Feature '{feature}' is {(state ? "enabled" : "disabled")}"))
  .AsIdempotentCommand().Done();

builder.Map("schedule {event} {when:DateTime}")
  .WithHandler((string @event, DateTime when) => Console.WriteLine($"📅 Event '{@event}' scheduled for {when:yyyy-MM-dd HH:mm:ss}"))
  .AsCommand().Done();

builder.Map("id {value:Guid}")
  .WithHandler((Guid value) => Console.WriteLine($"🔑 GUID: {value}"))
  .AsQuery().Done();

builder.Map("wait {duration:TimeSpan}")
  .WithHandler((TimeSpan duration) => Console.WriteLine($"⏲️  Waiting for {duration.TotalSeconds:F1} seconds"))
  .AsCommand().Done();

// ============================================================================
// Uri Type
// ============================================================================

builder.Map("fetch {url:Uri}")
  .WithHandler((Uri url) =>
  {
    Console.WriteLine($"🌐 Fetching from {url.AbsoluteUri}");
    Console.WriteLine($"   Scheme: {url.Scheme}");
    Console.WriteLine($"   Host: {url.Host}");
    Console.WriteLine($"   Path: {url.AbsolutePath}");
  })
  .AsQuery().Done();

builder.Map("open-url {url:uri}")
  .WithHandler((Uri url) => Console.WriteLine($"🔗 Opening {url} in browser"))
  .AsCommand().Done();

// ============================================================================
// FileInfo Type
// ============================================================================

builder.Map("read {path:FileInfo}")
  .WithHandler((FileInfo file) =>
  {
    Console.WriteLine($"📄 File: {file.Name}");
    Console.WriteLine($"   Full path: {file.FullName}");
    Console.WriteLine($"   Directory: {file.DirectoryName}");
    Console.WriteLine($"   Extension: {file.Extension}");
    Console.WriteLine($"   Exists: {file.Exists}");
    if (file.Exists)
    {
      Console.WriteLine($"   Size: {file.Length:N0} bytes");
      Console.WriteLine($"   Last modified: {file.LastWriteTime}");
    }
  })
  .AsQuery().Done();

builder.Map("edit {file:fileinfo} --backup {backup:FileInfo?}")
  .WithHandler((FileInfo file, FileInfo? backup) =>
  {
    Console.WriteLine($"✏️  Editing {file.FullName}");
    if (backup != null)
      Console.WriteLine($"   Backup to {backup.FullName}");
  })
  .AsCommand().Done();

// ============================================================================
// DirectoryInfo Type
// ============================================================================

builder.Map("list {path:DirectoryInfo}")
  .WithHandler((DirectoryInfo dir) =>
  {
    Console.WriteLine($"📁 Directory: {dir.Name}");
    Console.WriteLine($"   Full path: {dir.FullName}");
    Console.WriteLine($"   Parent: {dir.Parent?.FullName ?? "(root)"}");
    Console.WriteLine($"   Exists: {dir.Exists}");
    if (dir.Exists)
    {
      FileInfo[] files = dir.GetFiles();
      DirectoryInfo[] subdirs = dir.GetDirectories();
      Console.WriteLine($"   Contains: {files.Length} files, {subdirs.Length} directories");
    }
  })
  .AsQuery().Done();

builder.Map("sync {source:DirectoryInfo} {dest:DIRECTORYINFO}")
  .WithHandler((DirectoryInfo source, DirectoryInfo dest) => Console.WriteLine($"🔄 Syncing {source.FullName} → {dest.FullName}"))
  .AsCommand().Done();

// ============================================================================
// IPAddress Type
// ============================================================================

builder.Map("ping {address:ipaddress}")
  .WithHandler((IPAddress address) =>
  {
    Console.WriteLine($"📡 Pinging {address}");
    Console.WriteLine($"   Address family: {address.AddressFamily}");
    Console.WriteLine($"   Is IPv4: {address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork}");
    Console.WriteLine($"   Is IPv6: {address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6}");
    if (IPAddress.IsLoopback(address))
      Console.WriteLine($"   This is a loopback address");
  })
  .AsQuery().Done();

builder.Map("connect {host:ipaddress} {port:int}")
  .WithHandler((IPAddress host, int port) => Console.WriteLine($"🔌 Connecting to {host}:{port}"))
  .AsCommand().Done();

// ============================================================================
// DateOnly Type
// ============================================================================

builder.Map("report {date:DateOnly}")
  .WithHandler((DateOnly date) =>
  {
    Console.WriteLine($"📊 Generating report for {date:yyyy-MM-dd}");
    Console.WriteLine($"   Day of week: {date.DayOfWeek}");
    Console.WriteLine($"   Day of year: {date.DayOfYear}");
  })
  .AsQuery().Done();

builder.Map("range {start:dateonly} {end:DateOnly}")
  .WithHandler((DateOnly start, DateOnly end) =>
  {
    int days = end.DayNumber - start.DayNumber;
    Console.WriteLine($"📆 Date range: {start} to {end} ({days} days)");
  })
  .AsQuery().Done();

// ============================================================================
// TimeOnly Type
// ============================================================================

builder.Map("alarm {time:TimeOnly}")
  .WithHandler((TimeOnly time) =>
  {
    Console.WriteLine($"⏰ Alarm set for {time:HH:mm:ss}");
    Console.WriteLine($"   Hour: {time.Hour}");
    Console.WriteLine($"   Minute: {time.Minute}");
    Console.WriteLine($"   Second: {time.Second}");
  })
  .AsCommand().Done();

builder.Map("schedule-backup {time:timeonly}")
  .WithHandler((TimeOnly time) => Console.WriteLine($"💾 Backup scheduled daily at {time:HH:mm}"))
  .AsCommand().Done();

// ============================================================================
// Combined Examples
// ============================================================================

builder.Map("deploy {version:Guid} {target:Uri} {date:DateOnly} --dry-run?")
  .WithHandler((Guid version, Uri target, DateOnly date, bool dryRun) =>
  {
    Console.WriteLine($"🚀 Deployment Plan:");
    Console.WriteLine($"   Version: {version}");
    Console.WriteLine($"   Target: {target}");
    Console.WriteLine($"   Date: {date}");
    Console.WriteLine($"   Mode: {(dryRun ? "DRY RUN" : "LIVE")}");
  })
  .AsCommand().Done();

builder.Map("backup {source:DirectoryInfo} --dest {dest:DirectoryInfo?} --config {cfg:FileInfo?}")
  .WithHandler((DirectoryInfo source, DirectoryInfo? dest, FileInfo? cfg) =>
  {
    Console.WriteLine($"💾 Backup Configuration:");
    Console.WriteLine($"   Source: {source.FullName}");
    Console.WriteLine($"   Destination: {dest?.FullName ?? "(default)"}");
    Console.WriteLine($"   Config: {cfg?.FullName ?? "(none)"}");
  })
  .AsCommand().Done();

NuruApp app = builder.Build();

// Show usage examples
if (args.Length == 0)
{
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine("   TimeWarp.Nuru - Built-In Type Converters Demo (Fluent DSL)");
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine();
  Console.WriteLine("Try these examples:");
  Console.WriteLine();
  Console.WriteLine("Original types:");
  Console.WriteLine("  delay 1000");
  Console.WriteLine("  price 19.99 3");
  Console.WriteLine("  schedule Meeting 2024-12-25T14:30:00");
  Console.WriteLine();
  Console.WriteLine("Uri type:");
  Console.WriteLine("  fetch https://example.com/api/data");
  Console.WriteLine();
  Console.WriteLine("FileInfo type:");
  Console.WriteLine("  read /etc/passwd");
  Console.WriteLine("  edit myfile.txt --backup myfile.bak");
  Console.WriteLine();
  Console.WriteLine("DirectoryInfo type:");
  Console.WriteLine("  list /tmp");
  Console.WriteLine("  sync /source /destination");
  Console.WriteLine();
  Console.WriteLine("IPAddress type:");
  Console.WriteLine("  ping 192.168.1.1");
  Console.WriteLine("  connect 10.0.0.1 8080");
  Console.WriteLine();
  Console.WriteLine("DateOnly type:");
  Console.WriteLine("  report 2024-12-25");
  Console.WriteLine("  range 2024-01-01 2024-12-31");
  Console.WriteLine();
  Console.WriteLine("TimeOnly type:");
  Console.WriteLine("  alarm 07:30:00");
  Console.WriteLine("  schedule-backup 02:00");
  Console.WriteLine();
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
}

return await app.RunAsync(args);
