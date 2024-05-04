# XenoAtom.CommandLine User Guide

XenoAtom.CommandLine is a library that provides a simple and easy way to create command-line applications in .NET. It is a fork of the Mono.Options library with some modifications and improvements.

- [CommandApp and Command](#commandapp-and-command)
- [Options](#options)
- [Help Text](#help-text)
- [Actions](#actions)
- [ArgumentSource](#argumentsource)
- [CommandGroup](#commandgroup)
- [Going further](#going-further)

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

The `CommandApp` class inherits from `Command` and benefiting from using list initializers to add options, text and other commands to the application. This makes it easy to add options to the application in a readable and concise way.


```csharp
using XenoAtom.CommandLine;

const string _ = "";
bool flag = false;
string? name = null;
int age = 0;

var commandApp = new CommandApp()
{
    new CommandUsage("Usage: {NAME} [Options] [files]+"),
    _,
    "Options:",
    {"f|flag", "This is a flag", v => flag = v != null }, 
    {"n|name=", "Your {NAME}", v => name = v},
    {"a|age=", "Your {AGE}", (int v) => age = v},
    new HelpOption(),
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

await commandApp.RunAsync(args);
```

## Options

An option is composed of:

A prototype that defines the option syntax. (e.g . "o|output="))

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

Option processing is disabled by specifying `--`.  All options after `--`
are passed to the arguments unchanged and unprocessed.

Examples:

```c#
int verbose = 0;
var app = new CommandApp()
{
    {"v", v => ++verbose},
    {"name=|value=", v => Console.WriteLine(v))},
    (arguments) => { /* other code here */ }
};
await app.RunAsync(["-v", "--v", "/v", "-name=A", "/name", "B", "extra"]);
```

The above would parse the argument string array, and would invoke the
lambda expression three times, setting `verbose` to 3 when complete.  
It would also print out "A" and "B" to standard output.
The returned array in `arguments` would contain the string "extra".

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
     await p.RunAsync(["-a"]);    // sets v != null
     await p.RunAsync(["-a+"]);   // sets v != null
     await p.RunAsync(["-a-"]);   // sets v == null
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
  Usage: HelloWorld [Options]
    -n, --name=NAME            Your NAME
    -a, --age=AGE              Your AGE
    -h, -?, --help             Show this message and exit
  ```
- You can also create pair of key/values (like macros):
  ```csharp
  var app = new CommandApp()
  {
      { "D:", "Add a marco {0:NAME} and optional {1:VALUE}", (k, v) => Console.WriteLin($"Macro:    {k}` => `{v}`") },
      { "I|macro=", "Add a marco {0:NAME} and required {1:VALUE}", (k, v) => Console.WriteLine  ($"Required Macro: `{k}` => `{v}`") },
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
      { "<>", "files", otherArguments},
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
  Notice the usage of the special option/argument `<>` that will collect all the arguments instead of collecting them to the default arguments passed to the action on the command.
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

There is a special kind of text called `CommandUsage` that will be displayed at the beginning of the help message. It is used to display the usage of the command.

```csharp
var app = new CommandApp() {
    new CommandUsage("Usage: {NAME} [Options] [files]+"),
    "Available commands:",
    // ...  
    new CommandUsage("Usage: {NAME} [--advanced] [Advanced Options]"),
    // 
};
await app.RunAsync(args);
```
You can have multiple `CommandUsage` in a `CommandApp` or a `Command`. If no command usages are found, it will display a default one as the first line of the help message, otherwise it will display the `CommandUsage` that are defined.

## Actions

A `CommandApp` and a `Command` are meant to be executed. You can add a single action to a `CommandApp` or a `Command` that will be executed when the command-line after the options and arguments are parsed.

For example, the following code:

```csharp
const string _ = "";
bool flag = false;
string? name = null;
int age = 0;

var commandApp = new CommandApp()
{
    new CommandUsage("Usage: {NAME} [Options] [files]+"),
    _,
    "Options:",
    {"f|flag", "This is a flag", v => flag = v != null },
    {"n|name=", "Your {NAME}", v => name = v},
    {"a|age=", "Your {AGE}", (int v) => age = v},
    new HelpOption(),
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
    {"name=|value=", v => Console.WriteLine(v))},
    async (arguments) => { /* other code here */ }
};
```

The same applies to sub-commands.

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
Usage: myexe [Options]
Options:
  -h, -?, --help             Show this message and exit
  @file                      Read response file for more options.
```

If you pass a response file via the syntax `@responsefile.txt`, the content of the file will be read and the arguments will be injected in the command-line:

```
// Read lines from file.txt and inject arguments there
await app.RunAsync(["@file.txt"]);
```

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
Usage: HelloWorld [Options]
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
Usage: HelloWorld [Options]
Options:
      --advanced             Activate advanced options
  -h, -?, --help             Show this message and exit
Advanced Options:
      --special1             This is a special option 1
      --special2             This is a special option 2
```

Not only the text is not displayed, but the options `--special1` and `--special2` are not available unless the `--advanced` option is passed.

## Going further

You can have a look at the [samples](https://github.com/XenoAtom/XenoAtom.CommandLine/tree/main/samples) to see more examples of how to use the library.