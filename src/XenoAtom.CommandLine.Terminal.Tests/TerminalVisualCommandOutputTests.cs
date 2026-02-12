using System;
using System.Globalization;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI;
using TerminalHost = XenoAtom.Terminal.Terminal;

namespace XenoAtom.CommandLine.Terminal.Tests;

[TestClass]
public sealed class TerminalVisualCommandOutputTests
{
    [TestMethod]
    public async Task VisualOutput_RendersHelp_ThroughCommandOutput()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => new TerminalVisualCommandOutput(
                    new TerminalVisualOutputOptions
                    {
                        UseTableForOptions = true,
                        UseTableForArguments = true,
                        UseTableForCommands = true,
                    })
            })
        {
            new CommandUsage(),
            "Options:",
            { "n|name=", "The {NAME}", _ => { } },
            new HelpOption(),
            "Available commands:",
            new Command("hello", "Hello command")
            {
                (ctx, _) => ValueTask.FromResult(0)
            },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--help"]);
        Assert.AreEqual(0, result);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Usage: app [options] <command>");
        StringAssert.Contains(output, "Options");
        StringAssert.Contains(output, "-n, --name=NAME");
        Assert.IsFalse(output.Contains("Options:", StringComparison.Ordinal));
        StringAssert.Contains(output, "Available commands");
        Assert.IsFalse(output.Contains("Available commands:", StringComparison.Ordinal));
        StringAssert.Contains(output, "╭");
        StringAssert.Contains(output, "hello");
    }

    [TestMethod]
    public void ToHelpVisual_CreatesVisual_ForStandaloneRendering()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(100, 30));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var command = new Command("tool")
        {
            new CommandUsage(),
            "Options:",
            { "f|file=", "Input {FILE}", _ => { } },
            new HelpOption(),
            "Arguments:",
            { "<paths>*", "Input paths", new List<string>() },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var visual = command.ToHelpVisual(new TerminalVisualOutputOptions
        {
            UseTableForOptions = true,
            UseTableForArguments = true,
            PreserveNodeOrder = true,
        });

        TerminalHost.Write(visual);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Usage: tool [options] <paths>*");
        StringAssert.Contains(output, "Options");
        Assert.IsFalse(output.Contains("Options:", StringComparison.Ordinal));
        StringAssert.Contains(output, "-f, --file=FILE");
        StringAssert.Contains(output, "Arguments");
        Assert.IsFalse(output.Contains("Arguments:", StringComparison.Ordinal));
        StringAssert.Contains(output, "╭");
        StringAssert.Contains(output, "<paths>*");
    }

    [TestMethod]
    public void ToHelpVisual_SectionGroups_CanUseMinimumWidth()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var command = new Command("tool")
        {
            new CommandUsage(),
            "Options:",
            { "f|file=", "Input {FILE}", _ => { } },
            { "v|verbose", "Verbose output", _ => { } },
            new HelpOption(),
            "Arguments:",
            { "<paths>*", "Input paths", new List<string>() },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var visual = command.ToHelpVisual(new TerminalVisualOutputOptions
        {
            UseTableForOptions = true,
            UseTableForArguments = true,
            PreserveNodeOrder = true,
            UseSectionGroups = true,
            SectionGroupMinWidth = 80,
        });

        TerminalHost.Write(visual);

        var output = backend.GetOutText();
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        string? optionsLine = null;
        string? argumentsLine = null;

        foreach (var line in lines)
        {
            if (line.Contains("Options", StringComparison.Ordinal))
            {
                optionsLine = line;
            }
            else if (line.Contains("Arguments", StringComparison.Ordinal))
            {
                argumentsLine = line;
            }
        }

        Assert.IsNotNull(optionsLine);
        Assert.IsNotNull(argumentsLine);

        var optionsWidth = optionsLine!.TrimEnd().Length;
        var argumentsWidth = argumentsLine!.TrimEnd().Length;

        Assert.IsGreaterThanOrEqualTo(80, optionsWidth);
        Assert.AreEqual(optionsWidth, argumentsWidth);
    }

    [TestMethod]
    public async Task VisualOutput_RendersErrors_InsideGroup()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => new TerminalVisualCommandOutput(
                    new TerminalVisualOutputOptions
                    {
                        UseErrorGroups = true,
                        ErrorGroupMinWidth = 70,
                    })
            })
        {
            { "a|age=", "Age", (int _) => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--age", "oops"]);
        Assert.AreEqual(1, result);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Error");
        StringAssert.Contains(output, "app --age oops");
        StringAssert.Contains(output, "^");
        StringAssert.Contains(output, "Use `app --help` for usage.");
        StringAssert.Contains(output, "╭");
    }

    [TestMethod]
    public async Task VisualOutput_RendersUnknownTokens_InsideGroup()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var invocationTokens = new[] { "--verbos" };
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => new TerminalVisualCommandOutput(
                    new TerminalVisualOutputOptions
                    {
                        UseErrorGroups = true,
                        InvocationTokensProvider = () => invocationTokens,
                    })
            })
        {
            { "v|verbose", "Enable verbose output", _ => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(invocationTokens);
        Assert.AreEqual(1, result);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Unknown option: --verbos");
        StringAssert.Contains(output, "app --verbos");
        StringAssert.Contains(output, "^");
        StringAssert.Contains(output, "╭");
    }

    [TestMethod]
    public async Task VisualOutput_Help_DoesNotRenderHiddenOptionsArgumentsAndCommands()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var hiddenOptionDescription = "Hidden option description";
        var hiddenArgumentDescription = "Hidden argument description";
        var hiddenCommandDescription = "Hidden command description";

        var hiddenCommand = new Command("hidden-cmd", hiddenCommandDescription)
        {
            (ctx, _) => ValueTask.FromResult(0)
        };
        hiddenCommand.Hidden = true;

        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => new TerminalVisualCommandOutput()
            })
        {
            new CommandUsage(),
            "Options:",
            { "v|visible", "Visible option", _ => { } },
            { "hidden-opt=", hiddenOptionDescription, _ => { }, true },
            "Arguments:",
            { "<visibleArg>", "Visible argument", _ => { } },
            { "<hiddenArg>", hiddenArgumentDescription, _ => { }, true },
            "Available commands:",
            new Command("visible-cmd", "Visible command")
            {
                (ctx, _) => ValueTask.FromResult(0)
            },
            hiddenCommand,
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--help"]);
        Assert.AreEqual(0, result);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Visible option");
        StringAssert.Contains(output, "Visible argument");
        StringAssert.Contains(output, "visible-cmd");

        Assert.IsFalse(output.Contains(hiddenOptionDescription, StringComparison.Ordinal));
        Assert.IsFalse(output.Contains(hiddenArgumentDescription, StringComparison.Ordinal));
        Assert.IsFalse(output.Contains(hiddenCommandDescription, StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("--hidden-opt", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("hidden-cmd", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("<hiddenArg>", StringComparison.Ordinal));
    }
}
