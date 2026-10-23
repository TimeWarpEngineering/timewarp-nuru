# Nuru consumer check: TimeWarp.Nuru 4.0.0-beta.5

- Source: `https://api.nuget.org/v3/index.json` (private cache, `<clear/>` nuget.config)
- Docs: `v4.0.0-beta.5` (all)
- Run: 20261023T143255Z, dotnet 10.0.402
- Result: **FAIL**: 38 passed, 29 failed, 0 skipped

| Step | Check | Result | Evidence | Detail |
|------|-------|--------|----------|--------|
| launch | restore resolves TimeWarp.Nuru 4.0.0-beta.5 from the intended source | PASS | `launch/timewarp.nuru.nupkg.metadata` | https://api.nuget.org/v3/index.json |
| cache | global cache entries match the published packages | PASS | `cache/global-cache-audit.tsv` | 44 matched, 0 not cached |
| probes | use-logging compiles against the package | FAIL | `probes/use-logging.build-errors.txt` | NURU_S999: Unrecognized DSL method: UseLogging. |
| probes | core-routing: Map/WithHandler/AsQuery | PASS | `probes/01-core-routing.log` |  |
| probes | core-routing: optional parameter and option | PASS | `probes/02-core-routing.log` |  |
| probes | core-routing: optional parameter and option | PASS | `probes/03-core-routing.log` |  |
| probes | core-routing: AsIdempotentCommand | PASS | `probes/04-core-routing.log` |  |
| probes | core-routing: catch-all parameter | PASS | `probes/05-core-routing.log` |  |
| probes | core-routing: exit code from handler | PASS | `probes/06-core-routing.log` |  |
| probes | core-routing: unknown command | PASS | `probes/07-core-routing.log` |  |
| probes | core-routing: WithDescription/--help | PASS | `probes/08-core-routing.log` |  |
| probes | core-routing: route description in --help | PASS | `probes/09-core-routing.log` |  |
| probes | core-routing: --version | PASS | `probes/10-core-routing.log` |  |
| probes | core-routing: --capabilities | PASS | `probes/11-core-routing.log` |  |
| probes | core-routing: route kind in --capabilities | PASS | `probes/12-core-routing.log` |  |
| probes | group-prefix: WithGroupPrefix | PASS | `probes/13-group-prefix.log` |  |
| probes | group-prefix: nested WithGroupPrefix | PASS | `probes/14-group-prefix.log` |  |
| probes | configure-services: ConfigureServices | PASS | `probes/15-configure-services.log` |  |
| probes | add-configuration: AddConfiguration | PASS | `probes/16-add-configuration.log` |  |
| probes | add-behavior: AddBehavior (before) | PASS | `probes/17-add-behavior.log` |  |
| probes | add-behavior: AddBehavior (after) | PASS | `probes/18-add-behavior.log` |  |
| probes | type-converter: AddTypeConverter | PASS | `probes/19-type-converter.log` |  |
| probes | enable-completion: EnableCompletion script | PASS | `probes/20-enable-completion.log` |  |
| probes | enable-completion: EnableCompletion callback | PASS | `probes/21-enable-completion.log` |  |
| probes | help-hide-rows: ConfigureHelp ShowPerCommandHelpRoutes=false | PASS | `probes/22-help-hide-rows.log` |  |
| probes | help-hide-rows: ConfigureHelp ShowCompletionRoutes=false | PASS | `probes/23-help-hide-rows.log` |  |
| probes | help-show-rows: ConfigureHelp ShowPerCommandHelpRoutes=true | PASS | `probes/24-help-show-rows.log` |  |
| probes | help-show-rows: ConfigureHelp ShowCompletionRoutes=true | PASS | `probes/25-help-show-rows.log` |  |
| probes | help-exclude: ConfigureHelp ExcludePatterns | PASS | `probes/26-help-exclude.log` |  |
| probes | endpoint-dsl: Endpoint DSL (README) | PASS | `probes/27-endpoint-dsl.log` |  |
| probes | discover-endpoints: DiscoverEndpoints (first endpoint) | PASS | `probes/28-discover-endpoints.log` |  |
| probes | discover-endpoints: DiscoverEndpoints (second endpoint) | PASS | `probes/29-discover-endpoints.log` |  |
| probes | repl: AddRepl | PASS | `probes/30-repl.log` |  |
| probes | repl: AddRepl options (prompt, welcome, goodbye) | PASS | `probes/31-repl.log` |  |
| probes | telemetry: UseTelemetry options | PASS | `probes/32-telemetry.log` |  |
| probes | use-logging: UseLogging | FAIL | `probes/use-logging.build-errors.txt` | probe did not compile |
| cli | dotnet tool install TimeWarp.Nuru.Search 4.0.0-beta.5 | PASS | `cli/install.log` |  |
| cli | nuru --version reports 4.0.0-beta.5 | PASS | `cli/version.log` |  |
| cli | nuru indexes and finds a route in an app built from the package | PASS | `cli/search.log` |  |
| docs | snippet documentation/user/features/built-in-routes.md#L157 compiles against the package | FAIL | `docs/snippets.tsv` | CS0122: 'CheckUpdatesJsonSerializerContext' is inaccessible due to its protection level |
| docs | snippet documentation/user/features/logging.md#L10 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_S999: Unrecognized DSL method: UseConsoleLogging. |
| docs | snippet documentation/user/features/logging.md#L84 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_S999: Unrecognized DSL method: UseLogging. |
| docs | snippet documentation/user/features/logging.md#L104 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_S999: Unrecognized DSL method: UseLogging. |
| docs | snippet documentation/user/features/logging.md#L123 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_S999: Unrecognized DSL method: UseLogging. |
| docs | snippet documentation/user/features/logging.md#L217 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_S999: Unrecognized DSL method: UseLogging. |
| docs | snippet documentation/user/features/mcp-hosting.md#L50 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_S999: Unrecognized DSL method: AddMcpServer. |
| docs | snippet documentation/user/features/output-handling.md#L32 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/features/output-handling.md#L69 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/features/output-handling.md#L121 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_S999: Unrecognized DSL method: UseConsoleLogging. |
| docs | snippet documentation/user/features/output-handling.md#L155 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/features/output-handling.md#L181 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/features/output-handling.md#L209 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/features/output-handling.md#L243 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/features/output-handling.md#L271 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/features/output-handling.md#L347 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/features/routing.md#L130 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_R003: Route '' is unreachable. Route '{*args}' (defined at Program.cs:10) will match all the same inputs with equal or higher specificity (10 vs 0 points). |
| docs | snippet documentation/user/guides/best-practices.md#L28 compiles against the package | FAIL | `docs/snippets.tsv` | CS1061: 'NuruAppBuilder' does not contain a definition for 'WithGroup' and no accessible extension method 'WithGroup' accepting a first argument of type 'NuruAppBuilder' could be found (are you missing a using directive or an assembly reference?) |
| docs | snippet documentation/user/reference/supported-types.md#L28 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_H005: Handler parameter 'msg' doesn't match any route segment; available segments: echo, {message} |
| docs | snippet documentation/user/reference/supported-types.md#L49 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_H005: Handler parameter 'sec' doesn't match any route segment; available segments: wait, {seconds} |
| docs | snippet documentation/user/reference/supported-types.md#L109 compiles against the package | FAIL | `docs/snippets.tsv` | NURU_H005: Handler parameter 'dt' doesn't match any route segment; available segments: schedule, {when} |
| docs | snippet documentation/user/use-cases.md#L149 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/use-cases.md#L193 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | snippet documentation/user/use-cases.md#L241 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| docs | readme.md#L68: dotnet run -- add 15 25 prints '15 + 25 = 40' | PASS | `docs/s-readme-md-68.run.log` |  |
| docs | snippet readme.md#L176 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'CreateBuilder' takes 1 arguments |
| docs | snippet readme.md#L246 compiles against the package | FAIL | `docs/snippets.tsv` | CS1501: No overload for method 'CreateBuilder' takes 1 arguments |
| docs | doc snippets (v4.0.0-beta.5) use only APIs the package has | FAIL | `docs/snippets.tsv` | 26 broken, 38 compile, 74 fragments, 138 total |
