using System.Globalization;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class CommandOutputTests
{
    [TestMethod]
    public async Task OutputFactory_IsInvokedOnce_AndReusedForSubCommandHelp()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        RecordingOutput? output = null;
        var factoryCount = 0;
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ =>
                {
                    factoryCount++;
                    output ??= new RecordingOutput();
                    return output;
                }
            })
        {
            new Command("child")
            {
                new HelpOption(),
                (ctx, _) => ValueTask.FromResult(0)
            },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var writer = new StringWriter();
        var result = await app.RunAsync(["child", "--help"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.AreEqual(1, factoryCount);
        Assert.IsNotNull(output);
        Assert.AreEqual(1, output.HelpCalls);
        Assert.AreEqual("app child", output.HelpCommands[0]);
    }

    [TestMethod]
    public async Task OutputFactory_IsResolved_AfterOptionParsing_ForHelp()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var useVisualHelp = false;
        var defaultOutput = new RecordingOutput();
        var visualOutput = new RecordingOutput();
        var factoryCount = 0;

        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ =>
                {
                    factoryCount++;
                    return useVisualHelp ? visualOutput : defaultOutput;
                }
            })
        {
            { "visual-help", "Enable visual help output", value => useVisualHelp = value is not null },
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var writer = new StringWriter();
        var result = await app.RunAsync(["--visual-help", "--help"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.AreEqual(1, factoryCount);
        Assert.AreEqual(0, defaultOutput.HelpCalls);
        Assert.AreEqual(1, visualOutput.HelpCalls);
        Assert.AreEqual("app", visualOutput.HelpCommands[0]);
    }

    [TestMethod]
    public async Task VersionAndLicense_AreRoutedThroughOutput()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var output = new RecordingOutput();
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => output
            })
        {
            new VersionOption("1.2.3"),
            (ctx, _) => ValueTask.FromResult(0)
        };
        app.LicenseHeader = () => "LICENSE";

        var writer = new StringWriter();
        var runConfig = new CommandRunConfig { Out = writer, Error = writer };

        var result = await app.RunAsync(["--version"], runConfig);
        Assert.AreEqual(0, result);
        Assert.AreEqual(1, output.VersionCalls);
        Assert.AreEqual("1.2.3", output.Versions[0]);
        Assert.AreEqual(0, output.LicenseCalls);

        result = await app.RunAsync(Array.Empty<string>(), runConfig);
        Assert.AreEqual(0, result);
        Assert.AreEqual(1, output.LicenseCalls);
        Assert.AreEqual("LICENSE", output.LicenseHeaders[0]);
    }

    [TestMethod]
    public async Task UnknownSubCommand_UsesUnknownTokenOutput_WithSuggestionsAndSpan()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var output = new RecordingOutput();
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => output
            })
        {
            new Command("hello")
            {
                (ctx, _) => ValueTask.FromResult(0)
            },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var writer = new StringWriter();
        var result = await app.RunAsync(["he"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.AreEqual(1, output.UnknownCalls);
        Assert.AreEqual(UnknownTokenKind.UnknownCommandOrOption, output.UnknownKinds[0]);
        Assert.HasCount(1, output.UnknownTokens[0]);
        CollectionAssert.AreEqual(new[] { "he" }, output.UnknownInvocationTokens[0]!.ToArray());
        var unknown = output.UnknownTokens[0][0];
        Assert.AreEqual("he", unknown.Token);
        CollectionAssert.Contains(unknown.Suggestions.ToArray(), "hello");
        Assert.IsNotNull(unknown.TokenSpan);
        Assert.AreEqual(0, unknown.TokenSpan.Value.TokenIndex);
    }

    [TestMethod]
    public async Task OptionParseFailure_ProvidesDiagnosticToOutput()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var output = new RecordingOutput();
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => output
            })
        {
            { "a|age=", "Age", (int _) => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var writer = new StringWriter();
        var result = await app.RunAsync(["--age", "oops"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.AreEqual(1, output.ErrorCalls);
        Assert.IsInstanceOfType<CommandOptionException>(output.Errors[0]);
        Assert.IsNotNull(output.Errors[0].Diagnostic);
        var diagnostic = output.Errors[0].Diagnostic!.Value;
        Assert.AreEqual(CommandDiagnosticSource.CommandLine, diagnostic.Source);
        Assert.IsInstanceOfType<Option>(diagnostic.Node);
        Assert.IsNotNull(diagnostic.Tokens);
        Assert.HasCount(2, diagnostic.Tokens);
        Assert.IsNotNull(diagnostic.TokenSpan);
        Assert.AreEqual(1, diagnostic.TokenSpan.Value.TokenIndex);
    }

    [TestMethod]
    public void ShowHelp_Overload_UsesProvidedOutput()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var factoryOutput = new RecordingOutput();
        var overrideOutput = new RecordingOutput();
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                OutputFactory = _ => factoryOutput
            })
        {
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };
        app.LicenseHeader = () => "LICENSE";

        app.ShowHelp(overrideOutput, new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.AreEqual(0, factoryOutput.HelpCalls);
        Assert.AreEqual(0, factoryOutput.LicenseCalls);
        Assert.AreEqual(1, overrideOutput.HelpCalls);
        Assert.AreEqual(1, overrideOutput.LicenseCalls);
    }

    private sealed class RecordingOutput : ICommandOutput
    {
        public int HelpCalls;
        public int ErrorCalls;
        public int UnknownCalls;
        public int VersionCalls;
        public int LicenseCalls;
        public readonly List<string> HelpCommands = new();
        public readonly List<CommandException> Errors = new();
        public readonly List<UnknownTokenKind> UnknownKinds = new();
        public readonly List<IReadOnlyList<UnknownTokenInfo>> UnknownTokens = new();
        public readonly List<IReadOnlyList<string>?> UnknownInvocationTokens = new();
        public readonly List<string> Versions = new();
        public readonly List<string> LicenseHeaders = new();

        public void WriteHelp(Command command, CommandRunConfig runConfig)
        {
            HelpCalls++;
            HelpCommands.Add(command.GetFullCommandPath());
        }

        public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
        {
            ErrorCalls++;
            Errors.Add(exception);
        }

        public void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenReport report)
        {
            UnknownCalls++;
            UnknownKinds.Add(report.Kind);
            UnknownTokens.Add(report.UnknownTokens.ToArray());
            UnknownInvocationTokens.Add(report.InvocationTokens is null ? null : report.InvocationTokens.ToArray());
        }

        public void WriteVersion(Command command, CommandRunConfig runConfig, string version)
        {
            VersionCalls++;
            Versions.Add(version);
        }

        public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText)
        {
            LicenseCalls++;
            LicenseHeaders.Add(licenseText);
        }
    }
}
