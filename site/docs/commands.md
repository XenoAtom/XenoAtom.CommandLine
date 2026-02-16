---
title: "Commands"
---

# Commands

XenoAtom.CommandLine supports commands and sub-commands, allowing you to build rich multi-command CLIs similar to `git`, `docker`, or `dotnet`.

## CommandApp

`CommandApp` is the entry point for your application. It inherits from `Command` and is the root of the command tree:

<!-- snippet: site_docs_commands_md_001 -->
```cs
var app = new CommandApp("myexe")
{
    { "v|verbose", "Enable verbose output", v => {} },
    new HelpOption(),
    (ctx, _) =>
    {
        ctx.Out.WriteLine("Root command executed");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
```
<!-- endSnippet -->

If you omit the name, it defaults to the current executable name.

## Sub-Commands

Add sub-commands using `Command`:

<!-- snippet: site_docs_commands_md_002 -->
```cs
using XenoAtom.CommandLine;

const string _ = "";
string? name = null;
var messages = new List<string>();
var files = new List<string>();

var app = new CommandApp("myexe")
{
    new CommandUsage(),
    _,
    { "n|name=", "Your {NAME}", v => name = v },
    new HelpOption(),
    _,
    "Available commands:",
    new Command("commit", "Commit changes")
    {
        _,
        "Options:",
        { "m|message=", "Commit {MESSAGE}", messages },
        new HelpOption(),
        _,
        "Arguments:",
        { "<files>*", "Files to commit", files },
        (ctx, _) =>
        {
            ctx.Out.WriteLine($"Committing as {name}");
            foreach (var msg in messages)
                ctx.Out.WriteLine($"  Message: {msg}");
            foreach (var file in files)
                ctx.Out.WriteLine($"  File: {file}");
            return ValueTask.FromResult(0);
        }
    },
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Hello, {name}! Use 'myexe commit' to commit.");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
```
<!-- endSnippet -->

Running `myexe --help`:

```
Usage: myexe [options] <command>

  -n, --name=NAME            Your NAME
  -h, -?, --help             Show this message and exit

Available commands:
  commit                     Commit changes
```

Running `myexe commit --help`:

```
Usage: myexe commit [options] <files>*

Options:
  -m, --message=MESSAGE      Commit MESSAGE
  -h, -?, --help             Show this message and exit

Arguments:
  <files>*                   Files to commit
```

### How Parsing Works

1. Options on the root command are parsed first.
2. When a token matches a sub-command name, parsing switches to that command.
3. Root-level options (like `--name`) parsed **before** the sub-command are available to the sub-command's action.
4. The sub-command's action is invoked when parsing completes.

### Nested Sub-Commands

Commands can be nested to any depth:

<!-- snippet: site_docs_commands_md_003 -->
```cs
var app = new CommandApp("myexe")
{
    new Command("remote")
    {
        new Command("add", "Add a remote")
        {
            { "n|name=", "Remote {NAME}", v => {} },
            (ctx, _) => ValueTask.FromResult(0)
        },
        new Command("remove", "Remove a remote")
        {
            { "n|name=", "Remote {NAME}", v => {} },
            (ctx, _) => ValueTask.FromResult(0)
        },
    },
};
```
<!-- endSnippet -->

This creates `myexe remote add --name origin` and `myexe remote remove --name origin`.

### Hidden Commands

Commands can be hidden from help output:

<!-- snippet: site_docs_commands_md_004 -->
```cs
var cmd = new Command("internal-debug", "Debug command") { Hidden = true };
```
<!-- endSnippet -->

Hidden commands are still executable — they just don't appear in `--help`.

## Actions

Every `Command` (including `CommandApp`) can have a single action that runs after parsing. There are several callback signatures:

### With Context and Arguments

<!-- snippet: site_docs_commands_md_005 -->
```cs
(ctx, _) =>
{
    ctx.Out.WriteLine("Hello!");
    return ValueTask.FromResult(0);
}
```
<!-- endSnippet -->

The `ctx` parameter is a `CommandRunContext` providing access to `Out`, `Error`, and `RunConfig`. The second parameter is the array of remaining positional arguments (empty when using `CommandArgument` declarations).

### Arguments Only

<!-- snippet: site_docs_commands_md_006 -->
```cs
(arguments) =>
{
    foreach (var arg in arguments)
        Console.WriteLine(arg);
    return ValueTask.FromResult(0);
}
```
<!-- endSnippet -->

### Async Actions

All action signatures return `ValueTask<int>` for native async support:

