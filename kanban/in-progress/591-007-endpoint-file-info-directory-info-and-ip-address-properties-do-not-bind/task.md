# Endpoint FileInfo, DirectoryInfo and IPAddress properties do not bind

## Description

`GetTypeConstraintFromClrType` maps `Version` but not `FileInfo`, `DirectoryInfo`, or `IPAddress`, although
fluent `{x:FileInfo}` constraints convert them through `TypeConversionMap`. An endpoint property of one of those
types does not bind. (Review M5 on task 496.)

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
