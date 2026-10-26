# Apps in one compilation share the first app's logger factory

## Description

When one compilation builds several Nuru apps, every app's `ILogger<T>` comes from the first app's logger factory. `InterceptorEmitter.GetFirstLoggerFactoryFieldName` picks one factory field for all apps, so app B logs through app A's `AddLogging` configuration (providers and minimum level), or gets a logger even though B configured none.

Single-app consumers are unaffected. The CI multi-mode test assembly is affected, which is why task 527's `generator-66-add-logging-injected-logger.cs` had to run standalone. Found by task 527 (PR #356).

## Checklist

- [ ] Read the Purpose/Design regions of `InterceptorEmitter` and the per-app emit path
- [ ] Each app resolves `ILogger<T>` from its own logger factory field (keyed by app, like other per-app state)
- [ ] An app with no `AddLogging` gets no logger from another app (NullLogger behavior, NURU_H007 unchanged)
- [ ] Regression test: two apps in one compilation with different minimum levels or providers each log through their own configuration
- [ ] Logger fields are named by fully qualified type, not short class name, so `A.Foo` and `B.Foo` injecting `ILogger<T>` don't clash (noted by 545); regression test with two same-named classes in different namespaces
- [ ] Move generator-66 back into the multi-mode assembly if it now passes there, removing its exclude
- [ ] `ganda runfile cache --clear`, full CI suite green

## Notes

- 541, 546 and 545 (merged 2026-10-26) reworked this path: `LoggerExpressions`, parameterless `AddLogging()`, and service-constructor loggers in `EmitEnsureServicesInitialized`. Build on those; generator-67/68/69 run standalone and may also move back into multi-mode.
- Related: the multi-app codegen cost work (522, 524) and the multi-mode Jaribu/codegen design note follow-up.
- No trailing whitespace. Use `Map<T>()` in tests, not `DiscoverEndpoints()`.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- Scope added 2026-10-26 by cockpit 7dce871f (logger field name clash from 545)