<!-- snippet: site_docs_commands_md_007 -->
```cs
async (ctx, _) =>
{
    await SomeAsyncWork();
    return 0;
}
```
<!-- endSnippet -->

### Exit Codes

The return value is the process exit code. Return `0` for success and any non-zero value for failure. When an error is thrown during parsing, `RunAsync` returns `1`.

## CommandGroup

`CommandGroup` lets you group related nodes (options, commands, text) together. More importantly, groups can be **conditional** — their contents are only active when a condition is met:

<!-- snippet: site_docs_commands_md_008 -->
```cs
bool advanced = false;

var app = new CommandApp("myexe")
{
    "Options:",
    { "advanced", "Activate advanced options", v => advanced = v is not null },
    new HelpOption(),
    new CommandGroup(() => advanced)
    {
        "Advanced Options:",
        { "special1", "Special option 1", v => {} },
        { "special2", "Special option 2", v => {} },
    },
    (ctx, _) => ValueTask.FromResult(0)
};
```
<!-- endSnippet -->

Running `myexe --help`:

```
Usage: myexe [options]
Options:
      --advanced             Activate advanced options
  -h, -?, --help             Show this message and exit
```

Running `myexe --advanced --help`:

```
Usage: myexe [options]
Options:
      --advanced             Activate advanced options
  -h, -?, --help             Show this message and exit
Advanced Options:
      --special1             Special option 1
      --special2             Special option 2
```

The conditional group affects both **visibility** (help output) and **availability** (parsing). When the group is inactive, its options and commands are not recognized — the parser reports them as unknown.

### Conditional Commands

You can also put commands inside conditional groups:

<!-- snippet: site_docs_commands_md_009 -->
```cs
new CommandGroup(() => advanced)
{
    "Advanced Commands:",
    new Command("debug", "Debug the application")
    {
        new HelpOption(),
        (ctx, _) => ValueTask.FromResult(0)
    },
}
```
<!-- endSnippet -->

The `debug` command only appears and is executable when `--advanced` is passed.

## Descriptive Text

Any string added to a command becomes descriptive text in the help output:

<!-- snippet: site_docs_commands_md_010 -->
```cs
var app = new CommandApp("myexe")
{
    "Options:",
    { "v|verbose", "Verbose", v => {} },
    new HelpOption(),
    "",
    "Available commands:",
    new Command("build", "Build the project")
    {
        (ctx, _) => ValueTask.FromResult(0)
    },
};
```
<!-- endSnippet -->

The empty string `""` (or `const string _ = ""`) adds a blank line in the help output. All items — options, commands, text — are rendered in declaration order.

## Built-in Options

XenoAtom.CommandLine provides two built-in option types:

### HelpOption

Adds `-h`, `-?`, and `--help`:

<!-- snippet: site_docs_commands_md_011 -->
```cs
new HelpOption()
// Equivalent to:
// { "h|?|help", "Show this message and exit", v => { /* triggers help */ } }
```
<!-- endSnippet -->

You can customize the prototype and description:

<!-- snippet: site_docs_commands_md_012 -->
```cs
new HelpOption("h|help", "Display help information")
```
<!-- endSnippet -->

### VersionOption

Adds `--version` (and optionally `-v`):

<!-- snippet: site_docs_commands_md_013 -->
```cs
new VersionOption("1.2.3")
// Equivalent to:
// { "v|version", "Show the version of this command", v => { /* prints version */ } }
```
<!-- endSnippet -->

If no version string is provided, it extracts the version from the assembly's informational version attribute.

## Parsing Flow

Understanding the parsing flow helps debug complex scenarios:

1. Parse options for the current command (tokens starting with `-`, `--`, or `/`), applying option callbacks as values are consumed.
2. If help/version is requested for the current command, stop and render output.
3. Apply environment-variable fallbacks for options not set on the command line.
4. Run option constraint checks (mutually exclusive, requires).
5. If sub-commands exist and the next token matches an active sub-command, dispatch to that sub-command and repeat.
6. Parse positional arguments (`CommandArgument`) for the resolved command.
7. Invoke the command action and return its exit code.
8. On error, the error is reported to `Error` and `RunAsync` returns `1`.

If the parser is currently expecting a value for an option, the next token is **always** consumed as that value — even if it looks like `--` or matches a sub-command name.

## Next Steps

- [Options](options.md) — full option reference
- [Arguments](arguments.md) — positional arguments and cardinality
- [Validation & Constraints](validation.md) — validate values and declare option relationships
- [Help & Output](help-output.md) — customize help rendering

