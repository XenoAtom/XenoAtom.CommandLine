# XenoAtom.CommandLine User Guide

XenoAtom.CommandLine is a library that provides a simple and easy way to create command-line applications in .NET. It is a fork of the Mono.Options library with some modifications and improvements.

- [CommandApp and Command](#commandapp-and-command)
- [Options](#options)
- [Command Arguments](#command-arguments)
- [Help Text](#help-text)
- [Custom Output Rendering](#custom-output-rendering)
- [Actions](#actions)
- [Completions](#completions)
- [Configuration](#configuration)
- [ArgumentSource](#argumentsource)
- [CommandGroup](#commandgroup)
- [Performance and Benchmarks](#performance-and-benchmarks)
- [Going further](#going-further)
- [Class diagram](#class-diagram)

## CommandApp and Command

There are 2 main classes that you will use when creating a command-line application with:

- `CommandApp`: The entry point for your command-line application. A `CommandApp` inherits from `Command`.
    ```csharp
    var app = new CommandApp("myexe") {
        { "o|output=", "The target output {FILE}", v => target = v },
    };
    ```
    For example, `myexe --output file.txt` will set `target` to `file.txt`.
- `Command`: Represents a sub-command that can be executed from the command line. You can add:
  - `Option`: Options that your command will accept.
    ```csharp
    var app = new CommandApp("myexe") {
        new Command("hello") {
            { "n|name=", "The {NAME} of the person", v => name = v },
        }
    };
    ```
    For example, `myexe hello -n John` will set `name` to `John`.
  - Plain strings: Text that will be displayed when the showing the help

The first class that you will use is the `CommandApp` class.
This class is the entry point for your command-line application. You can create an instance of this class and then add options to it. The `CommandApp` class will parse the command-line arguments and call the appropriate methods based on the options that were specified.

The `CommandApp` class inherits from `Command` and benefits from collection initializers to add options, arguments, text and other commands in a readable and concise way.

Parsing flow (high level):
- Parse options (until `--`), applying option actions as values are consumed.
- If sub-commands exist, dispatch to the first matching active sub-command.
- Parse positional arguments (`CommandArgument`) for the selected command.
  - Positional arguments are strict by default: if none are declared, passing one is an error.
  - Use `<>` to accept and forward remaining arguments to the command action.
- Invoke the command action (if any) and return its exit code. Errors are reported to `Error` and `RunAsync` returns `1`.


```csharp
using XenoAtom.CommandLine;

const string _ = "";
bool flag = false;
string? name = null;
int age = 0;
var files = new List<string>();

var commandApp = new CommandApp()
{
    new CommandUsage(),
    _,
    "Options:",
    {"f|flag", "This is a flag", v => flag = v != null }, 
    {"n|name=", "Your {NAME}", v => name = v},
    {"a|age=", "Your {AGE}", (int v) => age = v},
    new HelpOption(),
    _,
    "Arguments:",
    { "<files>+", "Input files", files },
    // Run the command
    (_) =>
    {
        if (files.Count == 0) throw new CommandException("Missing at least one file argument");
        if (name == null) throw new OptionException("Missing name argument", nameof(name));
        if (age == 0) throw new OptionException("Missing age argument", nameof(age));
        
        Console.Out.WriteLine($"Hello {name}! You are {age} years old with flag = {flag}");
        int index = 0;
        foreach (var file in files)
        {
            Console.Out.WriteLine($"File[{index}]: {file}");
            index++;
        }
        return ValueTask.FromResult(0);
    }
};

await commandApp.RunAsync(args);
```

## Options

An option is composed of a prototype that defines the option syntax (e.g. `"o|output="`).

### Quick Reference

The prototype is what you pass to `Add(...)` (or use in the collection initializer). It controls how the option can be written on the command line.

| Prototype (declaration) | What it declares | How it’s passed | Notes |
|---|---|---|---|
| `"v|verbose"` | Flag / boolean option | `-v`, `--verbose`, `/v` | Use `-v+` / `-v-` to explicitly enable/disable when parsed as a bool option. |
| `"n|name="` | Required value | `--name John`, `--name=John`, `-nJohn`, `-n:John`, `/name:John` | If you omit `:`/`=`, the next token is consumed as the value (e.g. `--name John`). |
| `"o:"` | Optional value | `-o`, `-oVALUE`, `-o:VALUE`, `--o=VALUE` | Optional values must be inline; `-o VALUE` does **not** attach `VALUE` to `-o`. |
| `"D:"` with 2 values | Key/value pair (2 values) | `-DKEY`, `-DKEY=VALUE`, `-DKEY:VALUE` | Typically used for “macro” options. With `:` (optional), the second value can be omitted. |
| `"P={->}"` with 2 values | Key/value pair with custom separator | `-PKEY->VALUE` | Custom separator is declared between `{...}`. |
| `"i"` in a bundle | Bundled short options | `-abc`, `-txc` | Only works with `-` and single-letter options; at most one option in the bundle can take a value. |
| `--` | Stop option parsing | `myexe -- --not-an-option -x /mnt/home` | Everything after `--` is treated as positional arguments. |

Value placeholders in descriptions:
- For `MaxValueCount == 1`, `"{NAME}"` sets the displayed value name.
- For multiple values, use `"{0:KEY} {1:VALUE}"` to name each value.

Strictness note:
- Unknown `-` / `--` options fail by default (`CommandConfig.StrictOptionParsing = true`).
- This does not apply to `/...` tokens so POSIX paths like `/mnt/home` can be passed as positional arguments.

```
Regex-like BNF Grammar: 
    name: .+
    type: [=:]
    sep: ( [^{}]+ | '{' .+ '}' )?
    aliases: ( name type sep ) ( '|' name type sep )*
```

Each `|`-delimited name is an alias for the associated action.  If the
format string ends in a `=`, it has a required value.  If the format
string ends in a `:`, it has an optional value.  If neither `=` or `:`
is present, no value is supported.  `=` or `:` need only be defined on one
alias, but if they are provided on more than one they must be consistent.

Each alias portion may also end with a "key/value separator", which is used
to split option values if the option accepts > 1 value.  If not specified,
it defaults to `=` and `:`.  If specified, it can be any character except
`{` and `}` OR the *string* between `{` and `}`.  If no separator should be
used (i.e. the separate values should be distinct arguments), then "{}"
should be used as the separator.

Options are extracted either from the current option by looking for
the option name followed by an `=` or `:`, or is taken from the
following option IFF:
- The current option does not contain a `=` or a `:`
- The current option requires a value (i.e. not a Option type of `:`)

The `name` used in the option format string does NOT include any leading
option indicator, such as `-`, `--`, or `/`.  All three of these are
permitted/required on any named option.

Option bundling is permitted so long as:
  - `-` is used to start the option group
  - all of the bundled options are a single character
  - at most one of the bundled options accepts a value, and the value
    provided starts from the next character to the end of the string.

This allows specifying `-a -b -c` as `-abc`, and specifying `-D name=value`
as `-Dname=value`.

Option processing is disabled by specifying `--`. All tokens after `--` are treated as positional arguments (use `<>` to forward them to the command action).
The `--` marker itself is not included in the resulting arguments; any later `--` is treated as a regular positional token.

If the parser is currently expecting a value for an option, the next token is always consumed as that value, even if it is `--` or matches an existing sub-command name.

Examples:

```c#
int verbose = 0;
var app = new CommandApp()
{
    {"v", v => ++verbose},
    {"name=|value=", v => Console.WriteLine(v)},
    {"<>", "Extra arguments passed to the action"},
    (arguments) => { /* other code here */ }
};
await app.RunAsync(["-v", "--v", "/v", "-name=A", "/name", "B", "extra"]);
```

The above would parse the argument string array, and would invoke the
lambda expression three times, setting `verbose` to 3 when complete.  
It would also print out "A" and "B" to standard output.
The returned array in `arguments` would contain the string "extra" because the remainder argument `<>` is declared.

The interface [`ISpanParsable<TSelf>`](https://learn.microsoft.com/en-us/dotnet/api/system.ispanparsable-1) is also supported, allowing the use of
custom data types in the callback type; The method `ISpanParsable<TSelf>.Parse`
is used to convert the value option to an instance of the specified
type:

```c#
var app = new CommandApp () {
{ "foo=", (Foo f) => Console.WriteLine(f.ToString()) },
};
```

Random other tidbits:
- Boolean options (those w/o `=` or `:` in the option format string)
   are explicitly enabled if they are followed with `+`, and explicitly
   disabled if they are followed with `-`:
   ```csharp
      bool a;
      var p = new CommandApp() {
        { "a", s => a = s != null },
      };
      await p.RunAsync(["-a"]);    // sets a == true
      await p.RunAsync(["-a+"]);   // sets a == true
      await p.RunAsync(["-a-"]);   // sets a == false
    ```
- When declaring an option, you can name the value attached in the description:
  ```csharp
  string? name = null;
  int age = 0;
  var app = new CommandApp()
  {
      {"n|name=", "Your {NAME}", v => name = v},
      {"a|age=", "Your {AGE}", (int v) => age = v},
      new HelpOption(),
  };
  await app.RunAsync(["--help"]);
  ```
  will display the following message:
  ```
  Usage: HelloWorld [options]
    -n, --name=NAME            Your NAME
    -a, --age=AGE              Your AGE
    -h, -?, --help             Show this message and exit
  ```
- You can also create pair of key/values (like macros):
  ```csharp
  var app = new CommandApp()
  {
      { "D:", "Add a macro {0:NAME} and optional {1:VALUE}", (k, v) => {
        if (k is null) throw new OptionException("Missing macro name", "D");
        Console.WriteLine($"Macro: `{k}` => `{v}`"); 
      }},
      { "I|macro=", "Add a macro {0:NAME} and required {1:VALUE}", (k, v) => Console.WriteLine($"Required Macro: `{k}` => `{v}`") },
  };
  await app.RunAsync(["-DA=B", "-DHello=World", "-DG", "-IG=F", "--macro", "X=Y"]);
  ```
  will display the following message:
  ```
  Macro: `A` => `B`
  Macro: `Hello` => `World`
  Macro: `G` => ``
  Required Macro: `G` => `F`
  Required Macro: `X` => `Y`
  Use `HelloWorld --help` for usage.
  ```
  At the bottom you will notice that the `--help` option is displayed. This is because there are no action defined for the command app. See [Actions](#actions) for more information.
- You can append option values directly to a list without an action:
  ```csharp
  var strings = new List<string>();
  var ints = new List<int>();
  var otherArguments = new List<string>();
  var app = new CommandApp()
  {
      "Options:",
      { "n|name=", "Your {NAME}", strings },
      { "a|age=", "Your {AGE}", ints },
      { "<files>*", "Files", otherArguments},
      new HelpOption(),
      // Run the command
      (arguments) =>
      {
          foreach (var item in strings)
          {
              Console.Out.WriteLine(item);
          }
          foreach (var item in ints)
          {
              Console.Out.WriteLine(item);
          }
          foreach (var item in otherArguments)
          {
              Console.Out.WriteLine($"Arg: {item}");
          }
          return ValueTask.FromResult(0);
      }
  };
  await app.RunAsync(["Hello", "--name", "Lucy", "--age", "10", "--name", "John", "World"]);
  ```
  will display the following:
  ```
  Lucy
  John
  10
  Arg: Hello
  Arg: World
  ```
  Notice the usage of the positional argument `<files>*` that collects all remaining arguments into a list.
- There are builtin options like `HelpOption` and `VersionOption`:
      
  ```csharp
  var app = new CommandApp() {
      "Options:",
      new HelpOption(),
      new VersionOption(),
  };
  ```

  - `HelpOption` is similar to the declaration:
    ```csharp
    {"h|?|help", "Show this message and exit", v => {/* Specific action for help*/} },
    ```
  - `VersionOption` is similar to the declaration:
    ```csharp
    {"v|version", "Show the version of this command", v => {/* Specific action for version*/} },
    ```
    It will extract the version from the Assembly Informational Version attribute or the Assembly Version attribute and will display it on the standard output when the option is used.

## Command Arguments

In addition to options (prefixed with `-`, `--`, `/`), you can declare positional command arguments.

An argument prototype uses angle brackets:

| Prototype | Cardinality | Meaning |
|---|---:|---|
| `"<input>"` | 1 | Required positional argument |
| `"<output>?"` | 0..1 | Optional positional argument (only allowed for the last argument) |
| `"<files>*"` | 0..N | Optional list argument (only allowed for the last argument) |
| `"<files>+"` | 1..N | Required list argument (only allowed for the last argument) |
| `"<>"` | 0..N | Remainder argument forwarded to the command action (only allowed for the last argument) |

Collection initializer forms:
- Bind a single value: `{ "<input>", "Input file", v => input = v }`
- Collect multiple values: `{ "<files>*", "Extra files", extraFiles }`
- Accept pass-through remainder: `{ "<>", "Extra arguments passed to the action" }`

Example:

```csharp
string? input = null;
string? output = null;
var extraFiles = new List<string>();

var app = new CommandApp("myexe")
{
    "Arguments:",
    { "<input>", "Input file", v => input = v },
    { "<output>?", "Output file (optional)", v => output = v },

    // Collect all remaining arguments into a list.
    { "<files>*", "Extra files", extraFiles },

    new HelpOption(),
    (ctx, args) =>
    {
        // args is empty when all remaining arguments are collected via CommandArguments (e.g. <files>*).
        // Declare <> to forward remaining arguments to this action.
        return ValueTask.FromResult(0);
    }
};
```

If you declare no positional arguments, passing one is treated as an error. Use `<>` to accept and forward extra arguments to the command action.

Declared arguments are included in the default usage output (e.g. `Usage: myexe [options] <input> [<output>]` and `Usage: myexe [options] [args]...`).

## Help Text

Any string that is not an option is considered text and will be displayed when showing the help.

```csharp
var app = new CommandApp() {
    "Available commands:",
    new Command("hello") {
        "This is a plain text",
        "On a new line",
        "With the following option:",
        { "n|name=", "The {NAME} of the person", v => name = v },
    },
};
await app.RunAsync(args);
```

More in general, all the items (`CommandNode`: `Command`, `Option`, `string`, `ArgumentSource`, `Action`...) within a `Command` or a `CommandApp` are kept in order when displayed in the help message.

There is a special kind of text called `CommandUsage` that will be displayed at the beginning of the help message. It is used to display the usage of the command. You can use `{NAME}` to inject the full command path and `{SYNTAX}` to inject the default syntax derived from options/arguments/subcommands.

```csharp
var app = new CommandApp() {
    new CommandUsage(), // Defaults to "Usage: {NAME} {SYNTAX}"
    "Available commands:",
    // ...  
    new CommandUsage("Usage: {NAME} [--advanced] [Advanced Options]"),
    // 
};
await app.RunAsync(args);
```
You can have multiple `CommandUsage` in a `CommandApp` or a `Command`. If no command usages are found, it will display a default one as the first line of the help message, otherwise it will display the `CommandUsage` that are defined.

## Custom Output Rendering

All library-generated output (help, errors, unknown-token diagnostics, version, license header) can be replaced by setting `CommandConfig.OutputFactory`.

```csharp
var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = runConfig => new MyOutputRenderer()
})
{
    new HelpOption(),
    new VersionOption("1.2.3"),
    (ctx, _) => ValueTask.FromResult(0)
};
```

`ICommandOutput` receives the current `Command` object, so a renderer can use `command.Options`, `command.Arguments`, `command.SubCommands`, and `command.Nodes` to build plain text, JSON, or UI-specific visual output.

```csharp
public sealed class MyOutputRenderer : ICommandOutput
{
    public void WriteHelp(Command command, CommandRunConfig runConfig)
    {
        runConfig.Out.WriteLine($"Help for {command.GetFullCommandPath()}");
    }

    public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
    {
        runConfig.Error.WriteLine($"{command.GetFullCommandPath()}: {exception.Message}");
    }

    public void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenKind kind, IReadOnlyList<UnknownTokenInfo> unknownTokens)
    {
        foreach (var token in unknownTokens)
            runConfig.Error.WriteLine($"{kind}: {token.Token}");
    }

    public void WriteVersion(Command command, CommandRunConfig runConfig, string version)
        => runConfig.Out.WriteLine(version);

    public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText)
        => runConfig.Out.WriteLine(licenseText);
}
```

You can also render help with a one-off output implementation without changing `CommandConfig`:

```csharp
app.ShowHelp(new MyOutputRenderer());
```

`CommandOutputHelper` provides utility methods for common renderer tasks (`GetVisibleOptions`, `GetVisibleArguments`, `GetDescriptionText`, `RenderInvocation`, `RenderUnderline`, ...).

## Actions

A `CommandApp` and a `Command` are meant to be executed. You can add a single action to a `CommandApp` or a `Command` that will be executed when the command-line after the options and arguments are parsed.

When you declare positional arguments via `CommandArgument` (e.g. `<files>*`), those values are consumed and the action `arguments` array is empty. Use `<>` when you intentionally want to receive leftover/forwarded positional tokens in the command action.

For example, the following code:

```csharp
const string _ = "";
bool flag = false;
string? name = null;
int age = 0;

var commandApp = new CommandApp()
{
    new CommandUsage(),
    _,
    "Options:",
    {"f|flag", "This is a flag", v => flag = v != null },
    {"n|name=", "Your {NAME}", v => name = v},
    {"a|age=", "Your {AGE}", (int v) => age = v},
    new HelpOption(),
    {"<>", "Input files passed to the action"},
    // Run the command
    (arguments) =>
    {
        if (arguments.Length == 0) throw new CommandException("Missing at least one file argument");
        if (name == null) throw new OptionException("Missing name argument", nameof(name));
        if (age == 0) throw new OptionException("Missing age argument", nameof(age));

        Console.Out.WriteLine($"Hello {name}! You are {age} years old with flag = {flag}");
        int index = 0;
        foreach (var arg in arguments)
        {
            Console.Out.WriteLine($"Arg[{index}]: {arg}");
            index++;
        }
        return ValueTask.FromResult(0);
    }
};

await commandApp.RunAsync(["--name", "Alex", "--age", "30", "--flag", "file1", "file2", "file3"]);
```

will display the following message:

```
Hello Alex! You are 30 years old with flag = True
Arg[0]: file1
Arg[1]: file2
Arg[2]: file3
```

Most of the time, you might want to declare an async function:

```csharp
var app = new CommandApp()
{
    {"v", v => ++verbose},
    {"name=|value=", v => Console.WriteLine(v)},
    async (arguments) => { /* other code here */ }
};
```

The same applies to sub-commands.

## Completions

`CommandApp` can provide completion candidates for a partially typed command line:

```csharp
var candidates = commandApp.GetCompletions("hello --na"); // -> ["--name"]
```

You can also complete from a pre-tokenized command line (useful for shells that already provide tokens):

```csharp
var candidates = commandApp.GetCompletionsForTokens(["myexe", "hello", "--na"], tokenIndex: 2); // -> ["--name"]
```

Value completions (optional):

```csharp
var app = new CommandApp("myexe")
{
    { "c|color=", "Console {COLOR}", v => {} },
    { "<file>", "Input {FILE}", v => {} },
};

app.Options["color"].ValueCompleter = static (_, prefix) => ["red", "green", "blue"];
app.Arguments[0].ValueCompleter = static (_, prefix) => ["README.md", "src/"];
```

To expose completions from a CLI, add `CompletionCommands` (it adds `completion <shell>` and a hidden `__complete` command):

```csharp
var commandApp = new CommandApp("myexe")
{
    new CompletionCommands(),
    // ... your commands/options/actions ...
};
```

Generate and install a script for your shell:

```console
# Bash (current session)
eval "$(myexe completion bash)"       # if on PATH
eval "$(./myexe completion bash)"     # if in current directory
eval "$(./myexe.exe completion bash)" # on Windows (Git Bash / MSYS)

# Zsh (current session)
source <(myexe completion zsh)

# Fish (current session)
myexe completion fish | source

# PowerShell (current session)
myexe completion powershell | Out-String | Invoke-Expression
```

Notes:
- The completion glue scripts call the hidden `__complete` subcommand and expect one candidate per line on stdout.
  - Token mode (preferred when supported by the shell): `myexe __complete --command-name <NAME> --index <TOKEN_INDEX> --token <TOKEN> --token <TOKEN> ...`
  - Line mode (fallback): `myexe __complete --command-name <NAME> --line <LINE> --cursor <POS>`
- Value completion: you can provide value candidates for a specific option/argument by setting `ValueCompleter` on the declared node (option or argument).
- On PowerShell, the generated script invokes the current executable path (or `dotnet <entry-assembly.dll>` when hosted by `dotnet`) because the current directory is not searched by default.
- Completion is non-executing: it does not invoke user option actions; it only inspects the declared command tree.

## Configuration

`CommandRunConfig` controls how output and help formatting work at runtime:

```csharp
var config = new CommandRunConfig(Width: 120, OptionWidth: 32)
{
    Out = Console.Out,
    Error = Console.Error,
    ShowLicenseOnRun = true,
};

await app.RunAsync(args, config);
```

`CommandConfig` controls application-level behaviors:

- `CommandConfig.Localizer` lets you localize all built-in help/error text (it is applied before writing to `Out`/`Error`), for example:
  ```csharp
  var app = new CommandApp("myexe", config: new CommandConfig
  {
      Localizer = s => s, // replace with your localization
  });
  ```
- `CommandConfig.StrictOptionParsing` (default: `true`) makes unknown `-` / `--` option-like tokens (e.g. `--unknown`) fail early as an error instead of being treated as positional arguments.
  - This does not apply to `/`-prefixed tokens, to allow POSIX-style absolute paths like `/mnt/home` to be passed as positional arguments.
  - Use `--` to pass values that start with `-` (e.g. `myexe -- -5`).
- `CommandConfig.OutputFactory` lets you replace how library output is rendered (help/errors/version/license). The factory receives the effective `CommandRunConfig` so renderers can use the configured `Out`/`Error` writers.
- `CommandApp.LicenseHeader` (combined with `CommandRunConfig.ShowLicenseOnRun`) lets you print a license banner once before executing the selected command.

## ArgumentSource

The `ArgumentSource` class allows to define a source of arguments that can be used to inject more arguments.

One implementation provided is the `ResponseFileSource` that allows to read arguments from a file.

```csharp
var app = new CommandApp("myexe")
{
    "Options:",
    new HelpOption(),
    new ResponseFileSource(),
    (arguments) => { /* other code here */ }
};
await app.RunAsync(["--help"]);
```

will display the following message:

```
Usage: myexe [options]
Options:
  -h, -?, --help             Show this message and exit
  @file                      Read response file for more options.
```

If you pass a response file via the syntax `@responsefile.txt`, the content of the file will be read and the arguments will be injected in the command-line:

```
// Read lines from file.txt and inject arguments there
await app.RunAsync(["@file.txt"]);
```

Response file parsing supports:
- Whitespace separation (spaces/tabs)
- Single and double quotes
- `#` comments (when `#` is the first non-whitespace character on a line, or after a completed token)
- Basic `\` escaping on non-Windows platforms (e.g. `c\ d` -> `c d`), while keeping `\` as a literal character on Windows (so paths like `C:\Temp\file.txt` are preserved).

Quick examples:

| Response file line | Produces tokens |
|---|---|
| `--name John` | `--name`, `John` |
| `"hello world"` | `hello world` |
| `# comment` | *(no tokens)* |
| `c\\ d` (non-Windows) | `c d` |
| `C:\Temp\file.txt` (Windows) | `C:\Temp\file.txt` |

## CommandGroup

`CommandGroup` are a special kind of nodes that can contain any other nodes (commands, options, text, actions...). They are used to group commands/options together, but more importantly, they can be used to declare when they are active based on a function callback.

For example, the following code declare a command group that is not visible by default, unless you pass the `--advanced` option:

```csharp
bool advanced = false;
var app = new CommandApp()
{
    "Options:",
    { "advanced", "Activate advanced options", v => advanced = v != null },
    new HelpOption(),
    new CommandGroup(() => advanced)
    {
        "Advanced Options:",
        { "special1", "This is a special option 1", v => {} },
        { "special2", "This is a special option 2" , v => {} },
    },
};
await app.RunAsync(["--help"]);
```

will display the following message:

```
Usage: HelloWorld [options]
Options:
      --advanced             Activate advanced options
  -h, -?, --help             Show this message and exit
```

But if we pass the `--advanced` option:

```csharp
await app.RunAsync(["--advanced", "--help"]);
```
It will display the following:

```
Usage: HelloWorld [options]
Options:
      --advanced             Activate advanced options
  -h, -?, --help             Show this message and exit
Advanced Options:
      --special1             This is a special option 1
      --special2             This is a special option 2
```

Not only the text is not displayed, but the options `--special1` and `--special2` are not available unless the `--advanced` option is passed.

## Performance and Benchmarks

The parser is optimized for minimal allocations on hot paths (e.g. avoiding regex parsing and avoiding per-option `string.Split` arrays).

This repository includes a BenchmarkDotNet project to validate parsing speed:

```console
dotnet run -c Release --project src/XenoAtom.CommandLine.Benchmarks
```

## Going further

You can have a look at the [samples](https://github.com/XenoAtom/XenoAtom.CommandLine/tree/main/samples) to see more examples of how to use the library.

## Class diagram

The following class diagram shows the main classes of the library:

![Class diagram](XenoAtom.CommandLine.png)

The design of the library is voluntarily simple and straightforward. The main classes are `CommandApp`, `Command`, `Option`, `CommandArgument`, `CommandGroup`, `ArgumentSource`, `HelpOption`, `VersionOption`, `ResponseFileSource` and `CommandException`. The `Action` is a delegate that can be used to execute code when the command-line is parsed.

Other class derived from `Option` (for representing actions bound to options) are internal.

The `CommandContainer` derived classes (`Command`, `CommandApp`, `CommandGroup`) provide several [extension methods](https://github.com/XenoAtom/XenoAtom.CommandLine/blob/main/src/XenoAtom.CommandLine/CommandExtensions.cs) to add options and command arguments that are compatible with collection initializers:

```csharp
var app = new CommandApp();
app.Add("n|name=", "The {NAME} of the person", v => name = v);
```

is equivalent to use the collection initializer:

```csharp
var app = new CommandApp()
{
    { "n|name=", "The {NAME} of the person", v => name = v },
};
```
