# Error handling: parse errors on stdout, unhandled handler exceptions abort, Ctrl+C does not cancel

## Description

Three defects in how a CLI run ends when something goes wrong.

1. **Some parse errors go to stdout** (for example
   `Error: Invalid value 'bogus' for option '--databasetype'`). Errors belong on stderr.
2. **Unhandled handler exceptions abort the process** (SIGABRT, exit 134, raw stack trace). Consider a default
   handler that prints the message and returns 1.
3. **Ctrl+C does not cancel the handler's `CancellationToken`** in a CLI run (`RunAsync` passes `default`).
   System.CommandLine cancels the token and waits `ProcessTerminationTimeout`.

## Checklist

- [ ] Decide per item: fix it, or keep the behavior and say why in Results
- [ ] Fix with tests for each accepted item
- [ ] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [ ] `ganda runfile cache --clear`, then CI tests green

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.
