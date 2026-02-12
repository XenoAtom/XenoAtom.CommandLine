using System.Globalization;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using TerminalHost = XenoAtom.Terminal.Terminal;

namespace XenoAtom.CommandLine.Terminal.Tests;

[TestClass]
public sealed class TerminalMarkupCommandOutputTests
{
    [TestMethod]
    public async Task Help_IsRendered_WithMarkupOutput()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => new TerminalMarkupCommandOutput(new TerminalMarkupOutputOptions
                {
                    UseTerminalWindowWidth = false,
                })
            });
        app.Add(new CommandUsage());
        app.Add("Options:");
        app.Add("n|name=", "The {NAME}", _ => { }, envVar: "APP_NAME", hidden: false);
        app.Add(new HelpOption());
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(["--help"], new CommandRunConfig(Width: 80));
        Assert.AreEqual(0, result);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Usage: app [options]");
        StringAssert.Contains(output, "-n, --name=NAME");
        StringAssert.Contains(output, "[env: APP_NAME]");
    }

    [TestMethod]
    public async Task Error_UsesDiagnosticUnderline_WhenAvailable()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => new TerminalMarkupCommandOutput()
            })
        {
            { "a|age=", "Age", (int _) => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--age", "oops"]);
        Assert.AreEqual(1, result);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Error:");
        StringAssert.Contains(output, "app --age oops");
        StringAssert.Contains(output, "^");
        StringAssert.Contains(output, "Use `app --help` for usage.");
    }

    [TestMethod]
    public async Task UnknownToken_UsesSuggestions_AndInvocationTokens()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var invocationTokens = new[] { "--verbos" };
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => new TerminalMarkupCommandOutput(
                    new TerminalMarkupOutputOptions
                    {
                        UseTerminalWindowWidth = false,
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
    }

    [TestMethod]
    public async Task Error_ContainsEnvironmentVariableSourceContext()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = _ => "oops",
                OutputFactory = _ => new TerminalMarkupCommandOutput()
            });
        app.Add("a|age=", "Age", (int _) => { }, envVar: "APP_AGE");
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync([]);
        Assert.AreEqual(1, result);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "(in environment variable 'APP_AGE')");
        StringAssert.Contains(output, "Error");
    }

    [TestMethod]
    public async Task Help_DoesNotRenderHiddenOptionsArgumentsAndCommands()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var hiddenOptionDescription = "Hidden option description";
        var hiddenArgumentDescription = "Hidden argument description";
        var hiddenCommandDescription = "Hidden command description";

        var hiddenCommand = new Command("hidden-cmd", hiddenCommandDescription)
        {
            Hidden = true,
        };
        hiddenCommand.Add((ctx, _) => ValueTask.FromResult(0));

        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => new TerminalMarkupCommandOutput()
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
