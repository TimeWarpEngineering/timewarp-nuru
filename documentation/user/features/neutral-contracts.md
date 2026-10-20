# Neutral contracts

Nuru treats a TimeWarp.Contracts request as a complete endpoint. The request carries `[Operation]` and a TimeWarp.Mediator kind (`IQuery<T>`, `ICommand<T>`, or `IIdempotentCommand<T>`). An `[Operation]` type with no mediator kind is not an endpoint. Classes and record classes both qualify. Nuru does not ship a second attributes package. `[NuruRoute]`, `[Parameter]`, `[Option]`, and `[NuruRouteExample]` stay in `TimeWarp.Nuru` and override the derived CLI when you need a positional parameter, an alias, a different spelling, or an example.

`TimeWarp.Nuru` references `TimeWarp.Contracts`. The generator and analyzers resolve `[Operation]` and the CLI attributes by metadata name.

A contracts-first calculator lives next to the attribute calculator at `samples/endpoints/02-calculator-contracts/`.

## What the generator derives

The route literal is the operation name. A name with a space is invalid; put the extra words in a group. The description is `[Operation(Description = "...")]`.

Each public settable property is an option, including an `init` property and one inherited from a base type. A base marked `[NuruRouteGroup]` is a group, not a source of options. The option name is the property identifier as declared, so `--capabilities` `name` and the `--json-args` key are that same identifier. A camelCase property such as `solutionId` is the option `--solutionId`, and the generated code sets `solutionId`.

Nuru builds the request with `new()` when an accessible constructor takes no arguments, including one whose parameters are all optional. Otherwise it calls the accessible constructor whose parameters all match bound properties by name, ignoring case. A positional record uses its primary constructor this way, and a parameter default (`int Count = 5`) is that option's default. A property initializer that only copies a parameter (`= Name`) takes that parameter's default. Any other initializer that reads a parameter (`= Name.Trim()`) gives no default, so a non-nullable member is required. A `required` member is always set in the object initializer. When no constructor fits, or the type is abstract or static, the generator reports NURU_H009 as an error and skips the type.

| Member | CLI |
| --- | --- |
| `string?`, or a non-nullable member with an initializer | Optional option. Omission keeps the initializer. An initializer the generator cannot copy (`new()`, a method call) stays on the property, and `--capabilities` omits it. `--capabilities` also omits `= null!` and a non-constant field or property such as a `static readonly` value. Numeric defaults are plain numbers. An enum default is the member name, and a `[Flags]` combination is the member names joined with `, `, or the number when the bits match no members. |
| `required`, or a non-nullable member with no initializer | Required option. |
| `System.ComponentModel.Description` | Option description. |
| Enum | Allowed values are the enum names. A non-nullable enum with an initializer keeps that value when omitted. A non-nullable enum with no initializer is required. |
| Array or list of a scalar (`string[]`, `int[]`, and the same for other scalars) | Repeated option. Pass the option once per value. |
| Anything else, including an array of objects | One JSON value. `--capabilities` reports the type as `json` plus the CLR type name, without a `global::` prefix, and a `schema`: the JSON Schema of that type, as the binder deserializes it. An MCP tool publishes that schema. A required JSON member cannot be passed on argv; the call fails and tells you to use `--json-args`. |

Scalars are primitives, `string`, `decimal`, the date and time types, `Guid`, `Uri`, `Version`, `BigInteger`, enums, and nullable forms of those.

Argv cannot supply a JSON member. Passing `--Notes ...` is an error that tells you to use `--json-args`. The JSON key is the property identifier:

```bash
calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'
```

Names inside that JSON value are the property identifiers as declared, matched ignoring case, so `{"label":...}` and `{"Label":...}` both bind `Label`. The top-level `--json-args` keys still match exactly. Nuru does not rewrite names to camelCase. The deserializer is source-generated, so a published AOT app does not use reflection. For an enum inside a JSON value, use `[JsonConverter(typeof(JsonStringEnumConverter<TEnum>))]`. The non-generic `JsonStringEnumConverter` needs runtime code generation, and the generated context reports SYSLIB1034 for it.

## Contracts in a referenced assembly

The generator finds endpoint types in the compiling project's source. A CLI can also take its endpoints from a referenced contracts assembly, so it does not need the request files linked in as sources. Referencing an assembly adds no commands on its own. Opt in one of two ways:

```csharp
using Calculator.Contracts;
using TimeWarp.Nuru;

// Every public [Operation] and [NuruRoute] type in AddQuery's assembly joins discovery,
// exactly like a type declared in this project.
[assembly: NuruEndpointAssembly(typeof(AddQuery))]

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();
```

- `[assembly: NuruEndpointAssembly(typeof(AnyTypeInTheAssembly))]` adds every endpoint type in that assembly to the discovery pool. `DiscoverEndpoints()`, `DiscoverEndpoints(typeof(SomeGroup))`, and `Map<T>()` then see them. Repeat the attribute for several assemblies.
- `Map<T>()` on an endpoint type from a referenced assembly adds that one endpoint, without the attribute. It belongs to the app that maps it: another app's `DiscoverEndpoints()` in the same project does not pick it up.

