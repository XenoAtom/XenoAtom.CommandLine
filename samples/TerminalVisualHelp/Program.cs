using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;
using TerminalHost = XenoAtom.Terminal.Terminal;

const string _ = "";
var showVisualHelp = false;
var files = new List<string>();
string? name = null;
int age = 0;

using var session = TerminalHost.Open();

CommandApp? app = null;
app = new CommandApp(
    "terminal-help",
    config: new CommandConfig
    {
        OutputFactory = _ =>
        {
            if (showVisualHelp)
            {
                return new TerminalVisualCommandOutput();
            }

            return new TerminalMarkupCommandOutput();
        }
    })
{
    new CommandUsage(),
    _,
    "Options:",
    { "visual-help", "Render help by calling ToHelpVisual()", value =>
        {
            showVisualHelp = value is not null;
        }
    },
    new HelpOption(),
    new VersionOption("1.0.0"),
    _,
    "Available commands:",
    new Command("hello", "Greets someone")
    {
        _,
        "Options:",
        { "n|name=", "The {NAME} to greet", value => name = value },
        { "a|age=", "The {AGE}", (int value) => age = value },
        new HelpOption(),
        _,
        "Arguments:",
        { "<files>*", "Input files", files },
        (ctx, _) =>
        {
            ctx.Out.WriteLine($"Hello {name ?? "unknown"} (age={age})");
            foreach (var file in files)
            {
                ctx.Out.WriteLine($"- {file}");
            }

            return ValueTask.FromResult(0);
        }
    },
    (ctx, _) =>
    {
        if (showVisualHelp && app is not null)
        {
            var helpVisual = new Group(
                new Markup("[bold]terminal-help[/]"),
                app.ToHelpVisual(new TerminalHelpVisualOptions
                {
                    TableStyleOverride = TableStyle.Minimal with { ShowHeaderSeparator = false },
                }));
            TerminalHost.Write(helpVisual);
            return ValueTask.FromResult(0);
        }

        ctx.Out.WriteLine("Run with --help or --visual-help.");
        return ValueTask.FromResult(0);
    }
};

return await app.RunAsync(args);
