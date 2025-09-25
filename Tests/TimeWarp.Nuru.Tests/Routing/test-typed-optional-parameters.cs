#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test: Typed Optional Parameters
// Pattern: {param:type?}
// Validates that parameters can be both typed AND optional

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Typed Optional Parameters
  ==================================
  Patterns with optional typed constraints like {seconds:int?}
  """
);

NuruAppBuilder builder = new();

// Optional integer parameter
builder.AddRoute("wait {message} {seconds:int?}", (string message, int? seconds) =>
{
    if (seconds.HasValue)
    {
        WriteLine($"✓ Wait {seconds} seconds: {message}");
    }
    else
    {
        WriteLine($"✓ Wait indefinitely: {message}");
    }
});

// Optional double parameter
builder.AddRoute("discount {product} {percent:double?}", (string product, double? percent) =>
{
    if (percent.HasValue)
    {
        WriteLine($"✓ Product '{product}' discounted by {percent}%");
    }
    else
    {
        WriteLine($"✓ Product '{product}' at full price");
    }
});

// Optional DateTime parameter
builder.AddRoute("remind {task} {when:DateTime?}", (string task, DateTime? when) =>
{
    if (when.HasValue)
    {
        WriteLine($"✓ Reminder for '{task}' set at {when:yyyy-MM-dd HH:mm}");
    }
    else
    {
        WriteLine($"✓ Reminder for '{task}' with no specific time");
    }
});

// Optional with flags
builder.AddRoute("build {project} --threads? {count:int?} --config? {mode?}",
    (string project, int? count, string? mode) =>
{
    WriteLine($"✓ Building {project}:");
    WriteLine($"  Threads: {(count.HasValue ? count.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "default")}");
    WriteLine($"  Config: {mode ?? "Release"}");
});

// Mix of required typed and optional typed
builder.AddRoute("range {min:int} {max:int?}", (int min, int? max) =>
{
    if (max.HasValue)
    {
        WriteLine($"✓ Range from {min} to {max}");
    }
    else
    {
        WriteLine($"✓ Range starting from {min}");
    }
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: wait "Loading..." 5
  Expected: Match, message="Loading...", seconds=5
  """
);

try
{
    await app.RunAsync(["wait", "Loading...", "5"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 2: wait "Processing..."
  Expected: Match, message="Processing...", seconds=null
  """
);

try
{
    await app.RunAsync(["wait", "Processing..."]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 3: discount laptop 15.5
  Expected: Match, product="laptop", percent=15.5
  """
);

try
{
    await app.RunAsync(["discount", "laptop", "15.5"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 4: discount laptop
  Expected: Match, product="laptop", percent=null
  """
);

try
{
    await app.RunAsync(["discount", "laptop"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 5: remind "Call client" 2024-12-25T10:00:00
  Expected: Match with DateTime
  """
);

try
{
    await app.RunAsync(["remind", "Call client", "2024-12-25T10:00:00"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 6: remind "Check email"
  Expected: Match with when=null
  """
);

try
{
    await app.RunAsync(["remind", "Check email"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 7: build myapp --threads 8 --config Debug
  Expected: Match with all values
  """
);

try
{
    await app.RunAsync(["build", "myapp", "--threads", "8", "--config", "Debug"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 8: build myapp
  Expected: Match with default values
  """
);

try
{
    await app.RunAsync(["build", "myapp"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 9: range 1 100
  Expected: Match, min=1, max=100
  """
);

try
{
    await app.RunAsync(["range", "1", "100"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 10: range 1
  Expected: Match, min=1, max=null
  """
);

try
{
    await app.RunAsync(["range", "1"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 11: wait "Test" abc
  Expected: No match (type error on optional param)
  """
);

try
{
    await app.RunAsync(["wait", "Test", "abc"]);
}
catch (Exception ex)
{
    WriteLine($"✓ Expected error: {ex.Message}");
}

WriteLine
(
  """

  ========================================
  Typed optional parameters combine type
  safety with flexibility. The ? makes the
  parameter optional, type ensures validation.
  """
);