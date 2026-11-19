#!/usr/bin/env -S dotnet --

#region Purpose
// Pins [NuruRoute] and [Operation] binding of FileInfo, DirectoryInfo, and IPAddress,
// including nullable [Operation] properties of those types and of Uri.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{
using TimeWarp.Contracts;

/// <summary>
/// Endpoint properties of <see cref="FileInfo"/>, <see cref="DirectoryInfo"/>, and <see cref="IPAddress"/>
/// convert through the same constraint names a fluent route uses. A nullable [Operation] property of
/// those types, and of <see cref="Uri"/>, binds from argv and stays unset when omitted.
/// </summary>
[TestTag("Routing")]
public class EndpointFileDirectoryIpTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EndpointFileDirectoryIpTests>();

  // RunAsync is intercepted only when this field is assigned from a Build() in this compilation.
  private static NuruApp App = null!;

  private static void CreateApp(TestTerminal terminal)
  {
    App = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T591007ScanCommand>()
      .Map<T591007ReadCommand>()
      .Map<T591007PlaceQuery>()
      .Map<T591007OptionalQuery>()
      .Build();
  }

  public static async Task Should_bind_a_directoryinfo_parameter()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t591007-scan", "/tmp/scan-dir"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("path:scan-dir|output:none|address:none").ShouldBeTrue();
  }

  public static async Task Should_bind_a_fileinfo_option()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t591007-scan", "--output", "/tmp/scan-out.txt"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("path:none|output:scan-out.txt|address:none").ShouldBeTrue();
  }

  public static async Task Should_bind_a_required_fileinfo_parameter()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t591007-read", "/etc/hosts"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("file:hosts").ShouldBeTrue();
  }

  public static async Task Should_bind_an_ipaddress_option()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int ipv4 = await App.RunAsync(["t591007-scan", "--address", "192.168.1.1"]);
    ipv4.ShouldBe(0);
    terminal.OutputContains("path:none|output:none|address:192.168.1.1").ShouldBeTrue();

    using TestTerminal ipv6Terminal = new();
    CreateApp(ipv6Terminal);
    int ipv6 = await App.RunAsync(["t591007-scan", "--address", "::1"]);
    ipv6.ShouldBe(0);
    ipv6Terminal.OutputContains("address:::1").ShouldBeTrue();
  }

  public static async Task Should_reject_an_invalid_ipaddress_option()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t591007-scan", "--address", "not-an-ip"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("Invalid value 'not-an-ip'").ShouldBeTrue();
    terminal.ErrorContains("Expected: ipaddress").ShouldBeTrue();
  }

  public static async Task Should_leave_nullable_properties_unset_when_omitted()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t591007-scan"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("path:none|output:none|address:none").ShouldBeTrue();
  }

  public static async Task Should_bind_operation_fileinfo_directoryinfo_and_ipaddress()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(
    [
      "t591007-place",
      "--Input", "/tmp/place-in.txt",
      "--Destination", "/tmp/place-out",
      "--Address", "10.0.0.8"
    ]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("input:place-in.txt|destination:place-out|address:10.0.0.8").ShouldBeTrue();
  }

  public static async Task Should_bind_nullable_operation_fileinfo_directoryinfo_ipaddress_and_uri()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(
    [
      "t591007-optional",
      "--Input", "/tmp/opt-in.txt",
      "--Destination", "/tmp/opt-out",
      "--Address", "10.1.2.3",
      "--Location", "https://example.com/a"
    ]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("input:opt-in.txt|destination:opt-out|address:10.1.2.3|location:https://example.com/a").ShouldBeTrue();
  }

  public static async Task Should_leave_nullable_operation_properties_unset_when_omitted()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t591007-optional"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("input:none|destination:none|address:none|location:none").ShouldBeTrue();
  }
}

[NuruRoute("t591007-scan", Description = "Scan a folder")]
public sealed class T591007ScanCommand : IQuery<string>
{
  [Parameter(Description = "Folder to scan")]
  public DirectoryInfo? Path { get; set; }

  [Option("output", "o", Description = "Output file")]
  public FileInfo? Output { get; set; }

  [Option("address", "a", Description = "Bind address")]
  public IPAddress? Address { get; set; }

  public sealed class Handler : IRequestHandler<T591007ScanCommand, string>
  {
    public ValueTask<string> Handle(T591007ScanCommand query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      string path = query.Path?.Name ?? "none";
      string output = query.Output?.Name ?? "none";
      string address = query.Address?.ToString() ?? "none";
      return ValueTask.FromResult($"path:{path}|output:{output}|address:{address}");
    }
  }
}

[NuruRoute("t591007-read", Description = "Read a file")]
public sealed class T591007ReadCommand : IQuery<string>
{
  [Parameter(Description = "File to read")]
  public FileInfo File { get; set; } = null!;

  public sealed class Handler : IRequestHandler<T591007ReadCommand, string>
  {
    public ValueTask<string> Handle(T591007ReadCommand query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      return ValueTask.FromResult($"file:{query.File.Name}");
    }
  }
}

[Operation("t591007-place", Description = "Place a file")]
public sealed class T591007PlaceQuery : IQuery<string>
{
  public required FileInfo Input { get; set; }

  public required DirectoryInfo Destination { get; set; }

  public required IPAddress Address { get; set; }

  public sealed class Handler : IRequestHandler<T591007PlaceQuery, string>
  {
    public ValueTask<string> Handle(T591007PlaceQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      return ValueTask.FromResult(
        $"input:{query.Input.Name}|destination:{query.Destination.Name}|address:{query.Address}");
    }
  }
}

[Operation("t591007-optional", Description = "Optional file, folder, address, and uri")]
public sealed class T591007OptionalQuery : IQuery<string>
{
  public FileInfo? Input { get; set; }

  public DirectoryInfo? Destination { get; set; }

  public IPAddress? Address { get; set; }

  public Uri? Location { get; set; }

  public sealed class Handler : IRequestHandler<T591007OptionalQuery, string>
  {
    public ValueTask<string> Handle(T591007OptionalQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      string input = query.Input?.Name ?? "none";
      string destination = query.Destination?.Name ?? "none";
      string address = query.Address?.ToString() ?? "none";
      string location = query.Location?.ToString() ?? "none";
      return ValueTask.FromResult(
        $"input:{input}|destination:{destination}|address:{address}|location:{location}");
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
