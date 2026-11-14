# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** task commits `589eb507..2096c080`. `LambdaBodyQualifier`, the three `HandlerExtractor` body sites, generator-48 expectations, generator-74.

## Summary

`LambdaBodyQualifier` rewrites an inlined delegate body from the SemanticModel before `HandlerDefinition.LambdaBodySource` is stored. Types and namespaces become `global::` names, unqualified static members gain their containing type, and a reduced extension call `x.M(a)` becomes `global::N.T.M(x, a)`. Parenthesized lambdas, simple lambdas, and anonymous methods all go through that path. generator-74 builds and runs the accepted shapes in the multi-mode assembly: a namespace type in an expression body with and without a behavior, a block body (enum, nested record, `List<T>`, const, `typeof`, extension call, interpolation), a handler that takes a DI service, and an unqualified static call inside the enclosing class. generator-48's string expectations follow the same qualification (`Task` to `global::System.Threading.Tasks.Task`, global-namespace `Q` to `global::Q`) and still forbid a doubled `await`. The recorded `./bin/dev build`, `./bin/dev test` (generator-74 5/5 inside multi-mode), and `ganda repo audit` cover this diff. The restatement matches the groomed task: the method-group item stays with 552, and the AddBehavior symptom is the same unqualified-body failure without a behavior. No product defect.

## Issues
