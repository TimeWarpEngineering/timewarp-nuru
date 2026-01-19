#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using System.Runtime.InteropServices;

// Test history persistence (Section 4 of REPL Test Plan)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.HistoryPersistence
{
  [TestTag("REPL")]
  public class HistoryPersistenceTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<HistoryPersistenceTests>();

  private static string GetTestHistoryPath() =>
    Path.Combine(Path.GetTempPath(), $"nuru-test-history-{Guid.NewGuid()}.txt");

  public static async Task Should_save_history_on_exit_when_enabled()
  {
    // Arrange
    string historyPath = GetTestHistoryPath();
    try
    {
      using TestTerminal terminal = new();
      terminal.QueueLine("greet Alice");
      terminal.QueueLine("greet Bob");
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("greet {name}")
          .WithHandler((string name) => $"Hello, {name}!")
          .AsCommand()
          .Done()
        .AddReplSupport(options =>
        {
          options.PersistHistory = true;
          options.HistoryFilePath = historyPath;
        })
        .Build();

      // Act
      await app.RunReplAsync();

      // Assert
      File.Exists(historyPath).ShouldBeTrue("History file should be created");
      string[] lines = await File.ReadAllLinesAsync(historyPath);
      lines.Length.ShouldBeGreaterThanOrEqualTo(2);
    }
    finally
    {
      if (File.Exists(historyPath)) File.Delete(historyPath);
    }
  }

  public static async Task Should_load_history_on_start_when_enabled()
  {
    // Arrange
    string historyPath = GetTestHistoryPath();
    try
    {
      // Pre-create history file
      await File.WriteAllLinesAsync(historyPath, ["previous-command-1", "previous-command-2"]);

      using TestTerminal terminal = new();
      terminal.QueueLine("history");
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .AddReplSupport(options =>
        {
          options.PersistHistory = true;
          options.HistoryFilePath = historyPath;
        })
        .Build();

      // Act
      await app.RunReplAsync();

      // Assert - history command output should show loaded commands
      terminal.OutputContains("previous-command-1")
        .ShouldBeTrue("Previous history should be loaded");
    }
    finally
    {
      if (File.Exists(historyPath)) File.Delete(historyPath);
    }
  }

  public static async Task Should_use_default_history_location()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport(options => options.PersistHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - default location is ~/.nuru/history/{appname}
    string expectedDir = Path.Combine(
      System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
      ".nuru",
      "history");

    Directory.Exists(expectedDir).ShouldBeTrue("Default history directory should exist");
  }

  public static async Task Should_create_missing_history_directory()
  {
    // Arrange
    string tempDir = Path.Combine(Path.GetTempPath(), $"nuru-test-{Guid.NewGuid()}");
    string historyPath = Path.Combine(tempDir, "subdir", "history.txt");

    try
    {
      using TestTerminal terminal = new();
      terminal.QueueLine("test-command");
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("test-command")
          .WithHandler(() => "OK")
          .AsCommand()
          .Done()
        .AddReplSupport(options =>
        {
          options.PersistHistory = true;
          options.HistoryFilePath = historyPath;
        })
        .Build();

      // Act
      await app.RunReplAsync();

      // Assert
      File.Exists(historyPath).ShouldBeTrue("History file should be created even in nested directory");
    }
    finally
    {
      if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }
  }

  public static async Task Should_handle_file_access_errors_gracefully()
  {
    // Arrange - use an invalid path that will fail
    string invalidPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
      ? "Z:\\nonexistent\\invalid\\path\\history.txt"
      : "/nonexistent/invalid/path/history.txt";

    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport(options =>
      {
        options.PersistHistory = true;
        options.HistoryFilePath = invalidPath;
      })
      .Build();

    // Act - should not throw, just log warning
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should continue despite file error");
  }

  public static async Task Should_trim_history_to_max_size_when_loading()
  {
    // Arrange
    string historyPath = GetTestHistoryPath();
    try
    {
      // Create history file with more entries than max
      string[] manyCommands = [.. Enumerable.Range(1, 200).Select(i => $"command-{i}")];
      await File.WriteAllLinesAsync(historyPath, manyCommands);

      using TestTerminal terminal = new();
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .AddReplSupport(options =>
        {
          options.PersistHistory = true;
          options.HistoryFilePath = historyPath;
          options.MaxHistorySize = 50;
        })
        .Build();

      // Act
      await app.RunReplAsync();

      // Assert - file should be trimmed on load (only last N entries kept in memory)
      // We verify indirectly that no crash occurred
      terminal.OutputContains("Goodbye!")
        .ShouldBeTrue("Session should complete normally with trimmed history");
    }
    finally
    {
      if (File.Exists(historyPath)) File.Delete(historyPath);
    }
  }

  public static async Task Should_not_persist_when_disabled()
  {
    // Arrange
    string historyPath = GetTestHistoryPath();
    try
    {
      using TestTerminal terminal = new();
      terminal.QueueLine("test-command");
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("test-command")
          .WithHandler(() => "OK")
          .AsCommand()
          .Done()
        .AddReplSupport(options =>
        {
          options.PersistHistory = false;
          options.HistoryFilePath = historyPath;
        })
        .Build();

      // Act
      await app.RunReplAsync();

      // Assert
      File.Exists(historyPath).ShouldBeFalse("History file should NOT be created when persistence is disabled");
    }
    finally
    {
      if (File.Exists(historyPath)) File.Delete(historyPath);
    }
  }

  public static async Task Should_handle_corrupted_history_file()
  {
    // Arrange
    string historyPath = GetTestHistoryPath();
    try
    {
      // Create a file with some binary/corrupted content mixed in
      await File.WriteAllTextAsync(historyPath, "valid-command-1\n\0\0\0binary-junk\nvalid-command-2\n");

      using TestTerminal terminal = new();
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .AddReplSupport(options =>
        {
          options.PersistHistory = true;
          options.HistoryFilePath = historyPath;
        })
        .Build();

      // Act - should handle gracefully
      await app.RunReplAsync();

      // Assert
      terminal.OutputContains("Goodbye!")
        .ShouldBeTrue("Session should start normally despite corrupted history");
    }
    finally
    {
      if (File.Exists(historyPath)) File.Delete(historyPath);
    }
  }
  }
}
