#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru.Parsing;
using TimeWarp.Nuru.Serialization;
using System.Text.Json;
using static System.Console;

WriteLine("ParseException JSON Round-Trip Test");
WriteLine("====================================");
WriteLine();

// This is exactly what the user requested
var ex = new ParseException("Test error", new InvalidOperationException("Inner"));
string json = JsonSerializer.Serialize(ex, NuruJsonSerializerContext.Default.ParseException);
ParseException? deserialized = JsonSerializer.Deserialize(json, NuruJsonSerializerContext.Default.ParseException);

WriteLine($"Original message: {ex.Message}");
WriteLine($"Deserialized message: {deserialized?.Message}");
WriteLine($"Messages match: {ex.Message == deserialized?.Message}");
WriteLine();
WriteLine($"Original inner: {ex.InnerException?.Message}");
WriteLine($"Deserialized inner: {deserialized?.InnerException?.Message}");
WriteLine($"Inner messages match: {ex.InnerException?.Message == deserialized?.InnerException?.Message}");

return (ex.Message == deserialized?.Message) ? 0 : 1;