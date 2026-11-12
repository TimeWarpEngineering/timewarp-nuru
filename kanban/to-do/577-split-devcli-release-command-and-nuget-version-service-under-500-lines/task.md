# Split DevCli release-command and nuget-version-service under 500 lines

## Description

TimeWarp.Nuru.DevCli 4.0.0-beta.12 compiles two content files into every consumer's dev-cli project:

- `release-command.cs` is 609 lines
- `nuget-version-service.cs` is 704 lines

TWA0038 (TimeWarp.SourceGenerators) flags a file over 500 lines. Consumers do not own these files, so they cannot split them. timewarp-ganda task 617 scopes `TWA0038.max_lines = 800` to the dev-cli compilation only, and cites this task. That relaxation should come out once these files are under 500 lines.

## Checklist

- [ ] Split `release-command.cs` so each compiled file is at most 500 lines
- [ ] Split `nuget-version-service.cs` so each compiled file is at most 500 lines
- [ ] A consumer dev-cli build with the default TWA0038 limit of 500 lines does not warn on this package content

## Acceptance criteria

- Neither shipped content file, nor a file it is split into, is longer than 500 lines
- The package still exposes the same dev-cli commands

## Session

- Created: 3200630 (2026-11-12)
- Filed from timewarp-ganda task 617 (package-pin-currency). The consumer workaround is a dev-cli-only `TWA0038.max_lines = 800`, not a repo-wide setting.

## Notes

Do not fix this inside timewarp-ganda. The source lives in this package.
