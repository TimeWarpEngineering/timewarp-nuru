#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using static System.Console;

WriteLine("Testing how shell passes arguments:");
WriteLine();

WriteLine("Args received:");
for (int i = 0; i < args.Length; i++)
{
    WriteLine($"  [{i}] = '{args[i]}'");
}

WriteLine();
WriteLine("To test, run:");
WriteLine("  ./test-shell-behavior.cs git commit -m \"Test message\"");
WriteLine("  ./test-shell-behavior.cs git commit -m Test message");