---
title: "Migration Guide — 2.0"
---

# Migration Guide — 2.0

This guide summarizes the 2.0 API changes that may require code updates.

Most applications are expected to need little or no migration work.
The main updates are low-level API renames and metadata initialization changes.

## Breaking Renames

{.table}
| 1.x | 2.0 |
|---|---|
| `OptionException` | `CommandOptionException` |
| `CommandNode.IsThisNodeActive` | `CommandNode.ActivePredicate` |
| `Command.OptionsName` | `Command.OptionsSectionName` |

## Output Rendering API (New in 2.0)

The pluggable output system (`ICommandOutput`, `CommandConfig.OutputFactory`, markup/visual outputs) was introduced in 2.0.

This is additive for migration purposes: there is no 1.x output API to migrate from, so no breaking change is required unless you choose to adopt these new extensibility features.

## Metadata Mutability Changes

These members now use construction-time initialization:

- `Command.Hidden` (`init`)
- `Command.OptionsSectionName` (`init`)
- `Option.EnvironmentVariable` (`init`)
- `Option.EnvironmentVariableDelimiter` (`init`)

### Before

```csharp
var hidden = new Command("secret");
hidden.Hidden = true;

app.Options["name"].EnvironmentVariable = "APP_NAME";
```

### After

```csharp
var hidden = new Command("secret")
{
    Hidden = true
};

app.Add("n|name=", "Name", value => { }, envVar: "APP_NAME");
```

## New Clarity Helpers

The following helpers were added to reduce overload ambiguity and improve intent:

- `AddRemainder(string? description = null)`
- `AddText(string text)`
- `AddSection(string header)` (auto-appends `:` when missing)

## Overload Parity Additions

Typed and list families now support `hidden` parity:

- `Add<T>(..., Action<T> action, bool hidden)`
- `Add<T>(..., ICollection<T> list, bool hidden)`
- Validation and env-var variants with `hidden` support are available for typed/list overloads.

## Notes

- Key/value (`Action<TKey, TValue>`) overloads intentionally still do **not** support env-var fallback or validation delegates.
- Command graphs remain single-invocation-at-a-time; avoid concurrent `RunAsync`/`Parse` on the same graph instance.

