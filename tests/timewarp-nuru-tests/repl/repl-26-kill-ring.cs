#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test kill ring functionality (PSReadLine kill/yank operations)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.KillRingTests
{

[TestTag("REPL")]
public class KillRingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<KillRingTests>();

  // === Kill Ring Unit Tests ===

  public static async Task Should_add_text_to_kill_ring()
  {
    // Arrange
    KillRing ring = new();

    // Act
    ring.Add("hello");

    // Assert
    ring.ItemCount.ShouldBe(1);
    ring.IsEmpty.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_yank_most_recent_text()
  {
    // Arrange
    KillRing ring = new();
    ring.Add("first");
    ring.Add("second");
    ring.Add("third");

    // Act
    string? yanked = ring.Yank();

    // Assert
    yanked.ShouldBe("third");

    await Task.CompletedTask;
  }

  public static async Task Should_cycle_through_ring_with_yank_pop()
  {
    // Arrange
    KillRing ring = new();
    ring.Add("first");
    ring.Add("second");
    ring.Add("third");
    ring.Yank(); // Must call Yank first

    // Act & Assert
    ring.YankPop().ShouldBe("second");
    ring.YankPop().ShouldBe("first");
    ring.YankPop().ShouldBe("third"); // Wraps around

    await Task.CompletedTask;
  }

  public static async Task Should_append_to_last_entry_when_consecutive_kills()
  {
    // Arrange
    KillRing ring = new();
    ring.Add("hello");

    // Act - Append (forward kill)
    ring.AppendToLast(" world", prepend: false);

    // Assert
    ring.Yank().ShouldBe("hello world");

    await Task.CompletedTask;
  }

  public static async Task Should_prepend_to_last_entry_for_backward_kills()
  {
    // Arrange
    KillRing ring = new();
    ring.Add("world");

    // Act - Prepend (backward kill)
    ring.AppendToLast("hello ", prepend: true);

    // Assert
    ring.Yank().ShouldBe("hello world");

    await Task.CompletedTask;
  }

  public static async Task Should_not_allow_yank_pop_without_prior_yank()
  {
    // Arrange
    KillRing ring = new();
    ring.Add("text");
    // Don't call Yank()

    // Act
    string? result = ring.YankPop();

    // Assert
    result.ShouldBeNull();
    ring.CanYankPop.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_respect_ring_capacity()
  {
    // Arrange
    KillRing ring = new(capacity: 3);

    // Act - Add more than capacity
    ring.Add("first");
    ring.Add("second");
    ring.Add("third");
    ring.Add("fourth"); // Should overwrite "first"

    // Assert
    ring.ItemCount.ShouldBe(3);
    ring.Yank().ShouldBe("fourth");
    ring.YankPop().ShouldBe("third");
    ring.YankPop().ShouldBe("second");
    ring.YankPop().ShouldBe("fourth"); // Wraps, "first" was overwritten

    await Task.CompletedTask;
  }

  public static async Task Should_return_null_from_empty_ring()
  {
    // Arrange
    KillRing ring = new();

    // Act & Assert
    ring.Yank().ShouldBeNull();
    ring.YankPop().ShouldBeNull();
    ring.IsEmpty.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_ignore_empty_text_on_add()
  {
    // Arrange
    KillRing ring = new();

    // Act
    ring.Add("");
    ring.Add(null!);

    // Assert
    ring.IsEmpty.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_clear_ring()
  {
    // Arrange
    KillRing ring = new();
    ring.Add("text");
    ring.Yank();

    // Act
    ring.Clear();

    // Assert
    ring.IsEmpty.ShouldBeTrue();
    ring.CanYankPop.ShouldBeFalse();

    await Task.CompletedTask;
  }

  // === Integration Tests with ReplConsoleReader ===

  public static async Task Should_kill_line_to_end_with_ctrl_k()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'h'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'e'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'l'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'l'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'o'
    terminal.QueueKey(ConsoleKey.K, ctrl: true); // Kill " world"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - "hello" was executed (killed " world")
    terminal.OutputContains("Success!")
      .ShouldBeTrue("KillLine should remove text after cursor");

    await Task.CompletedTask;
  }

  public static async Task Should_kill_backward_to_line_start_with_ctrl_u()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'h'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'e'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'l'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'l'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'o'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after ' '
    terminal.QueueKey(ConsoleKey.U, ctrl: true); // Kill "hello "
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("world")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - "world" was executed (killed "hello ")
    terminal.OutputContains("Success!")
      .ShouldBeTrue("BackwardKillInput should remove text before cursor");

    await Task.CompletedTask;
  }

  public static async Task Should_kill_word_backward_with_ctrl_w()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.W, ctrl: true); // Kill "world"
    terminal.QueueKey(ConsoleKey.W, ctrl: true); // Kill "hello "
    terminal.QueueKeys("greeting");              // Type new word
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greeting")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("UnixWordRubout should delete words backward");

    await Task.CompletedTask;
  }

  public static async Task Should_yank_killed_text_with_ctrl_y()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Home);         // Go to start
    terminal.QueueKey(ConsoleKey.K, ctrl: true); // Kill "hello"
    terminal.QueueKey(ConsoleKey.Y, ctrl: true); // Yank "hello" back
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("Yank should restore killed text");

    await Task.CompletedTask;
  }

  public static async Task Should_cycle_kill_ring_with_alt_y()
  {
    // Arrange
    using TestTerminal terminal = new();
    // Build up multiple entries in kill ring
    terminal.QueueKeys("first");
    terminal.QueueKey(ConsoleKey.U, ctrl: true); // Kill "first"
    terminal.QueueKeys("second");
    terminal.QueueKey(ConsoleKey.U, ctrl: true); // Kill "second"
    terminal.QueueKeys("third");
    terminal.QueueKey(ConsoleKey.U, ctrl: true); // Kill "third"

    // Now yank and cycle
    terminal.QueueKey(ConsoleKey.Y, ctrl: true); // Yank "third"
    terminal.QueueKey(ConsoleKey.Y, alt: true);  // Replace with "second"
    terminal.QueueKey(ConsoleKey.Y, alt: true);  // Replace with "first"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("first")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("YankPop should cycle through kill ring entries");

    await Task.CompletedTask;
  }

  public static async Task Should_append_consecutive_kills_to_same_entry()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("one two three");
    terminal.QueueKey(ConsoleKey.Home);         // Go to start
    terminal.QueueKey(ConsoleKey.K, ctrl: true); // Kill "one two three"
    // Don't type anything between kills - consecutive kills append
    terminal.QueueKey(ConsoleKey.Y, ctrl: true); // Yank back all at once
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("one two three")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("Consecutive kills should be combined when yanked");

    await Task.CompletedTask;
  }

  public static async Task Should_reset_kill_tracking_on_character_input()
  {
    // Arrange
    using TestTerminal terminal = new();
    // Kill some text
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.U, ctrl: true); // Kill "hello"
    // Type a character (resets kill tracking)
    terminal.QueueKeys("x");
    // Kill more text - should be a new kill ring entry
    terminal.QueueKey(ConsoleKey.U, ctrl: true); // Kill "x"
    // Yank should get "x", not "hello" + "x"
    terminal.QueueKey(ConsoleKey.Y, ctrl: true); // Yank "x"
    terminal.QueueKey(ConsoleKey.Y, alt: true);  // YankPop to "hello"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Got hello!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Got hello!")
      .ShouldBeTrue("Character input should start new kill ring entry");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.KillRingTests
