---
title: "Validation & Constraints"
---

# Validation & Constraints

XenoAtom.CommandLine provides built-in support for validating option and argument values at parse time, as well as declaring relationships between options.

## Value Validation

Add a `validate` parameter when declaring an option or argument to validate parsed values immediately:

```csharp
int port = 0;
string? email = null;
string? input = null;

var app = new CommandApp("myexe")
{
    { "p|port=", "Server {PORT}", (int v) => port = v, validate: Validate.Range(1, 65535) },
    { "e|email=", "Contact {EMAIL}", v => email = v,
        validate: Validate.That<string>(v => v.Contains('@'), "The value must be a valid email address.") },
    { "<input>", "Input {FILE}", v => input = v, validate: Validate.FileExists() },
    (ctx, _) => ValueTask.FromResult(0)
};
```

If validation fails, a clear error message is shown:

```
myexe: Invalid value for option `--port`: The value must be between 1 and 65535.
Use `myexe --help` for usage.
```

## Built-in Validators

XenoAtom.CommandLine includes a comprehensive set of validators in the `Validate` class:

### Numeric Validators

{.table}
| Validator | Description |
|---|---|
| `Validate.Range<T>(min, max)` | Value must be within the inclusive range `[min, max]` |
| `Validate.Positive<T>()` | Value must be greater than zero |
| `Validate.NonNegative<T>()` | Value must be greater than or equal to zero |

```csharp
{ "p|port=", "Port", (int v) => port = v, validate: Validate.Range(1, 65535) }
{ "t|threads=", "Threads", (int v) => threads = v, validate: Validate.Positive<int>() }
{ "r|retries=", "Retries", (int v) => retries = v, validate: Validate.NonNegative<int>() }
```

### String Validators

{.table}
| Validator | Description |
|---|---|
| `Validate.NonEmpty()` | Value must not be null or empty |
| `Validate.Matches(pattern)` | Value must match a regex pattern |
| `Validate.Matches(regex)` | Value must match a compiled `Regex` |
| `Validate.OneOf<T>(params T[])` | Value must be one of the specified values |

```csharp
{ "n|name=", "Name", v => name = v, validate: Validate.NonEmpty() }
{ "e|email=", "Email", v => email = v, validate: Validate.Matches(@"^[^@]+@[^@]+$", "Must be a valid email.") }
{ "l|level=", "Level", v => level = v, validate: Validate.OneOf("debug", "info", "warn", "error") }
```

### File System Validators

{.table}
| Validator | Description |
|---|---|
| `Validate.FileExists()` | Path must refer to an existing file |
| `Validate.DirectoryExists()` | Path must refer to an existing directory |
| `Validate.PathExists()` | Path must refer to an existing file or directory |

```csharp
{ "<input>", "Input file", v => input = v, validate: Validate.FileExists() }
{ "o|output-dir=", "Output dir", v => dir = v, validate: Validate.DirectoryExists() }
```

### Custom Validators

{.table}
| Validator | Description |
|---|---|
| `Validate.That<T>(predicate, errorMessage)` | Custom predicate with error message |
| `Validate.Custom<T>(validator)` | Pass-through for an `OptionValidator<T>` delegate |

```csharp
{ "p|port=", "Port", (int v) => port = v,
    validate: Validate.That<int>(v => v % 2 == 0, "Port must be an even number.") }
```

### Composing Validators

Use `Validate.Chain(...)` to combine multiple validators. The first failure wins:

```csharp
{ "p|port=", "Port", (int v) => port = v,
    validate: Validate.Chain(
        Validate.Range(1, 65535),
        Validate.That<int>(v => v != 80, "Port 80 is reserved.")
    )
}
```

## Option Constraints

Constraints let you declare relationships between options. In collection initializer style, add constraint nodes directly in the command declaration. They are checked **after** all options are parsed (including environment variable fallbacks) and **before** the command action runs.

### Mutually Exclusive Options

Prevent two or more options from being used together:

```csharp
var app = new CommandApp("myexe")
{
    { "j|json", "Output JSON", _ => {} },
    { "x|xml", "Output XML", _ => {} },
    { "c|csv", "Output CSV", _ => {} },
    new MutuallyExclusiveConstraint("json", "xml", "csv"),
    (ctx, _) => ValueTask.FromResult(0)
};
```

If you are not using collection initializers, the fluent alternative is:

```csharp
app.AddMutuallyExclusive("json", "xml", "csv");
```

If the user passes `--json --xml`, the error is:

```
myexe: Options `--json` and `--xml` cannot be used together.
Use `myexe --help` for usage.
```

### Requires Constraint

Declare that when one option is present, another must also be specified:

```csharp
var app = new CommandApp("myexe")
{
    { "u|user=", "User", _ => {} },
    { "p|password=", "Password", _ => {} },
    new RequiresConstraint("password", "user"),
    (ctx, _) => ValueTask.FromResult(0)
};
```

If you are not using collection initializers, the fluent alternative is:

```csharp
app.AddRequires("password", "user");
```

If the user passes `--password secret` without `--user`, the error is:

```
myexe: Option `--password` requires `--user` to also be specified.
Use `myexe --help` for usage.
```

### Combining Constraints

You can add multiple constraints to the same command:

```csharp
var app = new CommandApp("myexe")
{
    { "j|json", "Output JSON", _ => {} },
    { "x|xml", "Output XML", _ => {} },
    { "v|verbose", "Verbose output", _ => {} },
    { "q|quiet", "Quiet output", _ => {} },
    { "u|user=", "User", _ => {} },
    { "p|password=", "Password", _ => {} },
    { "tls-cert=", "TLS cert path", _ => {} },
    { "tls-key=", "TLS key path", _ => {} },

    new MutuallyExclusiveConstraint("json", "xml"),
    new MutuallyExclusiveConstraint("verbose", "quiet"),
    new RequiresConstraint("password", "user"),
    new RequiresConstraint("tls-cert", "tls-key"),

    (ctx, _) => ValueTask.FromResult(0)
};
```

Equivalent fluent setup:

```csharp
app.AddMutuallyExclusive("json", "xml");
app.AddMutuallyExclusive("verbose", "quiet");
app.AddRequires("password", "user");
app.AddRequires("tls-cert", "tls-key");
```

### Constraint Timing

Constraints are checked in this order:

1. Command-line options are parsed.
2. Environment variable fallbacks are applied for options not set on the command line.
3. Constraint checks run (mutually exclusive, requires).
4. The command action is invoked.

This means environment variable fallbacks **do** trigger constraint checks. For example, if `--json` is set via an environment variable and `--xml` is passed on the command line, the mutually exclusive constraint still fires.

## Next Steps

- [Options](options.md) — full option reference
- [Arguments](arguments.md) — positional arguments
- [Help & Output](help-output.md) — customize help rendering
- [Advanced Topics](advanced.md) — parse API, completions, and configuration

