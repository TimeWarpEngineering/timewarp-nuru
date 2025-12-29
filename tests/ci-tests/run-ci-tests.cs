#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
// #:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj
// #:project ../../source/timewarp-nuru-completion/timewarp-nuru-completion.csproj
// #:project ../../source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

// Multi-mode Test Runner
// Test classes are auto-registered via [ModuleInitializer] when compiled with JARIBU_MULTI.

WriteLine("TimeWarp.Nuru Multi-Mode Test Runner");
WriteLine();

return await RunAllTests();
