using System;
using System.Globalization;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using TerminalHost = XenoAtom.Terminal.Terminal;

namespace XenoAtom.CommandLine.Terminal.Tests;

[TestClass]
public sealed class TerminalVisualNodeTests
{
    [TestMethod]
    public async Task DefaultOutput_RendersInlineVisualNode_AsPreformattedText()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new CommandUsage(),
            { new TextBlock("A  B"), "fallback-banner" },
            "Options:",
            { "n|name=", "The {NAME}", _ => { } },
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--help"], new CommandRunConfig(Width: 80, OptionWidth: 29) { Out = writer, Error = writer });
        Assert.AreEqual(0, result);

        var output = writer.ToString();
        StringAssert.Contains(output, "Usage: app [options]");
        StringAssert.Contains(output, "A  B");
        Assert.IsFalse(output.Contains("fallback-banner", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task DefaultOutput_RendersInlineVisualNode_WhenTerminalIsInitialized()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(120, 40));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new CommandUsage(),
            new TextBlock("VISUAL-NODE"),
            "Options:",
            { "n|name=", "The {NAME}", _ => { } },
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--help"], new CommandRunConfig(Width: 80, OptionWidth: 29) { Out = writer, Error = writer });
        Assert.AreEqual(0, result);

        var output = writer.ToString();
        StringAssert.Contains(output, "Usage: app [options]");
        StringAssert.Contains(output, "VISUAL-NODE");
        StringAssert.Contains(output, "--name=NAME");
    }

    [TestMethod]
    public async Task MarkupOutput_RendersInlineVisualNode_InDeclarationOrder()
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
            })
        {
            new CommandUsage(),
            new TextBlock("VISUAL-NODE"),
            "Options:",
            { "n|name=", "The {NAME}", _ => { } },
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--help"], new CommandRunConfig(Width: 80, OptionWidth: 29));
        Assert.AreEqual(0, result);

        var output = backend.GetOutText();
        var usageIndex = output.IndexOf("Usage: app [options]", StringComparison.Ordinal);
        var visualIndex = output.IndexOf("VISUAL-NODE", StringComparison.Ordinal);
        var optionIndex = output.IndexOf("--name=NAME", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, usageIndex);
        Assert.IsGreaterThan(usageIndex, visualIndex);
        Assert.IsGreaterThan(visualIndex, optionIndex);
    }

    [TestMethod]
    public void ToHelpVisual_EmbedsInlineVisualNodes()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var backend = new InMemoryTerminalBackend(new TerminalSize(100, 30));
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var command = new Command("tool")
        {
            new CommandUsage(),
            new TextBlock("VISUAL-NODE"),
            "Options:",
            { "f|file=", "Input {FILE}", _ => { } },
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var visual = command.ToHelpVisual();
        TerminalHost.Write(visual);

        var output = backend.GetOutText();
        StringAssert.Contains(output, "Usage: tool [options]");
        StringAssert.Contains(output, "VISUAL-NODE");
        StringAssert.Contains(output, "--file=FILE");
    }
}
