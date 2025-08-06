#!/usr/bin/dotnet --
#:project Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using System;

// Test nameof with type aliases
Console.WriteLine($"nameof(string) = Can't use nameof on aliases");
Console.WriteLine($"nameof(String) = {nameof(String)}");
Console.WriteLine($"nameof(Int32) = {nameof(Int32)}");
Console.WriteLine($"nameof(Int64) = {nameof(Int64)}");
Console.WriteLine($"nameof(Double) = {nameof(Double)}");
Console.WriteLine($"nameof(Decimal) = {nameof(Decimal)}");
Console.WriteLine($"nameof(Boolean) = {nameof(Boolean)}");
Console.WriteLine($"nameof(DateTime) = {nameof(DateTime)}");
Console.WriteLine($"nameof(Guid) = {nameof(Guid)}");
Console.WriteLine($"nameof(TimeSpan) = {nameof(TimeSpan)}");

// The problem is:
// nameof(int) doesn't work - "int" is a keyword alias
// nameof(Int32) works - gives "Int32" not "int"