A referenced type follows the same rules as a source type, with these differences:

- **Visibility.** The type must be a public class or record class that this project can name. An internal type counts only through `[InternalsVisibleTo]`. A nested `Handler` that this project cannot see, such as an `internal` one, is ignored; the endpoint then needs a registered handler, and NURU_H008 applies.
- **Defaults.** An assembly has no property initializer syntax, so Nuru cannot read `= "Hello"`. An omitted option leaves the member out of the object initializer, and the initializer compiled into the contract runs. `--capabilities` shows a default only when the member has `[System.ComponentModel.DefaultValue(...)]`. Keep that attribute in step with the initializer; the initializer is what runs. A `bool` with `[DefaultValue(true)]` is a value option, not a flag. A positional record's parameter default is in metadata and works as it does in source.
- **Required members.** Mark a member the caller must supply `required`. A non-nullable member without `required` is optional, because metadata cannot show whether it has an initializer. Nullability is read from metadata as usual.
- **Handlers.** With a generated mediator, the CLI dispatches through TimeWarp.Mediator, which registers handlers only from mediator graph members. Put `[assembly: TimeWarp.Mediator.MediatorAssembly]` in the contracts assembly, or dispatch throws `NoHandlerException`. A public nested `Handler` is also called directly under `UseDirectInvoke()` or when no generated mediator exists.
- **JSON.** The `TimeWarp.Nuru.Build` task registers the response and JSON member types of referenced endpoints in the generated JSON context, using the same two opt-ins.

The sample `samples/endpoints/17-referenced-contracts/` is a CLI whose endpoints all live in `contracts/`.

## Tool hints

A contract says how a harness should treat its operation with attributes on the request class. The MCP host turns them into tool annotations, and `--capabilities` carries them for CLI agents:

| Attribute | Catalog | MCP |
| --- | --- | --- |
| The mediator kind (`IQuery`, `IIdempotentCommand`, `ICommand`) | `kind` | `readOnlyHint`, `idempotentHint` |
| `[Destructive]`, or `[Destructive(false)]` for a command that only adds | `destructive` | `destructiveHint` |
| `[OpenWorld]`, or `[OpenWorld(false)]` for an operation that works only on local state | `openWorld` | `openWorldHint` |
| `[System.ComponentModel.DisplayName("...")]` | `title` | `title` |

```csharp
using System.ComponentModel;
using TimeWarp.Contracts;
using TimeWarp.Nuru;

[Operation("rename_symbol", Description = "Rename a symbol across the solution")]
[DisplayName("Rename symbol")]
[Destructive(false)]
[OpenWorld(false)]
public sealed class RenameSymbolCommand : ICommand<string> { ... }
```

