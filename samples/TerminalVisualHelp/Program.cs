using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Styling;
using TerminalHost = XenoAtom.Terminal.Terminal;

const string _ = "";
var showVisual = false;
var showMarkup = false;
var files = new List<string>();
string? name = null;
int age = 0;

CommandApp? app = null;
app = new CommandApp(
    "terminal-help",
    config: new CommandConfig
    {
        OutputFactory = _ =>
        {
            if (showVisual)
            {
                return new TerminalVisualCommandOutput(new TerminalVisualOutputOptions
                {
                    SectionGroupMinWidth = 70,
                    ErrorGroupMinWidth = 70,
                });
            }

            if (showMarkup)
            {
                return new TerminalMarkupCommandOutput();
            }

            return DefaultCommandOutput.Instance;
        }
    })
{
    new CommandUsage(),
    CreateBanner(),
    _,
    "Options:",
    { "markup", "Render help/errors with terminal markup", value => showMarkup = value is not null },
    { "visual|visual-help", "Render help by calling ToHelpVisual()", value => showVisual = value is not null },
    new HelpOption(),
    new VersionOption("1.0.0"),
    _,
    "Available commands:",
    new Command("hello", "Greets someone")
    {
        new CommandUsage(),
        CreateBanner(),
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
        if (showVisual && app is not null)
        {
            var helpVisual = new Group(
                new Markup("[bold]terminal-help[/]"),
                app.ToHelpVisual(new TerminalVisualOutputOptions
                {
                    SectionGroupMinWidth = 70,
                    TableStyleOverride = TableStyle.Minimal with { ShowHeaderSeparator = false },
                }));
            TerminalHost.Write(helpVisual);
            return ValueTask.FromResult(0);
        }

        ctx.Out.WriteLine("Run with --help, --markup --help, or --visual --help.");
        return ValueTask.FromResult(0);
    }
};

return await app.RunAsync(args);

static TextFiglet CreateBanner()
{
    var gradientBrush = Brush.LinearGradient(
        new GradientPoint(0f, 0f),
        new GradientPoint(1f, 0f),
        [
            new GradientStop(0f, Colors.DodgerBlue),
            new GradientStop(0.5f, Colors.White),
            new GradientStop(1f, Colors.Orange),
        ],
        mixSpaceOverride: ColorMixSpace.Oklab);

    return new TextFiglet("XenoAtom")
        .Font(FigletPredefinedFont.Standard)
        .LetterSpacing(1)
        .TextAlignment(TextAlignment.Left)
        .Style(TextFigletStyle.Default with { ForegroundBrush = gradientBrush });
}
