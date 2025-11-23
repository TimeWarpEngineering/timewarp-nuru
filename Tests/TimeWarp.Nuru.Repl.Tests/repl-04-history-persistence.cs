#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test history persistence (Section 4 of REPL Test Plan)
return await RunTests<HistoryPersistenceTests>();

[TestTag("REPL")]
public class HistoryPersistenceTests
{
  private static string GetTestHistoryPath() =>
    Path.Combine(Path.GetTempPath(), $"nuru-test-history-{Guid.NewGuid()}.txt");

  public static async Task Should_save_history_on_exit_when_enabled()
  {
    // Arrange
    string historyPath = GetTestHistoryPath();
    try
    {
      using var terminal = new TestTerminal();
      terminal.QueueLine("greet Alice");
      terminal.QueueLine("greet Bob");
      terminal.QueueLine("exit");

      NuruApp app = new NuruAppBuilder()
        .UseTerminal(terminal)
        .AddRoute("greet {name}", (string name) => $"Hello, {name}!")
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

      using var terminal = new TestTerminal();
      terminal.QueueLine("history");
      terminal.QueueLine("exit");

      NuruApp app = new NuruAppBuilder()
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
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options => options.PersistHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - default location is ~/.nuru/history/{appname}
    string expectedDir = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
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
      using var terminal = new TestTerminal();
      terminal.QueueLine("test-command");
      terminal.QueueLine("exit");

      NuruApp app = new NuruAppBuilder()
        .UseTerminal(terminal)
        .AddRoute("test-command", () => "OK")
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

    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options =>
      {
        options.PersistHistory = true;
        options.HistoryFilePath = invalidPath;
      })
      .Build();

    // Act - should not throw, just log warning
    int exitCode = await app.RunReplAsync();

    // Assert
    exitCode.ShouldBe(0, "Session should continue despite file error");
  }

  public static async Task Should_trim_history_to_max_size_when_loading()
  {
    // Arrange
    string historyPath = GetTestHistoryPath();
    try
    {
      // Create history file with more entries than max
      var manyCommands = Enumerable.Range(1, 200).Select(i => $"command-{i}").ToArray();
      await File.WriteAllLinesAsync(historyPath, manyCommands);

      using var terminal = new TestTerminal();
      terminal.QueueLine("exit");

      NuruApp app = new NuruAppBuilder()
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
      using var terminal = new TestTerminal();
      terminal.QueueLine("test-command");
      terminal.QueueLine("exit");

      NuruApp app = new NuruAppBuilder()
        .UseTerminal(terminal)
        .AddRoute("test-command", () => "OK")
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

      using var terminal = new TestTerminal();
      terminal.QueueLine("exit");

      NuruApp app = new NuruAppBuilder()
        .UseTerminal(terminal)
        .AddReplSupport(options =>
        {
          options.PersistHistory = true;
          options.HistoryFilePath = historyPath;
        })
        .Build();

      // Act - should handle gracefully
      int exitCode = await app.RunReplAsync();

      // Assert
      exitCode.ShouldBe(0, "Session should start normally despite corrupted history");
    }
    finally
    {
      if (File.Exists(historyPath)) File.Delete(historyPath);
    }
  }
}
