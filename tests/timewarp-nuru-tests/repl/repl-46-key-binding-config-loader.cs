#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins the JSON key binding profile loader: parsing, action registry, inheritance and search order.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.KeyBindingConfigLoaderTests
{
[TestTag("REPL")]
public class KeyBindingConfigLoaderTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<KeyBindingConfigLoaderTests>();

  private sealed class EmptyRoutes : IReplRouteProvider
  {
    public IReadOnlyList<string> GetCommandPrefixes() => [];

    public IEnumerable<CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace) => [];

    public bool IsKnownCommand(string token) => false;
  }

  private static ReplConsoleReader CreateReader(TestTerminal terminal)
  {
    ReplOptions options = new()
    {
      PersistHistory = false,
      EnableColors = false,
      KeyBindingProfile = new DefaultKeyBindingProfile()
    };

    return new ReplConsoleReader
    (
      history: [],
      routeProvider: new EmptyRoutes(),
      replOptions: options,
      loggerFactory: null,
      terminal: terminal
    );
  }

  public static async Task Parser_should_parse_documented_combinations()
  {
    (string Text, ConsoleKey Key, ConsoleModifiers Modifiers)[] cases =
    [
      ("A", ConsoleKey.A, ConsoleModifiers.None),
      ("Ctrl+A", ConsoleKey.A, ConsoleModifiers.Control),
      ("Alt+F", ConsoleKey.F, ConsoleModifiers.Alt),
      ("Ctrl+Shift+K", ConsoleKey.K, ConsoleModifiers.Control | ConsoleModifiers.Shift),
      ("Ctrl+Shift+Left", ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift),
      ("Enter", ConsoleKey.Enter, ConsoleModifiers.None),
      ("Escape", ConsoleKey.Escape, ConsoleModifiers.None),
      ("Tab", ConsoleKey.Tab, ConsoleModifiers.None),
      ("Shift+Tab", ConsoleKey.Tab, ConsoleModifiers.Shift),
      ("Alt+.", ConsoleKey.OemPeriod, ConsoleModifiers.Alt),
      ("Alt+<", ConsoleKey.OemComma, ConsoleModifiers.Alt | ConsoleModifiers.Shift),
      ("Control+a", ConsoleKey.A, ConsoleModifiers.Control),
      ("ctrl + k", ConsoleKey.K, ConsoleModifiers.Control)
    ];

    foreach ((string text, ConsoleKey key, ConsoleModifiers modifiers) in cases)
    {
      (ConsoleKey parsedKey, ConsoleModifiers parsedModifiers) = KeyBindingComboParser.Parse(text);
      parsedKey.ShouldBe(key, text);
      parsedModifiers.ShouldBe(modifiers, text);
    }

    await Task.CompletedTask;
  }

  public static async Task Parser_should_reject_bad_combinations()
  {
    Should.Throw<KeyBindingConfigException>(() => KeyBindingComboParser.Parse("Ctrl+Nope"));
    Should.Throw<KeyBindingConfigException>(() => KeyBindingComboParser.Parse("Ctrl+Shift"));
    Should.Throw<KeyBindingConfigException>(() => KeyBindingComboParser.Parse("Ctrl+Ctrl+A"));
    Should.Throw<ArgumentException>(() => KeyBindingComboParser.Parse("  "));
    await Task.CompletedTask;
  }

  public static async Task Registry_should_list_existing_handlers_and_resolve_aliases()
  {
    IReadOnlyList<string> names = KeyBindingActionRegistry.GetAvailableActions();
    names.Count.ShouldBe(67, "Canonical actions are the handlers that exist today, with one name per digit argument");
    names.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(names.Count);

    string[] required =
    [
      "BackwardChar",
      "ForwardChar",
      "BackwardWord",
      "ForwardWord",
      "BeginningOfLine",
      "EndOfLine",
      "PreviousHistory",
      "NextHistory",
      "BeginningOfHistory",
      "EndOfHistory",
      "HistorySearchBackward",
      "HistorySearchForward",
      "ReverseSearchHistory",
      "ForwardSearchHistory",
      "BackwardDeleteChar",
      "DeleteChar",
      "DeleteCharOrExit",
      "Escape",
      "KillLine",
      "DeleteWordBackward",
      "DeleteToLineStart",
      "ClearScreen",
      "ToggleInsertMode",
      "UpcaseWord",
      "DowncaseWord",
      "CapitalizeWord",
      "SwapCharacters",
      "DeleteWord",
      "BackwardDeleteWord",
      "SelectBackwardChar",
      "SelectForwardChar",
      "SelectBackwardWord",
      "SelectNextWord",
      "SelectBackwardsLine",
      "SelectLine",
      "SelectAll",
      "CopyOrCancelLine",
      "Cut",
      "Paste",
      "DeleteSelection",
      "KillLineToRing",
      "BackwardKillInput",
      "UnixWordRubout",
      "KillWord",
      "BackwardKillWord",
      "Yank",
      "YankPop",
      "YankLastArg",
      "YankNthArg",
      "Undo",
      "Redo",
      "RevertLine",
      "TabComplete",
      "TabCompleteReverse",
      "PossibleCompletions",
      "AddLine",
      "Enter"
    ];

    foreach (string name in required)
    {
      names.Contains(name).ShouldBeTrue(name);
      KeyBindingActionRegistry.IsKnownAction(name).ShouldBeTrue(name);
    }

    for (int digit = 0; digit <= 9; digit++)
    {
      string digitName = $"DigitArgument:{digit}";
      names.Contains(digitName).ShouldBeTrue(digitName);
    }

    names.Contains("TabComplete:reverse").ShouldBeFalse("The reverse alias is accepted but not listed twice");
    names.Contains("CharacterWithOverwrite").ShouldBeFalse("Overwrite is ToggleInsertMode plus typed characters");
    KeyBindingActionRegistry.IsKnownAction("TabComplete:reverse").ShouldBeTrue();
    KeyBindingActionRegistry.IsKnownAction("tabcomplete").ShouldBeTrue();
    KeyBindingActionRegistry.IsKnownAction("Bell").ShouldBeFalse();
    KeyBindingActionRegistry.IsKnownAction("CharacterWithOverwrite").ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Registry_should_create_handlers_for_parameterized_actions()
  {
    using TestTerminal terminal = new();
    ReplConsoleReader reader = CreateReader(terminal);

    await KeyBindingActionRegistry.GetAction("DigitArgument:4", reader)().ConfigureAwait(false);
    await KeyBindingActionRegistry.GetAction("TabComplete", reader)().ConfigureAwait(false);
    await KeyBindingActionRegistry.GetAction("TabCompleteReverse", reader)().ConfigureAwait(false);
    await KeyBindingActionRegistry.GetAction("TabComplete:reverse", reader)().ConfigureAwait(false);

    Should.Throw<KeyBindingConfigException>(() => KeyBindingActionRegistry.GetAction("Bell", reader));
  }

  public static async Task Loader_should_parse_json_and_apply_base_override_add_and_remove()
  {
    using TestTerminal terminal = new();
    ReplConsoleReader reader = CreateReader(terminal);

    string json =
      """
      {
        "name": "Mine",
        "baseProfile": "Emacs",
        "overrides": {
          "Ctrl+K": "KillLineToRing"
        },
        "additions": {
          "Left": "BackwardChar",
          "Ctrl+K": "Escape"
        },
        "removals": [
          "Ctrl+D"
        ],
        "exitKeys": [
          "Escape"
        ]
      }
      """;

    CustomKeyBindingProfile profile = KeyBindingConfigLoader.LoadFromJson(json);
    profile.Name.ShouldBe("Mine");

    Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<Task>> bindings = profile.GetBindings(reader);
    bindings.ContainsKey((ConsoleKey.D, ConsoleModifiers.Control)).ShouldBeFalse("Ctrl+D was removed");
    bindings.ContainsKey((ConsoleKey.LeftArrow, ConsoleModifiers.None)).ShouldBeTrue("Left was added");
    BindingMethod(bindings, ConsoleKey.K, ConsoleModifiers.Control, reader, "Escape")
      .ShouldBeTrue("Additions are applied after overrides");
    BindingMethod(bindings, ConsoleKey.LeftArrow, ConsoleModifiers.None, reader, "BackwardChar")
      .ShouldBeTrue("Left should run BackwardChar");

    EmacsKeyBindingProfile emacs = new();
    Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<Task>> emacsBindings = emacs.GetBindings(reader);
    bindings[(ConsoleKey.A, ConsoleModifiers.Control)].Method
      .ShouldBe(emacsBindings[(ConsoleKey.A, ConsoleModifiers.Control)].Method, "Unchanged Emacs bindings are kept");

    HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> exitKeys = profile.GetExitKeys();
    exitKeys.Contains((ConsoleKey.Enter, ConsoleModifiers.None)).ShouldBeTrue("Base exit keys remain");
    exitKeys.Contains((ConsoleKey.Escape, ConsoleModifiers.None)).ShouldBeTrue("exitKeys marks Escape");
    await Task.CompletedTask;
  }

  public static async Task Loader_should_name_a_base_only_profile_after_the_base()
  {
    CustomKeyBindingProfile profile = KeyBindingConfigLoader.LoadFromJson("""{"baseProfile":"Emacs"}""");
    profile.Name.ShouldBe("Emacs");
    await Task.CompletedTask;
  }

  public static async Task Loader_should_accept_comments_and_trailing_commas()
  {
    CustomKeyBindingProfile profile = KeyBindingConfigLoader.LoadFromJson
    (
      """
      {
        // team default
        "name": "Commented",
        "baseProfile": "Default",
      }
      """
    );

    profile.Name.ShouldBe("Commented");
    await Task.CompletedTask;
  }

  public static async Task Loader_should_report_line_numbers_for_invalid_config()
  {
    KeyBindingConfigException unknownAction = Should.Throw<KeyBindingConfigException>
    (
      () => KeyBindingConfigLoader.LoadFromJson("{\n  \"overrides\": {\n    \"Ctrl+K\": \"Bell\"\n  }\n}\n")
    );
    unknownAction.Message.ShouldContain("Line 3");
    unknownAction.Message.ShouldContain("Bell");

    KeyBindingConfigException unknownKey = Should.Throw<KeyBindingConfigException>
    (
      () => KeyBindingConfigLoader.LoadFromJson("{\n  \"removals\": [\"Ctrl+Nope\"]\n}\n")
    );
    unknownKey.Message.ShouldContain("Line 2");
    unknownKey.Message.ShouldContain("Ctrl+Nope");

    KeyBindingConfigException badBase = Should.Throw<KeyBindingConfigException>
    (
      () => KeyBindingConfigLoader.LoadFromJson("{\n  \"baseProfile\": \"Nope\"\n}\n")
    );
    badBase.Message.ShouldContain("Line 2");
    badBase.Message.ShouldContain("Nope");

    KeyBindingConfigException brokenJson = Should.Throw<KeyBindingConfigException>
    (
      () => KeyBindingConfigLoader.LoadFromJson("{\n  \"name\": \n")
    );
    brokenJson.Message.ShouldContain("Invalid key binding JSON");
    brokenJson.Message.ShouldContain("Line ");

    Should.Throw<KeyBindingConfigException>(() => KeyBindingConfigLoader.LoadFromJson("""{"foo": true}"""));
    await Task.CompletedTask;
  }

  private static bool BindingMethod
  (
    Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<Task>> bindings,
    ConsoleKey key,
    ConsoleModifiers modifiers,
    ReplConsoleReader reader,
    string actionName
  )
  {
    if (!bindings.TryGetValue((key, modifiers), out Func<Task>? actual))
    {
      return false;
    }

    Func<Task> expected = KeyBindingActionRegistry.GetAction(actionName, reader);
    return actual.Method == expected.Method;
  }
}
}
