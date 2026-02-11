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
                        Help = new TerminalHelpVisualOptions
                        {
                            UseTableForOptions = true,
                            UseTableForArguments = true,
                            UseTableForCommands = true,
                        }
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
        StringAssert.Contains(output, "-n, --name=NAME");
        StringAssert.Contains(output, "Available commands:");
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

        var visual = command.ToHelpVisual(new TerminalHelpVisualOptions
        {
            UseTableForOptions = true,
            UseTableForArguments = true,
            PreserveNodeOrder = true,
        });

        TerminalHost.Write(visual);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Usage: tool [options] <paths>*");
        StringAssert.Contains(output, "Options:");
        StringAssert.Contains(output, "-f, --file=FILE");
        StringAssert.Contains(output, "Arguments:");
        StringAssert.Contains(output, "<paths>*");
    }
}
