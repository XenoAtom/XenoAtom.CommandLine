---
title: "Positional Arguments"
---

# Positional Arguments

In addition to options (prefixed with `-`, `--`, `/`), XenoAtom.CommandLine supports positional arguments — values passed without any option prefix that are consumed in declaration order.

## Declaring Arguments

Arguments use angle-bracket prototypes and are added with the same collection initializer syntax as options:

<!-- snippet: site_docs_arguments_md_001 -->
```cs
string? input = null;
var app = new CommandApp("myexe")
{
    new HelpOption(),
    "Arguments:",
    { "<input>", "Input file", v => input = v },
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Input: {input}");
        return ValueTask.FromResult(0);
    }
};
```
<!-- endSnippet -->

Running `myexe file.txt`:

```
Input: file.txt
```

## Cardinality

The suffix on the argument prototype controls how many values are expected:

{.table}
| Prototype | Cardinality | Meaning |
|---|---:|---|
| `<input>` | Exactly 1 | Required — an error is raised if missing |
| `<output>?` | 0 or 1 | Optional — only allowed as the last argument |
| `<files>*` | 0 to N | Optional list — only allowed as the last argument |
| `<files>+` | 1 to N | Required list (at least one) — only allowed as the last argument |
| `<>` | 0 to N | Remainder — forwarded to the action callback |

### Required Argument

<!-- snippet: site_docs_arguments_md_002 -->
```cs
{ "<input>", "Input file", v => input = v }
```
<!-- endSnippet -->

If the user does not provide a value, an error is raised:

```
myexe: Missing required argument: <input>
```

### Optional Argument

<!-- snippet: site_docs_arguments_md_003 -->
```cs
{ "<output>?", "Output file (optional)", v => output = v }
```
<!-- endSnippet -->

The `?` suffix makes the argument optional. Optional arguments must be the **last** declared argument.

### List Arguments

<!-- snippet: site_docs_arguments_md_004 -->
```cs
var files = new List<string>();
{ "<files>*", "Input files", files }
```
<!-- endSnippet -->

The `*` suffix collects zero or more remaining positional values into a list. Use `+` to require at least one:

<!-- snippet: site_docs_arguments_md_005 -->
```cs
{ "<files>+", "Input files (at least one)", files }
```
<!-- endSnippet -->

List arguments must be the **last** declared argument.

### Remainder Argument

<!-- snippet: site_docs_arguments_md_006 -->
```cs
{ "<>", "Extra arguments passed to the action" }
```
<!-- endSnippet -->

Or with the explicit helper:

<!-- snippet: site_docs_arguments_md_007 -->
```cs
app.AddRemainder("Extra arguments passed to the action");
```
<!-- endSnippet -->

The `<>` prototype forwards all remaining positional arguments to the command action's `arguments` array instead of binding them to a specific variable or list:

<!-- snippet: site_docs_arguments_md_008 -->
```cs
var app = new CommandApp("myexe")
{
    { "<>", "Extra arguments" },
    (ctx, arguments) =>
    {
        foreach (var arg in arguments)
            ctx.Out.WriteLine($"Arg: {arg}");
        return ValueTask.FromResult(0);
    }
};
```
<!-- endSnippet -->

## Combining Arguments

You can combine multiple arguments with different cardinalities:

<!-- snippet: site_docs_arguments_md_009 -->
```cs
string? input = null;
string? output = null;
var extraFiles = new List<string>();

var app = new CommandApp("myexe")
{
    new CommandUsage(),
    new HelpOption(),
    "Arguments:",
    { "<input>", "Input file", v => input = v },
    { "<output>?", "Output file (optional)", v => output = v },
    { "<extra>*", "Additional files", extraFiles },
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Input: {input}");
        ctx.Out.WriteLine($"Output: {output ?? "(none)"}");
        foreach (var f in extraFiles)
            ctx.Out.WriteLine($"Extra: {f}");
        return ValueTask.FromResult(0);
    }
};
```
<!-- endSnippet -->

Running `myexe --help`:

```
Usage: myexe [options] <input> [<output>] <extra>*

  -h, -?, --help             Show this message and exit

Arguments:
  <input>                    Input file
  <output>?                  Output file (optional)
  <extra>*                   Additional files
```

## Typed Arguments

Like options, arguments support typed parsing with `ISpanParsable<T>`:

<!-- snippet: site_docs_arguments_md_010 -->
```cs
int count = 0;
{ "<count>", "Number of items", (int v) => count = v }
```
<!-- endSnippet -->

## Argument Validation

Arguments also support validation (see [Validation & Constraints](validation.md)):

<!-- snippet: site_docs_arguments_md_011 -->
```cs
{ "<input>", "Input {FILE}", v => input = v, Validate.FileExists(), false }
```
<!-- endSnippet -->

## Strict Argument Parsing

By default, if a command declares **no** positional arguments, passing a positional value is an error:

```
myexe: Unexpected argument: file.txt
```

This prevents typos in option names from silently becoming positional arguments. If you want to accept arbitrary extra arguments, declare `<>`.

## Arguments with Options

Arguments and options work together naturally. Options are parsed first, and remaining tokens become positional arguments in declaration order:

<!-- snippet: site_docs_arguments_md_012 -->
```cs
var app = new CommandApp("myexe")
{
    { "v|verbose", "Verbose", v => {} },
    new HelpOption(),
    { "<input>", "Input file", v => input = v },
    { "<output>?", "Output file", v => output = v },
    (ctx, _) => ValueTask.FromResult(0)
};
```
<!-- endSnippet -->

```sh
myexe --verbose input.txt output.txt
myexe input.txt --verbose output.txt   # same result — options are extracted first
```

## Stop Parsing with `--`

Use `--` to stop option parsing. Everything after `--` becomes positional arguments:

```sh
myexe -- --not-an-option -x /mnt/home
```

This is especially useful when argument values start with `-`.

## Next Steps

- [Options](options.md) — full option reference
- [Commands](commands.md) — sub-commands and groups
- [Validation & Constraints](validation.md) — validate argument values

