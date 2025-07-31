#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

// Test descriptions
var app = new NuruAppBuilder()
    .AddRoute("hello {name|Your name} --upper,-u|Convert to uppercase", 
        (string name, bool upper) => WriteLine(upper ? name.ToUpper() : name))
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);