#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

// Tests for Uri, FileInfo, and DirectoryInfo type constraints in source-generated code.
// These types don't have TryParse methods and require special handling:
// - Uri: uses Uri.TryCreate
// - FileInfo/DirectoryInfo: use constructor with try/catch for ArgumentException
//
// Issue #381: Source generator was not emitting conversion code for these types,
// causing CS0103 errors (undefined variables) at compile time.
[TestTag("Routing")]
public class UriFileInfoDirectoryInfoTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<UriFileInfoDirectoryInfoTests>();

  // ============================================================================
  // Uri Tests
  // ============================================================================

  public static async Task Should_bind_uri_parameter_absolute()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("fetch {url:Uri}").WithHandler((Uri url) => $"scheme:{url.Scheme},host:{url.Host}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["fetch", "https://example.com/path"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("scheme:https").ShouldBeTrue();
    terminal.OutputContains("host:example.com").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_uri_parameter_relative()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("open {path:Uri}").WithHandler((Uri path) => $"isAbsolute:{path.IsAbsoluteUri}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["open", "/relative/path"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("isAbsolute:False").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_optional_uri_parameter_when_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("link {url:Uri?}").WithHandler((Uri? url) => $"host:{url?.Host ?? "none"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["link", "https://example.com"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("host:example.com").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_uri_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("download --url {target:Uri}").WithHandler((Uri target) => $"host:{target.Host}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["download", "--url", "https://files.example.com/data.zip"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("host:files.example.com").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_optional_uri_option_when_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("upload --callback {url:Uri?}").WithHandler((Uri? url) => $"callback:{url?.Host ?? "none"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["upload", "--callback", "https://webhook.example.com"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("callback:webhook.example.com").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // FileInfo Tests
  // ============================================================================

  public static async Task Should_bind_fileinfo_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("read {file:FileInfo}").WithHandler((FileInfo file) => $"name:{file.Name},ext:{file.Extension}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["read", "/tmp/test.txt"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:test.txt").ShouldBeTrue();
    terminal.OutputContains("ext:.txt").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_optional_fileinfo_parameter_when_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("edit {file:FileInfo?}").WithHandler((FileInfo? file) => $"name:{file?.Name ?? "none"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["edit", "/tmp/document.md"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:document.md").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_fileinfo_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("process --input {file:FileInfo}").WithHandler((FileInfo file) => $"input:{file.Name}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["process", "--input", "/data/input.csv"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("input:input.csv").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_optional_fileinfo_option_when_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("compile --config {cfg:FileInfo?}").WithHandler((FileInfo? cfg) => $"config:{cfg?.Name ?? "default"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["compile", "--config", "/etc/app.conf"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("config:app.conf").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // DirectoryInfo Tests
  // ============================================================================

  public static async Task Should_bind_directoryinfo_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("list {dir:DirectoryInfo}").WithHandler((DirectoryInfo dir) => $"name:{dir.Name}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["list", "/tmp/mydir"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:mydir").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_optional_directoryinfo_parameter_when_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("scan {dir:DirectoryInfo?}").WithHandler((DirectoryInfo? dir) => $"name:{dir?.Name ?? "none"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["scan", "/var/log"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:log").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_directoryinfo_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("backup --dest {dir:DirectoryInfo}").WithHandler((DirectoryInfo dir) => $"dest:{dir.Name}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["backup", "--dest", "/mnt/backup"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("dest:backup").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_optional_directoryinfo_option_when_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("sync --target {dir:DirectoryInfo?}").WithHandler((DirectoryInfo? dir) => $"target:{dir?.Name ?? "current"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["sync", "--target", "/home/user/docs"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("target:docs").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // Combined Tests
  // ============================================================================

  public static async Task Should_bind_mixed_uri_fileinfo_directoryinfo_parameters()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {source:DirectoryInfo} {target:Uri}").WithHandler((DirectoryInfo source, Uri target) =>
        $"source:{source.Name},target:{target.Host}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "/app/dist", "https://cdn.example.com"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("source:dist").ShouldBeTrue();
    terminal.OutputContains("target:cdn.example.com").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_directoryinfo_with_fileinfo_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("archive {dir:DirectoryInfo} --manifest {file:FileInfo?}").WithHandler((DirectoryInfo dir, FileInfo? file) =>
        $"dir:{dir.Name},manifest:{file?.Name ?? "none"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["archive", "/data/project", "--manifest", "/data/manifest.json"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("dir:project").ShouldBeTrue();
    terminal.OutputContains("manifest:manifest.json").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // Case Insensitivity Tests
  // ============================================================================

  public static async Task Should_bind_uri_with_lowercase_constraint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("get {url:uri}").WithHandler((Uri url) => $"scheme:{url.Scheme}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["get", "http://test.local"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("scheme:http").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_fileinfo_with_mixed_case_constraint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("cat {path:FILEINFO}").WithHandler((FileInfo path) => $"name:{path.Name}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["cat", "/etc/hosts"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:hosts").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_directoryinfo_with_mixed_case_constraint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("ls {path:directoryInfo}").WithHandler((DirectoryInfo path) => $"name:{path.Name}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["ls", "/usr/local"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:local").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
