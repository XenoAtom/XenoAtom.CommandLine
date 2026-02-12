# Options

Options are the core building block of any command-line interface. XenoAtom.CommandLine provides a rich option syntax inherited from Mono.Options with significant extensions.

## Declaring Options

Options are added to a `CommandApp` or `Command` using collection initializers or the `Add` method:

```csharp
var app = new CommandApp("myexe")
{
    { "o|output=", "The target output {FILE}", v => target = v },
};

// Equivalent:
app.Add("o|output=", "The target output {FILE}", v => target = v);
```

## Option Prototype Syntax

The prototype string defines the option's names and value behavior. Each `|`-delimited segment is an alias:

```
prototype = alias ( '|' alias )*
alias     = name [ '=' | ':' ] [ separator ]
```

- **`=`** — the option **requires** a value (e.g. `"n|name="`)
- **`:`** — the option has an **optional** value (e.g. `"o:"`)
- No suffix — the option is a **flag/boolean** (e.g. `"v|verbose"`)

The suffix only needs to appear on one alias, but if it appears on multiple aliases it must be consistent.

### Quick Reference

| Prototype | Type | Command-line examples | Notes |
|---|---|---|---|
| `"v\|verbose"` | Flag | `-v`, `--verbose`, `/v` | `+`/`-` suffix to explicitly enable/disable |
| `"n\|name="` | Required value | `--name John`, `--name=John`, `-nJohn` | Next token consumed as value |
| `"o:"` | Optional value | `-o`, `-oVALUE`, `-o:VALUE` | Must be inline; `-o VALUE` does **not** attach `VALUE` |
| `"D:"` (2 values) | Key/value pair | `-DKEY`, `-DKEY=VALUE` | Second value optional with `:` |
| `"I\|macro="` (2 values) | Key/value pair | `-IKEY=VALUE`, `--macro X=Y` | Second value required with `=` |
| `"P={->}"` (2 values) | Custom separator | `-PKEY->VALUE` | Separator declared between `{...}` |

### Option Prefixes

All options can be invoked with any of the three prefixes:

| Prefix | Example |
|---|---|
| `-` (POSIX short) | `-v`, `-n John` |
| `--` (GNU long) | `--verbose`, `--name=John` |
| `/` (Windows) | `/v`, `/name:John` |

> **Note:** `/`-prefixed tokens that look like absolute paths (e.g. `/mnt/home`) are treated as positional arguments, not options.

## Flag (Boolean) Options

A flag option has no `=` or `:` in its prototype. It receives a non-null value when present:

```csharp
bool verbose = false;
var app = new CommandApp("myexe")
{
    { "v|verbose", "Enable verbose output", v => verbose = v is not null },
};
```

You can explicitly enable or disable a flag with `+` and `-`:

```csharp
await app.RunAsync(["-v"]);   // verbose == true
await app.RunAsync(["-v+"]);  // verbose == true
await app.RunAsync(["-v-"]);  // verbose == false
```

## Required Value Options

Append `=` to the prototype to require a value:

```csharp
string? name = null;
var app = new CommandApp("myexe")
{
    { "n|name=", "Your {NAME}", v => name = v },
};
```

The value can be provided in several ways:
- `--name John` (next argument)
- `--name=John` (inline with `=`)
- `-nJohn` (inline with short option)
- `-n:John` (inline with `:`)
- `/name:John` (Windows style)

## Optional Value Options

Append `:` to the prototype for an optional value:

```csharp
string? output = null;
var app = new CommandApp("myexe")
{
    { "o:", "Output file (optional)", v => output = v },
};
```

When the value is optional, it **must** be provided inline. `-o VALUE` does **not** attach `VALUE` to `-o` — instead, `VALUE` becomes a positional argument and `output` is set to `null` (or empty string).

Valid: `-ofile.txt`, `-o:file.txt`, `-o=file.txt`

## Key/Value Pair Options

When an option callback has two parameters, values are automatically split into a key and a value:

```csharp
var app = new CommandApp("myexe")
{
    { "D:", "Define {0:NAME} and optional {1:VALUE}", (key, value) =>
    {
        if (key is null) throw new OptionException("Missing macro name", "D");
        Console.WriteLine($"Macro: {key} = {value}");
    }},
    { "I|macro=", "Define {0:NAME} and required {1:VALUE}", (key, value) =>
    {
        Console.WriteLine($"Macro: {key} = {value}");
    }},
};
```

```sh
myexe -DA=B -DHello -IG=F --macro X=Y
```

Output:

```
Macro: A = B
Macro: Hello =
Macro: G = F
Macro: X = Y
```

