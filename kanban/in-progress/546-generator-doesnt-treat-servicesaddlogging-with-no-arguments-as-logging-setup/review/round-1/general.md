# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** branch `task/546-generator-doesnt-treat-servicesaddlogging-with-no` vs merge-base `a69e545a`. `ServiceExtractor` AddLogging detection, `EmitLoggingFactoryFields`, `LoggingConfiguration`, generator-68, and the CI standalone exclude.

## Summary

`ExtractLoggingFromLambdaBody` now resolves each invocation to an `IInvocationOperation` and keeps it only when `ReducedFrom ?? TargetMethod` is `LoggingServiceCollectionExtensions.AddLogging`. The configure delegate is the argument whose parameter type is `Action<ILoggingBuilder>`, so a static call (`AddLogging(services, lambda)`), a named `configure:` argument, and a parameterless call all select the right overload. A missing configure argument becomes `LoggingConfiguration` with an empty body, which is non-null, so `HasLogging` is true, NURU_H007 stays quiet, and `EmitLoggingFactoryFields` emits `LoggerFactory.Create(builder => { })` without a stray semicolon. Lambdas and a one-parameter anonymous method still copy the delegate body. Method groups and delegate variables return null, so NURU_H007 still fires. That matches the recorded choice: an empty factory would drop providers the generated class cannot see.

The implementer restatement matches the original description and checklist. Semantic matching, the parameterless overload, the runtime `ILogger<T>` proof, and the other call shapes are what the task asked for. No misread.

generator-68 is excluded from the multi-mode assembly the same way as generator-67 and is listed in `run-ci-tests.cs`. Re-executed the file-based build the SDK already produced for this source file: 8/8 passed. The parameterless test runs the compiled app and sees `Logger`1`, not a null logger. The lambda, static, named, and anonymous-method cases compile and contain the expected factory assignment. The method-group and no-`AddLogging` cases still report exactly one NURU_H007. `AddLogging` remains on the NURU052 whitelist by name, so the zero-argument call does not pick up a new extension-method warning.

## Issues

<!-- none -->