`DisplayName` is the BCL attribute, like the `Description` on members. `Destructive` and `OpenWorld` are in `TimeWarp.Nuru`, because `TimeWarp.Contracts` 2.0.0-beta.23 has no place for them: `McpOperationBinding` carries `ToolName` only. When TimeWarp.Contracts gains these hints, Nuru will read them too. An app that serves contracts it does not own overrides any hint per tool with `NuruMcpHostOptions.ToolAnnotations` (see [MCP hosting](mcp-hosting.md#annotations)).

## Bool defaults

A Nuru flag starts at `false`, so a non-nullable `bool` whose initializer is `true` is not a flag. Omission would flip the default. Nuru emits that member as a value option. Leave it out and the initializer stands. Pass `--IncludeAverage true` or `--IncludeAverage false` to override it. `--capabilities` shows the default.

A non-nullable `bool` with no initializer is required. Nuru does not invent `false`. A non-nullable `bool` initialized to `false`, and a `bool` that is optional for another reason, stays a flag.

## Groups

`--group-filter` matches the group path. Derived endpoints get that path in this order:

1. `[Operation("get_symbol", Group = "symbols")]` with a non-blank `Group`. The group is metadata: it becomes `groupPath`, split on `.` or space (`"symbols.read"` and `"symbols read"` are both `["symbols", "read"]`). It is not a route prefix or a command prefix, so the command stays `get_symbol` and so does the MCP tool name. No `[NuruRouteGroup]` prefix or `[NuruCliBinding(Group)]` is added on top of it.
2. Otherwise a containing `[NuruRouteGroup]`, including nested groups. This is the same prefix attribute-declared routes use, and it is part of the command.
3. Otherwise `[NuruCliBinding(Group = "segment")]`. `Group` is one prefix. Spaces separate literals, the same way one `[NuruRouteGroup]` prefix does. `TimeWarp.Contracts.CliOperationBinding` has `Pattern` only, so this group lives on the Nuru attribute.
4. Otherwise the endpoint is ungrouped and `groupPath` is empty.

A blank `[Operation(Group)]` is ungrouped, so steps 2 to 4 apply. A `[NuruRouteGroup]` wins over `[NuruCliBinding]` when both are present. Only `[NuruRouteGroup]` adds a group type, so `DiscoverEndpoints(typeof(SomeGroup))` follows `[NuruRouteGroup]` only, including for an operation whose `Group` replaced the prefix. MCP tool filtering (`groupFilter`) reads this same group path.

## Handlers

A nested `Handler` is a convenience, not a requirement. The endpoint handler is any registered `IQueryHandler<TQuery, T>`, `ICommandHandler<TCommand, T>`, or `IIdempotentCommandHandler<TCommand, T>` for that request. A CLI edition can register a forwarding handler and a daemon the real one for the same request type.

When the compilation can see no handler, the generator reports NURU_H008 as a warning. The build is accepted. Invoking that route resolves the handler and returns exit code 1 when it is still unregistered. `--help` and `--capabilities` still run. A project that treats warnings as errors must suppress NURU_H008 for a handler this compilation cannot see.

## Dispatch and rendering

When this compilation has a generated mediator, endpoints dispatch through TimeWarp.Mediator, so mediator behaviors run. `INuruBehavior` stays CLI middleware around that dispatch.

No generated mediator exists when `TimeWarpMediatorAssembly` is not true, or when the mediator profile is `Aot` or `Link`. A nested `Handler` is then called directly, and FluentValidation behaviors do not run. With source-generated DI, a visible handler that is not nested writes an error and exits 1 instead of throwing. With `UseMicrosoftDependencyInjection()`, Nuru resolves `IMediator` from the container instead, and that call throws unless the app registers a mediator.

A nested `Handler` is also called directly when `UseDirectInvoke()` is set and a generated mediator does exist. An endpoint with no nested handler still goes through the mediator, including when `UseDirectInvoke()` is set. `UseDirectInvoke()` is the opt-out when the mediator cost matters for an AOT CLI.

A response is rendered in this order:

1. A registered `IResponseRenderer<TResponse>`, from source-generated services or from runtime DI.
2. Otherwise the one visible `IResponseRenderer<TResponse>` with a public parameterless constructor, when the compilation can see exactly one. The generator constructs it.
3. The existing output strategy: raw text, invariant or ISO formatting, or JSON.

`OneOf<TSuccess, SharedProblemDetails>` writes the problem to stderr and returns a non-zero exit code. The first line is the problem type and title. The detail is the next line when it is present. The exit code is `Status` when that value is from 1 to 255, and 1 otherwise. The success value uses the renderer or the output strategy above.

An `IResponseRenderer<SharedProblemDetails>` replaces those default problem lines. Nuru finds it the same way as a success renderer: registered first, then the one visible renderer with a public parameterless constructor. Its text goes to stderr, and the exit code is still `Status` as above. A visible problem renderer applies to every problem pair in the compilation. To scope one to a single app, give it a constructor parameter and register it in that app's `ConfigureServices`.

By default the CLI adds a newline after renderer text. `UseVerbatimRendererOutput()` writes the text exactly as the renderer returns it, so a CLI and an MCP host that share a renderer write the same bytes:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .UseVerbatimRendererOutput()
  .DiscoverEndpoints()
  .Build();
```

It applies to every renderer in the app: success renderers on stdout, and a problem renderer on stderr. It covers argv, `--json-args`, and `RenderAsync`. Output with no renderer (raw text, ISO 8601, JSON, and the default problem lines) still ends with a newline. Only the success type is registered for JSON, and none is registered when a renderer is found. When two types would get the same generated JSON name, such as two nested `Response` classes, each gets a name from its full type name.

The problem renderer also renders failures that happen outside a handler's result. `NuruProblems` maps them to `SharedProblemDetails`. A bind failure is `Type` `Invalid` with `Status` 400. `Detail` is the bind message without its `Error: ` prefix, and the extension members `kind` (the `NuruBindErrorKind`) and `key` (the argument name) are set when they apply. An escaped handler exception is `Type` `Faulted` with `Status` 500 and the exception message as `Detail`. An MCP host and a served app's server always render these with the problem renderer when the app has one (see [MCP hosting](mcp-hosting.md#errors-through-the-problem-renderer)). The CLI does so only when the app opts in:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .UseProblemRendererForErrors()
  .DiscoverEndpoints()
  .Build();
```

With the opt-in and a problem renderer, a `--json-args` error (a bind failure, or a value that is not a JSON object), an argv value that does not convert, such as `abc` for an `int` parameter, and a JSON member passed on argv or left out are rendered as an `Invalid` problem on stderr. A missing value has `kind` `MissingValue`. The exit code does not change. Without the opt-in, or with no problem renderer, the CLI writes its plain `Error: ...` text as before. Argv conversion errors stay on stdout in that case. `NuruApp.TryRenderProblem` renders a problem with the app's renderer from your own code.

## Capabilities

`--capabilities` uses one `OptionCapability` shape for derived and attribute-declared options. A derived endpoint has the same fields: `name`, `type`, `required`, `isFlag`, `isRepeated`, and, when they apply, `description`, `defaultValue`, and `allowedValues`. A JSON member's `type` is `json` plus the CLR type name, and its `schema` is the JSON Schema of that type. `groupPath` is the group decision above. The tool hints above add `destructive`, `openWorld` and `title` to the endpoint.

`[NuruRoute]` still overrides the operation name and description. `[Parameter]` and `[Option]` still override a single member, including a lowercase long form or a positional parameter.
