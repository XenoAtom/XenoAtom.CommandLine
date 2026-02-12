# Getting Started

This guide walks you through installing XenoAtom.CommandLine and building your first command-line application.

## Installation

Add the NuGet package to your project:

```sh
dotnet add package XenoAtom.CommandLine
```

XenoAtom.CommandLine targets `net8.0` and later. It has zero dependencies and is fully compatible with NativeAOT publishing.

## Your First Application

A minimal CLI application needs just a `CommandApp`, one or more options, and an action:

```csharp
using XenoAtom.CommandLine;

string? name = null;

var app = new CommandApp("greet")
{
    { "n|name=", "Your {NAME}", v => name = v },
    new HelpOption(),
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Hello, {name ?? "World"}!");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
```

Running `greet --name Alice` prints:

```
Hello, Alice!
```

Running `greet --help` prints:

```
Usage: greet [options]

  -n, --name=NAME            Your NAME
  -h, -?, --help             Show this message and exit
```

### What's Happening

1. `CommandApp("greet")` creates the root command with the executable name `greet`.
2. `{ "n|name=", ... }` declares an option with aliases `-n` and `--name` that requires a value (the `=` suffix).
3. `new HelpOption()` adds the built-in `--help` / `-h` / `-?` option.
4. The lambda `(ctx, _) => { ... }` is the command action — it runs after all options are parsed.
5. `app.RunAsync(args)` parses the arguments and invokes the action.

## Adding More Options

You can add multiple options of different types:

```csharp
using XenoAtom.CommandLine;

string? name = null;
int age = 0;
bool verbose = false;

var app = new CommandApp("greet")
{
    new CommandUsage(),
    { "n|name=", "Your {NAME}", v => name = v },
    { "a|age=", "Your {AGE}", (int v) => age = v },
    { "v|verbose", "Enable verbose output", v => verbose = v is not null },
    new HelpOption(),
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Hello, {name}! You are {age} years old.");
        if (verbose)
            ctx.Out.WriteLine("(verbose mode enabled)");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
```

Key points:
- `"a|age="` with a typed callback `(int v) =>` automatically parses the value as an `int`.
- `"v|verbose"` (no `=` or `:`) is a boolean flag — it is `true` when present and `false` when absent.
- `CommandUsage()` adds a `Usage:` line at the top of the help output.

Running `greet --help`:

```
Usage: greet [options]

  -n, --name=NAME            Your NAME
  -a, --age=AGE              Your AGE
  -v, --verbose              Enable verbose output
  -h, -?, --help             Show this message and exit
```

## Adding a Sub-Command

Real-world CLI tools often have sub-commands (like `git commit`, `docker run`). Add them with `Command`:

```csharp
using XenoAtom.CommandLine;

const string _ = "";
string? name = null;
var messages = new List<string>();

var app = new CommandApp("myapp")
{
    new CommandUsage(),
    _,
    { "n|name=", "Your {NAME}", v => name = v },
    new HelpOption(),
    _,
    "Available commands:",
    new Command("greet", "Greet someone")
    {
        _,
        "Options:",
        { "m|message=", "Greeting {MESSAGE}", messages },
        new HelpOption(),
        (ctx, _) =>
        {
            ctx.Out.WriteLine($"Hello, {name}!");
            foreach (var msg in messages)
                ctx.Out.WriteLine($"  {msg}");
            return ValueTask.FromResult(0);
        }
    },
    (ctx, _) =>
    {
        ctx.Out.WriteLine("Use 'myapp greet --help' for more info.");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
```

The empty string `_` adds a blank line in the help output for visual separation.

## Adding Positional Arguments

Besides options (prefixed with `-`/`--`/`/`), you can declare positional arguments:

```csharp
using XenoAtom.CommandLine;

string? input = null;
var extraFiles = new List<string>();

var app = new CommandApp("myapp")
{
    new CommandUsage(),
    new HelpOption(),
    "Arguments:",
    { "<input>", "The input file", v => input = v },
    { "<extra>*", "Additional files", extraFiles },
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Input: {input}");
        foreach (var f in extraFiles)
            ctx.Out.WriteLine($"Extra: {f}");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
```

Running `myapp file1.txt file2.txt file3.txt`:

```
Input: file1.txt
Extra: file2.txt
Extra: file3.txt
```

See the [Arguments](arguments.md) guide for full details on cardinality (`?`, `*`, `+`, `<>`).

## Error Handling

XenoAtom.CommandLine provides clear error messages out of the box. If a user passes an unknown option or an invalid value, the library prints a helpful message:

```
myapp: Unknown option: --unknown
Use `myapp --help` for usage.
```

For strict error handling, the library rejects unknown `-`/`--` options by default (`StrictOptionParsing = true`). This prevents typos from being silently treated as positional arguments.

## Using the Parse API for Testing

You can parse arguments without executing the command action — useful for unit testing:

```csharp
var result = app.Parse(["--name", "Alice", "--age", "30"]);

// result.HasErrors             → false
// result.OptionValues["name"]  → ["Alice"]
// result.OptionValues["age"]   → ["30"]
```

See [Advanced Topics](advanced.md#parse-api) for full details.

## Next Steps

Now that you have a working application, explore the rest of the documentation:

- [Options](options.md) — learn about all option types, typed parsing, aliases, and bundling
- [Commands](commands.md) — nested commands, groups, and conditional commands
- [Arguments](arguments.md) — positional arguments and cardinality
- [Validation & Constraints](validation.md) — validate values and declare option relationships
- [Help & Output](help-output.md) — customize help rendering and use the Terminal package
- [Advanced Topics](advanced.md) — parse API, completions, response files, and configuration
