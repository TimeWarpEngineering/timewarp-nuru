# Generator: escape C# keyword identifiers in emitted code (event, when route params)

## Description

Found 2026-08-08 while fixing task 459: a route like
`schedule {event} {when:DateTime}` with handler `(string @event, DateTime when)`
is legal Nuru + legal C#, but NuruGenerator emits the parameter identifiers
UNESCAPED into generated interceptor code, producing:

- `error CS0065: 'GeneratedInterceptor.': event property must have both add and
  remove accessors` (the raw `event` token parsed as a keyword)
- `error CS0246: The type or namespace name 'when' could not be found`
- cascading CS0708/CS8802 in NuruGenerated.g.cs

Repro: restore `{event}`/`{when:DateTime}` param names in
`samples/fluent/08-type-converters/fluent-type-converters-builtin.cs`
(renamed to `{name}`/`{at:DateTime}` in the 459 fix to unblock CI) and build.

## Checklist

- [ ] Generator escapes identifiers that are C# keywords/contextual keywords (`@`-prefix) everywhere parameter names are emitted
- [ ] Analyzer or generator test covering keyword-named route params (event, when, class, ref, ...)
- [ ] Restore a keyword-named param somewhere in samples as a living regression check (or add a dedicated test)

## Notes

Filed from the 458 endgame session; surfaced when the 459 H005 fix exposed the
next failure layer in the same sample.
