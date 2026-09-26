# Delegate emitter emits await await for expression-bodied async lambdas

## Description

Found by 443-001 (2026-09-25): Nuru's delegate emitter produces `await await …` for an expression-bodied
async lambda whose body starts with `await`, for example `async (ISender s) => await s.Send(q)`. The
generated code does not compile, so 443-001's test used block bodies instead. With Nuru handlers now
injecting `ISender`, this shape is the natural one to write.

## Requirements

- The emitter produces valid code for expression-bodied async lambdas whose body is an `await` expression,
  including `ConfigureAwait`, parenthesized, and nested-await forms.
- Block-bodied and non-async lambdas are unchanged.
- Generator tests covering each shape, including `async (ISender s) => await s.Send(q)` end to end.

## Checklist

- [ ] Emitter fixed
- [ ] Generator tests for each shape
- [ ] End-to-end ISender lambda test

## Notes

- Implementer: **commit and push your changes before reporting done.**
- Run the build and test gate in the foreground.
