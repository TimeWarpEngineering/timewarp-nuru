#!/usr/bin/dotnet --
// hyperlink-widget-demo - Demonstrates OSC 8 hyperlinks in terminal output
// GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/95
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Terminal;

// Get a terminal instance
ITerminal terminal = NuruTerminal.Default;

terminal.WriteLine();
terminal.WriteLine("OSC 8 Hyperlink Demo".Cyan().Bold());
terminal.WriteLine("Demonstrates clickable hyperlinks in supported terminals");
terminal.WriteLine();

// Check terminal support
terminal.WriteLine($"Terminal hyperlink support: {(terminal.SupportsHyperlinks ? "✓ Yes".Green() : "✗ No".Yellow())}");
terminal.WriteLine();

// 1. Simple hyperlink using string extension
terminal.WriteLine("1. String extension - Link():");
terminal.WriteLine($"   Visit {"Ardalis.com".Link("https://ardalis.com")}");
terminal.WriteLine();

// 2. Terminal extension methods
terminal.WriteLine("2. Terminal extension - WriteLink():");
terminal.Write("   Check out: ");
terminal.WriteLink("https://github.com", "GitHub");
terminal.WriteLine();
terminal.WriteLine();

// 3. WriteLinkLine with just URL (URL as display text)
terminal.WriteLine("3. URL as display text:");
terminal.Write("   ");
terminal.WriteLinkLine("https://docs.microsoft.com/dotnet");
terminal.WriteLine();

// 4. Chaining with color extensions
terminal.WriteLine("4. Hyperlinks with styling:");
terminal.WriteLine($"   {"Download here".Link("https://example.com/download").Blue().Underline()}");
terminal.WriteLine($"   {"Read the docs".Link("https://docs.example.com").Cyan().Bold()}");
terminal.WriteLine($"   {"Report a bug".Link("https://github.com/issues").Yellow()}");
terminal.WriteLine();

// 5. Multiple links in one line
terminal.WriteLine("5. Multiple links in one line:");
terminal.WriteLine($"   {"Home".Link("https://example.com")} | {"About".Link("https://example.com/about")} | {"Contact".Link("https://example.com/contact")}");
terminal.WriteLine();

// 6. Links in formatted text
terminal.WriteLine("6. Links in formatted output:");
terminal.WriteLine($"   For more information, see the {"documentation".Link("https://docs.example.com").Cyan()}");
terminal.WriteLine($"   or visit our {"community forum".Link("https://forum.example.com").Green()}.");
terminal.WriteLine();

// 7. Practical example - CLI help with links
terminal.WriteLine("7. Practical example - Help text with links:");
terminal.WriteLine();
terminal.WritePanel(panel => panel
    .Header("TimeWarp.Nuru".Cyan().Bold())
    .Content(
        "A fluent CLI framework for .NET 10\n\n" +
        $"Documentation: {"https://timewarp.dev/nuru".Link("https://timewarp.dev/nuru").Cyan()}\n" +
        $"Source:        {"GitHub".Link("https://github.com/TimeWarpEngineering/timewarp-nuru").Cyan()}\n" +
        $"Issues:        {"Report bugs".Link("https://github.com/TimeWarpEngineering/timewarp-nuru/issues").Yellow()}")
    .Border(BorderStyle.Rounded)
    .BorderColor(AnsiColors.Cyan)
    .Padding(2, 1));
terminal.WriteLine();

// 8. Conditional hyperlinks
terminal.WriteLine("8. Graceful degradation:");
if (terminal.SupportsHyperlinks)
{
  terminal.WriteLine($"   Your terminal supports hyperlinks! {"Try clicking this".Link("https://example.com").Green()}");
}
else
{
  terminal.WriteLine("   Your terminal doesn't support OSC 8 hyperlinks.");
  terminal.WriteLine("   The text still displays, but won't be clickable.");
  terminal.WriteLine("   Supported terminals: Windows Terminal, iTerm2, VS Code, Hyper, Konsole, GNOME Terminal 3.26+");
}
terminal.WriteLine();

// 9. Technical details
terminal.WriteLine("9. OSC 8 escape sequence format:");
terminal.WriteLine("   \\e]8;;URL\\e\\\\DISPLAY_TEXT\\e]8;;\\e\\\\".Gray());
terminal.WriteLine();

terminal.WriteLine("Demo complete! Try running this in different terminals to see hyperlink support.".Gray());
terminal.WriteLine();

return 0;
