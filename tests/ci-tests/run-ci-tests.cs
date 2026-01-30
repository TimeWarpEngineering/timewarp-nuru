#!/usr/bin/dotnet --

// Multi-mode Test Runner
// Test classes are auto-registered via [ModuleInitializer] when compiled with JARIBU_MULTI.

WriteLine("TimeWarp.Nuru Multi-Mode Test Runner");
WriteLine();

return await RunAllTests();
