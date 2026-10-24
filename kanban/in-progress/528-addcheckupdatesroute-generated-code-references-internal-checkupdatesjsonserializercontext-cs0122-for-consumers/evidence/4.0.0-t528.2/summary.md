# Nuru consumer check: TimeWarp.Nuru 4.0.0-t528.2

The source (code and its Purpose/Design regions) is the authority. Docs are checked separately.

- Source: `/tmp/t528-feed` (private cache, `<clear/>` nuget.config)
- Docs: `v4.0.0-t528.2` (all)
- Run: 20261024T162109Z, dotnet 10.0.402
- Release verdict (launch, cache, probes, cli): **FAIL**: 35 passed, 4 failed

## Release verdict

| Step | Check | Result | Evidence | Detail |
|------|-------|--------|----------|--------|
| launch | restore resolves TimeWarp.Nuru 4.0.0-t528.2 from the intended source | PASS | `launch/timewarp.nuru.nupkg.metadata` | /tmp/t528-feed |
| cache | global cache entries match the published packages | PASS | `cache/global-cache-audit.tsv` | 43 matched, 1 not cached |
| probes | use-console-logging compiles against the package | FAIL | `probes/use-console-logging.build-errors.txt` | NURU_S999: Unrecognized DSL method: UseConsoleLogging. |
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
| probes | add-check-updates-route: AddCheckUpdatesRoute | PASS | `probes/34-add-check-updates-route.log` |  |
| probes | use-console-logging: UseConsoleLogging | FAIL | `probes/use-console-logging.build-errors.txt` | probe did not compile |
