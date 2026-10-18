# Neutral contracts

Nuru treats a TimeWarp.Contracts request as a complete endpoint. The request carries `[Operation]` and a TimeWarp.Mediator kind (`IQuery<T>`, `ICommand<T>`, or `IIdempotentCommand<T>`). Nuru does not ship a second attributes package. `[NuruRoute]`, `[Parameter]`, `[Option]`, and `[NuruRouteExample]` stay in `TimeWarp.Nuru` and override the derived CLI when you need a positional parameter, an alias, a different spelling, or an example.

`TimeWarp.Nuru` references `TimeWarp.Contracts`. The generator and analyzers resolve `[Operation]` and the CLI attributes by metadata name.

A contracts-first calculator lives next to the attribute calculator at `samples/endpoints/02-calculator-contracts/`.

## What the generator derives

The route literal is the operation name. A name with a space is invalid; put the extra words in a group. The description is `[Operation(Description = "...")]`.

Each public settable property is an option. The option name is the property identifier as declared, so `--capabilities` `name` and the `--json-args` key are that same identifier.

| Member | CLI |
| --- | --- |
| `string?`, or a non-nullable member with an initializer | Optional option. Omission keeps the initializer. |
| `required`, or a non-nullable member with no initializer | Required option. |
| `System.ComponentModel.Description` | Option description. |
| Enum | Allowed values are the enum names. A non-nullable enum with an initializer keeps that value when omitted. A non-nullable enum with no initializer is required. |
| Array or list of a scalar (`string[]`, `int[]`, and the same for other scalars) | Repeated option. Pass the option once per value. |
| Anything else, including an array of objects | One JSON value. `--capabilities` reports the type as `json` plus the CLR type name. |

Scalars are primitives, `string`, `decimal`, the date and time types, `Guid`, `Uri`, `Version`, `BigInteger`, enums, and nullable forms of those.

Argv cannot supply a JSON member. Passing `--Notes ...` is an error that tells you to use `--json-args`. The JSON key is the property identifier:

```bash
calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'
```

Names in that object are the property identifiers as declared. Nuru matches them case-sensitively and does not rewrite them to camelCase. The deserializer is source-generated, so a published AOT app does not use reflection.

## Bool defaults

A Nuru flag starts at `false`, so a non-nullable `bool` whose initializer is `true` is not a flag. Omission would flip the default. Nuru emits that member as a value option. Leave it out and the initializer stands. Pass `--IncludeAverage true` or `--IncludeAverage false` to override it. `--capabilities` shows the default.

A non-nullable `bool` with no initializer is required. Nuru does not invent `false`. A non-nullable `bool` initialized to `false`, and a `bool` that is optional for another reason, stays a flag.

## Groups

`--group-filter` matches the group path. Derived endpoints get that path in this order:

1. A containing `[NuruRouteGroup]`, including nested groups. This is the same prefix attribute-declared routes use.
2. Otherwise `[NuruCliBinding(Group = "segment")]`. `Group` is one prefix. Spaces separate literals, the same way one `[NuruRouteGroup]` prefix does. `TimeWarp.Contracts.CliOperationBinding` has `Pattern` only, so the group lives on this Nuru attribute.
3. Otherwise the endpoint is ungrouped and `groupPath` is empty.

A `[NuruRouteGroup]` wins when both are present. `[NuruCliBinding]` does not add a group type, so `DiscoverEndpoints(typeof(SomeGroup))` still follows `[NuruRouteGroup]` only. Task 508-004 reads this same group path for MCP tool filtering.

## Handlers

A nested `Handler` is a convenience, not a requirement. The endpoint handler is any registered `IQueryHandler<TQuery, T>`, `ICommandHandler<TCommand, T>`, or `IIdempotentCommandHandler<TCommand, T>` for that request. A CLI edition can register a forwarding handler and a daemon the real one for the same request type.

When the compilation can see no handler, the generator reports NURU_H008. A handler in another assembly that is not visible here is still accepted at build time. Startup then resolves the handler service and returns exit code 1 when it is missing.

## Dispatch and rendering

Endpoints dispatch through TimeWarp.Mediator, so mediator behaviors run the same way in every host. `INuruBehavior` stays CLI middleware around that dispatch.

`UseDirectInvoke()` skips the mediator and calls a nested `Handler` directly. Use it when the mediator cost matters for an AOT CLI. An endpoint with no nested handler still goes through the mediator.

A response is rendered in this order:

1. The visible `IResponseRenderer<TResponse>` with a public parameterless constructor, when the compilation can see exactly one. The generator constructs it. More than one visible renderer leaves the choice to the next step.
2. Under runtime DI, a registered `IResponseRenderer<TResponse>`.
3. The existing output strategy: raw text, invariant or ISO formatting, or JSON.

`OneOf<TSuccess, SharedProblemDetails>` writes the problem to stderr and returns a non-zero exit code. The first line is the problem type and title. The detail is the next line when it is present. The exit code is `Status` when that value is from 1 to 255, and 1 otherwise. The success value uses the renderer or the output strategy above.

## Capabilities

`--capabilities` uses one `OptionCapability` shape for derived and attribute-declared options. A derived endpoint has the same fields: `name`, `type`, `required`, `isFlag`, `isRepeated`, and, when they apply, `description`, `defaultValue`, and `allowedValues`. A JSON member's `type` is `json` plus the CLR type name. `groupPath` is the group decision above.

`[NuruRoute]` still overrides the operation name and description. `[Parameter]` and `[Option]` still override a single member, including a lowercase long form or a positional parameter.
