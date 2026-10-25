# Nuru consumer check: TimeWarp.Nuru 4.0.0-t527.1

The source (code and its Purpose/Design regions) is the authority. Docs are checked separately.

- Source: `/tmp/t527-feed` (private cache, `<clear/>` nuget.config)
- Docs: `HEAD` (all)
- Run: 20261025T161719Z, dotnet 10.0.402
- Release verdict (launch, cache, probes, cli): **PASS**: 36 passed, 0 failed
- Docs out of date with source: **22** doc defects (report only, not part of the verdict)

## Release verdict

| Step | Check | Result | Evidence | Detail |
|------|-------|--------|----------|--------|
| launch | restore resolves TimeWarp.Nuru 4.0.0-t527.1 from the intended source | PASS | `launch/timewarp.nuru.nupkg.metadata` | /tmp/t527-feed |
| cache | global cache entries match the published packages | PASS | `cache/global-cache-audit.tsv` | 43 matched, 1 not cached |
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
| probes | add-logging: ConfigureServices AddLogging + ILogger<T> (Information logged) | PASS | `probes/33-add-logging.log` |  |
| probes | add-check-updates-route: AddCheckUpdatesRoute | PASS | `probes/34-add-check-updates-route.log` |  |

## Docs out of date with source (DOC-DEFECT, file:line)

A failing snippet is a doc defect unless the source public API says it should work. Then it is a code defect and the probe for that API shows it.

| Check | Result | Evidence | Detail |
|-------|--------|----------|--------|
| snippet documentation/user/features/mcp-hosting.md:17 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1061: 'NuruAppBuilder' does not contain a definition for 'AddMcpRoute' and no accessible extension method 'AddMcpRoute' accepting a first argument of type 'NuruAppBuilder' could be found (are you missing a using directive or an assembly reference?) |
| snippet documentation/user/features/mcp-hosting.md:50 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS0234: The type or namespace name 'McpHost' does not exist in the namespace 'TimeWarp.Nuru' (are you missing an assembly reference?) |
| snippet documentation/user/features/output-handling.md:32 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/features/output-handling.md:69 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/features/output-handling.md:168 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/features/output-handling.md:194 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/features/output-handling.md:222 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/features/output-handling.md:256 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/features/output-handling.md:284 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/features/output-handling.md:360 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/features/routing.md:130 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | NURU_R003: Route '' is unreachable. Route '{*args}' (defined at Program.cs:10) will match all the same inputs with equal or higher specificity (10 vs 0 points). |
| snippet documentation/user/features/served-apps.md:12 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1061: 'NuruAppBuilder' does not contain a definition for 'AddServeRoute' and no accessible extension method 'AddServeRoute' accepting a first argument of type 'NuruAppBuilder' could be found (are you missing a using directive or an assembly reference?) |
| snippet documentation/user/guides/best-practices.md:28 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1061: 'NuruAppBuilder' does not contain a definition for 'WithGroup' and no accessible extension method 'WithGroup' accepting a first argument of type 'NuruAppBuilder' could be found (are you missing a using directive or an assembly reference?) |
| snippet documentation/user/reference/supported-types.md:28 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | NURU_H005: Handler parameter 'msg' doesn't match any route segment; available segments: echo, {message} |
| snippet documentation/user/reference/supported-types.md:49 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | NURU_H005: Handler parameter 'sec' doesn't match any route segment; available segments: wait, {seconds} |
| snippet documentation/user/reference/supported-types.md:109 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | NURU_H005: Handler parameter 'dt' doesn't match any route segment; available segments: schedule, {when} |
| snippet documentation/user/use-cases.md:149 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/use-cases.md:193 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| snippet documentation/user/use-cases.md:241 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'Map' takes 2 arguments |
| readme.md:70: dotnet run -- add 15 25 prints '15 + 25 = 40' | PASS | `docs/s-readme-md-70.run.log` |  |
| snippet readme.md:178 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'CreateBuilder' takes 1 arguments |
| snippet readme.md:248 compiles against the package | DOC-DEFECT | `docs/snippets.tsv` | CS1501: No overload for method 'CreateBuilder' takes 1 arguments |
| doc snippets (HEAD) use only APIs the package has | DOC-DEFECT | `docs/snippets.tsv` | 21 broken, 39 compile, 76 fragments, 136 total |
