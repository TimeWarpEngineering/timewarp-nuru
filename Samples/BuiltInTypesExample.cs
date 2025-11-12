#!/usr/bin/dotnet --
#:project TimeWarp.Nuru.Sample/TimeWarp.Nuru.Sample.csproj

// ============================================================================
// Built-In Type Converters Example
// ============================================================================
// This sample demonstrates the 15 built-in type converters available in
// TimeWarp.Nuru, with special focus on the 6 types added in v2.1.0:
//   - Uri (web URLs, relative paths)
//   - FileInfo (file paths with metadata)
//   - DirectoryInfo (directory paths with metadata)
//   - IPAddress (IPv4 and IPv6 addresses)
//   - DateOnly (dates without time component)
//   - TimeOnly (time without date component)
//
// All built-in types support:
//   - Case-insensitive constraint names (fileinfo, FileInfo, FILEINFO all work)
//   - Nullable variants (FileInfo?, Uri?, etc.)
//   - Arrays for catch-all and repeated options
// ============================================================================

using System.Net;
using TimeWarp.Nuru;

NuruAppBuilder builder = new();

// ============================================================================
// Original Built-In Types (v1.0)
// ============================================================================

builder.AddRoute("delay {milliseconds:int}", (int milliseconds) =>
{
  Console.WriteLine($"⏱️  Delaying for {milliseconds}ms");
  return 0;
});

builder.AddRoute("price {amount:double} {quantity:int}", (double amount, int quantity) =>
{
  Console.WriteLine($"💰 Total: ${amount * quantity:F2} ({quantity} × ${amount:F2})");
  return 0;
});

builder.AddRoute("enabled {feature} {state:bool}", (string feature, bool state) =>
{
  Console.WriteLine($"🎚️  Feature '{feature}' is {(state ? "enabled" : "disabled")}");
  return 0;
});

builder.AddRoute("schedule {event} {when:DateTime}", (string @event, DateTime when) =>
{
  Console.WriteLine($"📅 Event '{@event}' scheduled for {when:yyyy-MM-dd HH:mm:ss}");
  return 0;
});

builder.AddRoute("id {value:Guid}", (Guid value) =>
{
  Console.WriteLine($"🔑 GUID: {value}");
  return 0;
});

builder.AddRoute("wait {duration:TimeSpan}", (TimeSpan duration) =>
{
  Console.WriteLine($"⏲️  Waiting for {duration.TotalSeconds:F1} seconds");
  return 0;
});

// ============================================================================
// Uri Type
// ============================================================================
// Supports absolute URLs, relative URLs, and file:// URIs

builder.AddRoute("fetch {url:Uri}", (Uri url) =>
{
  Console.WriteLine($"🌐 Fetching from {url.AbsoluteUri}");
  Console.WriteLine($"   Scheme: {url.Scheme}");
  Console.WriteLine($"   Host: {url.Host}");
  Console.WriteLine($"   Path: {url.AbsolutePath}");
  return 0;
});

builder.AddRoute("open-url {url:uri}", (Uri url) =>  // Case-insensitive!
{
  Console.WriteLine($"🔗 Opening {url} in browser");
  return 0;
});

// ============================================================================
// FileInfo Type
// ============================================================================
// Provides file metadata without requiring the file to exist

builder.AddRoute("read {path:FileInfo}", (FileInfo file) =>
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
  return 0;
});

builder.AddRoute("edit {file:fileinfo} --backup {backup:FileInfo?}", (FileInfo file, FileInfo? backup) =>
{
  Console.WriteLine($"✏️  Editing {file.FullName}");
  if (backup != null)
    Console.WriteLine($"   Backup to {backup.FullName}");
  return 0;
});

// ============================================================================
// DirectoryInfo Type
// ============================================================================
// Provides directory metadata without requiring the directory to exist

builder.AddRoute("list {path:DirectoryInfo}", (DirectoryInfo dir) =>
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
  return 0;
});

builder.AddRoute("sync {source:DirectoryInfo} {dest:DIRECTORYINFO}", (DirectoryInfo source, DirectoryInfo dest) =>
{
  Console.WriteLine($"🔄 Syncing {source.FullName} → {dest.FullName}");
  return 0;
});

// ============================================================================
// IPAddress Type
// ============================================================================
// Supports both IPv4 and IPv6 addresses

builder.AddRoute("ping {address:ipaddress}", (IPAddress address) =>
{
  Console.WriteLine($"📡 Pinging {address}");
  Console.WriteLine($"   Address family: {address.AddressFamily}");
  Console.WriteLine($"   Is IPv4: {address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork}");
  Console.WriteLine($"   Is IPv6: {address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6}");
  if (IPAddress.IsLoopback(address))
    Console.WriteLine($"   This is a loopback address");
  return 0;
});

builder.AddRoute("connect {host:ipaddress} {port:int}", (IPAddress host, int port) =>
{
  Console.WriteLine($"🔌 Connecting to {host}:{port}");
  return 0;
});

// ============================================================================
// DateOnly Type
// ============================================================================
// For dates without time component (.NET 6+)

