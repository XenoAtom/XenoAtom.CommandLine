using System.Globalization;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using TerminalHost = XenoAtom.Terminal.Terminal;

[assembly: DoNotParallelize]

namespace XenoAtom.CommandLine.Terminal.Tests;

[TestClass]
public sealed class MarkupInjectionTests
{
    [TestMethod]
    public async Task Help_EscapesMarkupLikeText()
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
            "Options:",
            { "x=", "Literal [red]tag[/] text", _ => { } },
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--help"]);
        Assert.AreEqual(0, result);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Literal [red]tag[/] text");
    }

    [TestMethod]
    public async Task UnknownToken_EscapesMarkupLikeTokenText()
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
            { "v|verbose", "Verbose output", _ => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--[red]oops[/]"]);
        Assert.AreEqual(1, result);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Unknown option: --[red]oops[/]");
    }
}
