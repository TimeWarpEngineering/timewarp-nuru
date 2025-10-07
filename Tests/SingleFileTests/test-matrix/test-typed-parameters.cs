#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test: Typed Parameters
// Pattern: {param:type}
// Validates that parameters can be constrained to specific types

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Typed Parameters
  ========================
  Patterns with type constraints like {seconds:int}, {amount:double}
  """
);

NuruAppBuilder builder = new();

// Integer parameter
builder.AddRoute("wait {seconds:int}", (int seconds) =>
{
    WriteLine($"✓ Wait for {seconds} seconds");
});

// Double parameter
builder.AddRoute("price {amount:double}", (double amount) =>
{
    WriteLine($"✓ Price set to ${amount:F2}");
});

// Boolean parameter
builder.AddRoute("enable {feature} {active:bool}", (string feature, bool active) =>
{
    WriteLine($"✓ Feature '{feature}' is {(active ? "enabled" : "disabled")}");
});

// DateTime parameter
builder.AddRoute("schedule {event} {when:DateTime}", (string eventName, DateTime when) =>
{
    WriteLine($"✓ Event '{eventName}' scheduled for {when:yyyy-MM-dd HH:mm}");
});

// Guid parameter
builder.AddRoute("lookup {id:Guid}", (Guid id) =>
{
    WriteLine($"✓ Looking up entity with ID: {id}");
});

// Long parameter
builder.AddRoute("process {bytes:long}", (long bytes) =>
{
    WriteLine($"✓ Processing {bytes:N0} bytes");
});

// Decimal parameter for money
builder.AddRoute("transfer {amount:decimal}", (decimal amount) =>
{
    WriteLine($"✓ Transfer ${amount:C}");
});

// TimeSpan parameter
builder.AddRoute("timeout {duration:TimeSpan}", (TimeSpan duration) =>
{
    WriteLine($"✓ Timeout set to {duration}");
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: wait 30
  Expected: Match, seconds=30
  """
);

try
{
    await app.RunAsync(["wait", "30"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 2: wait abc
  Expected: No match (type mismatch)
  """
);

try
{
    await app.RunAsync(["wait", "abc"]);
}
catch (Exception ex)
{
    WriteLine($"✓ Expected error: {ex.Message}");
}

WriteLine
(
  """

  Test 3: price 29.99
  Expected: Match, amount=29.99
  """
);

try
{
    await app.RunAsync(["price", "29.99"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 4: enable logging true
  Expected: Match, feature="logging", active=true
  """
);

try
{
    await app.RunAsync(["enable", "logging", "true"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 5: schedule meeting 2024-12-25T14:30:00
  Expected: Match with parsed DateTime
  """
);

try
{
    await app.RunAsync(["schedule", "meeting", "2024-12-25T14:30:00"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 6: lookup 550e8400-e29b-41d4-a716-446655440000
  Expected: Match with parsed Guid
  """
);

try
{
    await app.RunAsync(["lookup", "550e8400-e29b-41d4-a716-446655440000"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 7: process 1234567890
  Expected: Match with long value
  """
);

try
{
    await app.RunAsync(["process", "1234567890"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 8: transfer 1234.56
  Expected: Match with decimal value
  """
);

try
{
    await app.RunAsync(["transfer", "1234.56"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 9: timeout 00:05:30
  Expected: Match with TimeSpan value
  """
);

try
{
    await app.RunAsync(["timeout", "00:05:30"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  ========================================
  Supported types: string, int, long, double,
  decimal, bool, DateTime, Guid, TimeSpan
  """
);