builder.AddRoute("report {date:DateOnly}", (DateOnly date) =>
{
  Console.WriteLine($"📊 Generating report for {date:yyyy-MM-dd}");
  Console.WriteLine($"   Day of week: {date.DayOfWeek}");
  Console.WriteLine($"   Day of year: {date.DayOfYear}");
  return 0;
});

builder.AddRoute("range {start:dateonly} {end:DateOnly}", (DateOnly start, DateOnly end) =>
{
  int days = end.DayNumber - start.DayNumber;
  Console.WriteLine($"📆 Date range: {start} to {end} ({days} days)");
  return 0;
});

// ============================================================================
// TimeOnly Type
// ============================================================================
// For time without date component (.NET 6+)

builder.AddRoute("alarm {time:TimeOnly}", (TimeOnly time) =>
{
  Console.WriteLine($"⏰ Alarm set for {time:HH:mm:ss}");
  Console.WriteLine($"   Hour: {time.Hour}");
  Console.WriteLine($"   Minute: {time.Minute}");
  Console.WriteLine($"   Second: {time.Second}");
  return 0;
});

builder.AddRoute("schedule-backup {time:timeonly}", (TimeOnly time) =>
{
  Console.WriteLine($"💾 Backup scheduled daily at {time:HH:mm}");
  return 0;
});

// ============================================================================
// Combined Examples
// ============================================================================

builder.AddRoute("deploy {version:Guid} {target:Uri} {date:DateOnly} --dry-run?",
  (Guid version, Uri target, DateOnly date, bool dryRun) =>
{
  Console.WriteLine($"🚀 Deployment Plan:");
  Console.WriteLine($"   Version: {version}");
  Console.WriteLine($"   Target: {target}");
  Console.WriteLine($"   Date: {date}");
  Console.WriteLine($"   Mode: {(dryRun ? "DRY RUN" : "LIVE")}");
  return 0;
});

builder.AddRoute("backup {source:DirectoryInfo} --dest {dest:DirectoryInfo?} --config {cfg:FileInfo?}",
  (DirectoryInfo source, DirectoryInfo? dest, FileInfo? cfg) =>
{
  Console.WriteLine($"💾 Backup Configuration:");
  Console.WriteLine($"   Source: {source.FullName}");
  Console.WriteLine($"   Destination: {dest?.FullName ?? "(default)"}");
  Console.WriteLine($"   Config: {cfg?.FullName ?? "(none)"}");
  return 0;
});

NuruApp app = builder.Build();

// Show usage examples
if (args.Length == 0)
{
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine("   TimeWarp.Nuru - Built-In Type Converters Demo");
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  Console.WriteLine();
  Console.WriteLine("Try these examples:");
  Console.WriteLine();
  Console.WriteLine("Original types:");
  Console.WriteLine("  ./BuiltInTypesExample.cs delay 1000");
  Console.WriteLine("  ./BuiltInTypesExample.cs price 19.99 3");
  Console.WriteLine("  ./BuiltInTypesExample.cs schedule Meeting 2024-12-25T14:30:00");
  Console.WriteLine();
  Console.WriteLine("Uri type:");
  Console.WriteLine("  ./BuiltInTypesExample.cs fetch https://example.com/api/data");
  Console.WriteLine("  ./BuiltInTypesExample.cs open-url file:///home/user/document.pdf");
  Console.WriteLine();
  Console.WriteLine("FileInfo type:");
  Console.WriteLine("  ./BuiltInTypesExample.cs read /etc/passwd");
  Console.WriteLine("  ./BuiltInTypesExample.cs edit myfile.txt --backup myfile.bak");
  Console.WriteLine();
  Console.WriteLine("DirectoryInfo type:");
  Console.WriteLine("  ./BuiltInTypesExample.cs list /tmp");
  Console.WriteLine("  ./BuiltInTypesExample.cs sync /source /destination");
  Console.WriteLine();
  Console.WriteLine("IPAddress type:");
  Console.WriteLine("  ./BuiltInTypesExample.cs ping 192.168.1.1");
  Console.WriteLine("  ./BuiltInTypesExample.cs ping ::1");
  Console.WriteLine("  ./BuiltInTypesExample.cs connect 10.0.0.1 8080");
  Console.WriteLine();
  Console.WriteLine("DateOnly type:");
  Console.WriteLine("  ./BuiltInTypesExample.cs report 2024-12-25");
  Console.WriteLine("  ./BuiltInTypesExample.cs range 2024-01-01 2024-12-31");
  Console.WriteLine();
  Console.WriteLine("TimeOnly type:");
  Console.WriteLine("  ./BuiltInTypesExample.cs alarm 07:30:00");
  Console.WriteLine("  ./BuiltInTypesExample.cs schedule-backup 02:00");
  Console.WriteLine();
  Console.WriteLine("Combined examples:");
  Console.WriteLine("  ./BuiltInTypesExample.cs deploy $(uuidgen) https://prod.example.com 2024-12-25");
  Console.WriteLine("  ./BuiltInTypesExample.cs backup /data --dest /backup --config config.json");
  Console.WriteLine();
  Console.WriteLine("═══════════════════════════════════════════════════════════════");
  return 0;
}

return await app.RunAsync(args);
