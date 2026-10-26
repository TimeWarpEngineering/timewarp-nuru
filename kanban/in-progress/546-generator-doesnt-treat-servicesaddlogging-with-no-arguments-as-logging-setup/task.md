# Generator doesn't treat services.AddLogging() with no arguments as logging setup

## Description

The generator does not recognize `services.AddLogging()` called with no arguments as logging setup. Handlers then get a NullLogger, and NURU_H007 warns that logging isn't configured, even though the app called `AddLogging`. Only `AddLogging(logging => ...)` is detected.

`AddLogging()` with no arguments is valid Microsoft.Extensions.Logging API: it registers `ILoggerFactory` and `ILogger<T>` with no providers. The generator should treat it as logging configured. Logs then go nowhere because no providers are registered, but that is the documented behavior of the M.E.L API, not a Nuru bug.

Found by the 541 implementer (fix pass 2). It predates 541; generator-67 uses `AddLogging(logging => ...)` to avoid it.

## Checklist

- [ ] Find the AddLogging detection in the generator and accept the zero-argument overload, using the semantic model (method symbol), not syntax text
- [ ] Test: `ConfigureServices(s => s.AddLogging())` with an `ILogger<T>` delegate parameter gets a real logger from the app's factory, and NURU_H007 does not fire
- [ ] Check for other overload shapes the detection misses

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
