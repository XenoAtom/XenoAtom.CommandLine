using System.Globalization;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class CommandLineTests : VerifyBase
{
    [TestMethod]
    [DataRow(0, "--help")]
    [DataRow(0, "--name", "John", "-a50" )]
    [DataRow(0, "--name", "John", "-a50", "-DHello", "-DWorld=126")]
    [DataRow(0, "commit", "--help")]
    [DataRow(0, "--name", "John", "-a50", "commit", "--message", "Hello!", "--message", "World!")]
    public async Task TestHelloWorld(int result, params string[] args)
    {
        const string _ = "";
        string? name = null;
        int age = 0;
        List<(string, string?)> keyValues = new List<(string, string?)>();
        List<string> messages = new List<string>();

        var commandApp = new CommandApp("myexe")
        {
            _,
            {"D:", "Defines a {0:name} and optional {1:value}", (key, value) =>
            {
                if (key is null) throw new OptionException("The key is mandatory for a define", "D");
                keyValues.Add((key, value));
            }},
            {"n|name=", "Your {NAME}", v => name = v},
            {"a|age=", "Your {AGE}", (int v) => age = v},
            new HelpOption(),
            _,
            "Available commands:",
            new Command("commit")
            {
                _,
                {"m|message=", "Add a {MESSAGE} to this commit", messages},
                new HelpOption(),

                // Action for the commit command
                (ctx, arguments) =>
                {
                    ctx.Out.WriteLine($"Committing with name={name}, age={age}");
                    foreach (var message in messages)
                    {
                        ctx.Out.WriteLine($"Commit message: {message}");
                    }
                    return ValueTask.FromResult(0);
                }
            },
            // Default action if no command is specified
            (ctx, _) =>
            {
                ctx.Out.WriteLine($"Hello {name}! You are {age} years old.");
                if (keyValues.Count > 0)
                {
                    foreach (var keyValue in keyValues)
                    {
                        ctx.Out.WriteLine($"Define: {keyValue.Item1} => {keyValue.Item2}");
                    }
                }

                return ValueTask.FromResult(0);
            }
        };

        await VerifyCommand(commandApp, args, result);
    }
    
    [TestMethod]
    [DataRow(0)]
    [DataRow(1, "-w")]
    [DataRow(1, "--wonder")]
    [DataRow(0, "-t")]
    [DataRow(0, "-t", "-x")]
    [DataRow(0, "-txc")]
    [DataRow(0, "-DHELLO")]
    [DataRow(0, "-DHELLO", "-DTEST=WORLD")]
    [DataRow(0, "-f", "test1.txt")]
    [DataRow(0, "-f", "test1.txt", "-f", "test2.txt")]
    [DataRow(0, "-f=")]
    [DataRow(1, "-f")]
    [DataRow(1, "file1.txt")]
    [DataRow(0, "-v")]
    [DataRow(0, "--version")]
    [DataRow(0, "-h")]
    [DataRow(0, "/?")]
    [DataRow(0, "-a", "--help")]
    [DataRow(0, "--advanced", "--help")]
    [DataRow(1, "--special1")]
    [DataRow(0, "-a", "--special1", "--special2")]
    [DataRow(0, "-a", "chroot", "--help")]
    [DataRow(0, "-a", "chroot", "1", "2", "3")]
    [DataRow(0, "-a", "chroot", "-i", "-i+", "-i-", "1", "2", "3")]
    [DataRow(0, "-a", "chroot", "-M", "name", "value")]
    [DataRow(0, "-a", "chroot", "-P", "name1->value2")]
    [DataRow(1, "-a", "chroot", "-M", "name")]
    [DataRow(0, "--help")]
    [DataRow(0, "hello")]
    [DataRow(0, "hello", "--name", "foo", "--age", "15")]
    [DataRow(0, "hello", "--name", "foo", "--", "--age", "15")]
    [DataRow(1, "hello", "--age", "invalid")]
    [DataRow(0, "hello", "file1.txt")]
    [DataRow(0, "hello", "file1.txt", "file2.txt")]
    [DataRow(0, "hello", "--help")]
    [DataRow(0, "world", "--help")]
    [DataRow(0, "world", "-eValue1", "--enum", "Value2", "--enum=Value3", "--enum:Value1")]
    [DataRow(1, "world", "--enum", "Value4")]
    [DataRow(0, "world", "1", "2", "-e", "Value1", "-e=Value2", "3")]
    [DataRow(0, "world", "@world.txt")]
    public async Task Test(int result, params string[] args)
    {
        await VerifyCommand(GetDefaultCommandApp(), args, result);
    }
    
    private static CommandApp GetDefaultCommandApp()
    {
        const string _ = "";
        bool showAdvanced = false;
        var enums = new List<TestEnum>();
        var keyValues = new List<(string Key, string? Value)>();
        var files = new List<string>();
        bool extract = false;
        bool create = false;
        bool list = false;
        bool special1 = false;
        bool special2 = false;
        int specialIncrease = 0;
        int specialDecrease = 0;

        var hello_files = new List<string>();

        string? name = null;
        int age = 0;

        var app = new CommandApp("multi")
        {
            new CommandUsage(),
            _,
            "Options:",
            // gcc-like options
            { "D:", "Add a marco {0:NAME} and optional {1:VALUE}", (k, v) => keyValues.Add((k, v)) },

            // tar-like options
            { "f=", "The input {FILE}", files },
            { "x", "Extract the file", v => extract = v != null },
            { "c", "Create the file", v => create = v != null },
            { "t", "List the file", v => list = v != null },
            { "a|advanced", "Show advanced options", v => showAdvanced = v != null },
            // others
            new HelpOption(),
            new VersionOption("1.2.3"),
            // A group of options only shown when --advanced is specified
            new CommandGroup(() => showAdvanced)
            {
                _,
                "Advanced Options:",
                { "special1", "This is a special option 1", v => special1 = v != null },
                { "special2", "This is a special option 2",  v => special2 = v != null },
            },
            _,
            "Available commands:",
            new Command("hello", "This is a hello command")
            {
                _,
                "Options:",
                { "n|name=", "This is a name", v => { name = v; } },
                { "a|age=", "Sets the {AGE}", (int v) => age = v > 200 ? throw new ArgumentException("Age must be <= 200") : v },
                new HelpOption(),
                {"<>", "[files]*", hello_files},

                (ctx, _) =>
                {
                    ctx.Out.WriteLine($"Hello name={name}, age={age}");
                    foreach (var file in hello_files)
                    {
                        ctx.Out.WriteLine($"Hello File {file}");
                    }
                    return ValueTask.FromResult(age > 100 ? 1 : 0);
                }
            },
            new Command("world", "This is a world command")
            {
                new CommandUsage("Usage: {{NAME}} [Options] [files]* @file"),
                _,
                "Options:",
                { "e|enum=", $"This is an {{ENUM}} accepting the following values: {EnumWrapper<TestEnum>.Names}", (EnumWrapper<TestEnum> v) => { enums.Add(v); } },
                new HelpOption(),
                new ResponseFileSource(),
                
                (ctx,arguments) =>
                {
                    foreach (var file in arguments)
                    {
                        ctx.Out.WriteLine($"World {file}");
                    }

                    foreach (var enumValue in enums)
                    {
                        ctx.Out.WriteLine($"World Enum {enumValue}");
                    }
                    return ValueTask.FromResult(0);
                }
            },
            // A group of options only shown when --advanced is specified
            new CommandGroup(() => showAdvanced)
            {
                _,
                "Advanced Commands:",
                new Command("chroot", "This is an advanced command 1")
                {
                    _,
                    "Options:",
                    { "M=", "Add a marco {0:NAME} and mandatory {1:VALUE}", (k, v) => keyValues.Add((k, v)) },
                    { "P={->}", "Add a marco {0:NAME} and mandatory {1:VALUE}", (k, v) => keyValues.Add((k, v)) },
                    { "this-is-a-very-long-option-that-doesnt-fit-on-the-column", "This is the long option", v => {}},
                    {"i", s =>
                        {
                            if (s != null)
                            {
                                specialIncrease++;
                            }
                            else
                            {
                                specialDecrease++;
                            }
                        }
                    },
                    new HelpOption(),
                    (ctx, arguments) =>
                    {
                        ctx.Out.WriteLine($"specialIncrease: {specialIncrease}");
                        ctx.Out.WriteLine($"specialDecrease: {specialDecrease}");

                        foreach (var file in arguments)
                        {
                            ctx.Out.WriteLine($"chroot {file}");
                        }

                        // Macros
                        foreach (var keyValue in keyValues)
                        {
                            ctx.Out.WriteLine($"Macro: {keyValue.Key} => {keyValue.Value}");
                        }

                        return ValueTask.FromResult(0);
                    }
                },

            },
            _,
            new CommandUsage("Run '{NAME} [command] --help' for more information on a command."),

            (ctx,arguments) =>
            {
                ctx.Out.WriteLine($"Extract: {extract}");
                ctx.Out.WriteLine($"Create: {create}");
                ctx.Out.WriteLine($"List: {list}");
                ctx.Out.WriteLine($"Special1: {special1}");
                ctx.Out.WriteLine($"Special2: {special2}");
                ctx.Out.WriteLine($"ShowAdvanced: {showAdvanced}");

                // Files
                foreach (var file in files)
                {
                    ctx.Out.WriteLine($"File: {file}");
                }
                
                // Macros
                foreach (var keyValue in keyValues)
                {
                    ctx.Out.WriteLine($"Macro: {keyValue.Key} => {keyValue.Value}");
                }

                return ValueTask.FromResult(0);
            }
        };

        return app;
    }

    private async Task VerifyCommand(CommandApp app, string[] args, int expectedResult)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        var writer = new StringWriter();
        var result = await app.RunAsync(args, new CommandRunConfig()
        {
            Out = writer,
            Error = writer,
        });

        var settings = new VerifySettings();
        settings.UseDirectory("Verified");
        settings.DisableDiff();
        settings.UseParameters([expectedResult.ToString(CultureInfo.InvariantCulture), ..args]);

        if (result != expectedResult)
        {
            writer.WriteLine($"Expected result: {expectedResult} but received {result}");
        }

        var text = writer.ToString().ReplaceLineEndings("\n");

        await Verify(text, settings);
    }

    private enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }
}