The default key/value separators are `=` and `:`. You can use a custom separator:

```csharp
{ "P={->}", "Define {0:NAME} and {1:VALUE}", (k, v) => Console.WriteLine($"{k} -> {v}") },
```

Now `-PKey->Value` splits into `Key` and `Value`.

## Typed Options

Any type that implements `ISpanParsable<TSelf>` can be used directly:

```csharp
int port = 0;
var app = new CommandApp("myexe")
{
    { "p|port=", "Server {PORT}", (int v) => port = v },
};
```

This works with all built-in numeric types, `DateTime`, `DateTimeOffset`, `Guid`, `TimeSpan`, `IPAddress`, and any custom type implementing `ISpanParsable<T>`.

## Collecting Multiple Values

Instead of a callback, you can bind an option directly to a collection:

```csharp
var names = new List<string>();
var ports = new List<int>();

var app = new CommandApp("myexe")
{
    { "n|name=", "A {NAME}", names },
    { "p|port=", "A {PORT}", ports },
};
```

Each time the option appears on the command line, the value is added to the list:

```sh
myexe --name Alice --name Bob --port 8080 --port 9090
```

## Enum Options

Use `EnumWrapper<T>` for AOT-friendly enum parsing:

```csharp
var colors = new List<Color>();

var app = new CommandApp("myexe")
{
    { "c|color=", $"The {{COLOR}} ({EnumWrapper<Color>.Names})", (EnumWrapper<Color> v) => colors.Add(v) },
};
```

`EnumWrapper<T>` implements `ISpanParsable<T>` and provides case-insensitive parsing. The `.Names` property returns a comma-separated list of valid values for use in descriptions.

## Option Bundling

Single-character options can be bundled with the `-` prefix (POSIX/tar style):

```csharp
bool a = false, b = false, c = false;
var app = new CommandApp("myexe")
{
    { "a", "Flag A", v => a = v is not null },
    { "b", "Flag B", v => b = v is not null },
    { "c", "Flag C", v => c = v is not null },
};

await app.RunAsync(["-abc"]); // a == true, b == true, c == true
```

At most one option in the bundle can accept a value, and its value starts from the next character:

```csharp
string? file = null;
var app = new CommandApp("myexe")
{
    { "x", "Extract", v => {} },
    { "f=", "Input {FILE}", v => file = v },
};

await app.RunAsync(["-xfarchive.tar"]); // file == "archive.tar"
```

## Value Placeholders in Descriptions

Use `{NAME}` in the description to label the value in help output:

```csharp
{ "n|name=", "Your {NAME}", v => name = v }
// Help: -n, --name=NAME            Your NAME
```

For key/value options, use indexed placeholders:

```csharp
{ "D:", "Define {0:KEY} and optional {1:VALUE}", (k, v) => {} }
// Help: -D[=KEY:VALUE]             Define KEY and optional VALUE
```

## Stop Parsing with `--`

The `--` token stops option parsing. Everything after `--` is treated as a positional argument:

```sh
myexe --name John -- --not-an-option -x /mnt/home
```

This is useful for passing values that start with `-` without them being interpreted as options.

## Hidden Options

Options can be hidden from help output:

```csharp
{ "secret=", "Secret option", v => {}, hidden: true }
```

Hidden options are still functional — they just don't appear in `--help`.

## Environment Variable Fallback

Options can fall back to environment variables when not provided on the command line:

```csharp
int port = 0;
var includes = new List<string>();

var app = new CommandApp("myexe")
{
    { "p|port=", "Server {PORT}", (int v) => port = v, envVar: "MY_PORT" },
    { "i|include=", "Include {PATH}", includes, envVar: "MY_INCLUDES", envVarDelimiter: Path.PathSeparator },
    (ctx, _) => ValueTask.FromResult(0)
};
```

The environment variable name appears in help output:

```
  -p, --port=PORT            Server PORT [env: MY_PORT]
```

Rules:
- The environment variable is only used when the option is **not** provided on the command line.
- For value options, environment variable values go through the same parse pipeline as command-line values.
- For flag options, accepted values are `true`/`false`, `1`/`0`, `yes`/`no` (case-insensitive).
- The `envVarDelimiter` parameter allows splitting a single environment variable value into multiple values (e.g. `PATH`-style lists).
- Environment variable fallback is skipped when `--help` is requested.

## Next Steps

- [Commands](commands.md) — sub-commands, groups, and actions
- [Arguments](arguments.md) — positional arguments
- [Validation & Constraints](validation.md) — validate values and declare option relationships
