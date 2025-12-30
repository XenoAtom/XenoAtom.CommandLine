using System.Globalization;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class CommandLineBasicTests
{
    [TestMethod]
    public async Task PassingArgument_WithNoArgumentSpec_ReturnsUsageError()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["a.txt"], new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("Unexpected argument `a.txt`.", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CommandUsage_SyntaxMarker_IsExpanded()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new CommandUsage(),
            { "n|name=", "Name", _ => { } },
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--help"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.IsTrue(writer.ToString().Contains("Usage: app [options]", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task PendingValue_MatchesSubcommandName_IsConsumedAsValue()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        string? name = null;
        var writer = new StringWriter();

        var app = new CommandApp("app")
        {
            { "n|name=", "Name", v => name = v },
            new Command("hello")
            {
                (ctx, _) =>
                {
                    ctx.Out.WriteLine("SUBCOMMAND");
                    return ValueTask.FromResult(0);
                }
            },
            (ctx, args) =>
            {
                ctx.Out.WriteLine($"ROOT name={name}");
                foreach (var arg in args) ctx.Out.WriteLine($"ARG {arg}");
                return ValueTask.FromResult(0);
            }
        };

        var result = await app.RunAsync(["--name", "hello"], new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.AreEqual("hello", name);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("ROOT name=hello", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("SUBCOMMAND", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task PendingValue_ConsumesDoubleDash_AndAllowsFollowingSubcommand()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        string? name = null;
        var writer = new StringWriter();

        var app = new CommandApp("app")
        {
            { "n|name=", "Name", v => name = v },
            new Command("hello")
            {
                (ctx, args) =>
                {
                    ctx.Out.WriteLine($"SUBCOMMAND name={name}");
                    foreach (var arg in args) ctx.Out.WriteLine($"ARG {arg}");
                    return ValueTask.FromResult(0);
                }
            },
            (ctx, _) =>
            {
                ctx.Out.WriteLine($"ROOT name={name}");
                return ValueTask.FromResult(0);
            }
        };

        var result = await app.RunAsync(["--name", "--", "hello"], new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.AreEqual("--", name);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("SUBCOMMAND name=--", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("ROOT name=--", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task DoubleDash_AfterStopOptionParsing_IsPreservedAsArgument()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        List<string> args = new();
        var writer = new StringWriter();

        var app = new CommandApp("app")
        {
            { "<args>*", "Args", args },
            (ctx, a) =>
            {
                return ValueTask.FromResult(0);
            }
        };

        var result = await app.RunAsync(["--", "--", "a"], new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        CollectionAssert.AreEqual(new[] { "--", "a" }, args);
    }

    [TestMethod]
    public async Task InvalidOptionInsideBundle_ThrowsAndShowsBundleToken()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            { "t", "t", _ => { } },
            { "x", "x", _ => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["-txz"], new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("Cannot use unregistered option 'z' in bundle '-txz'.", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BoolSuffix_WorksForLongOptions()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        bool advanced = false;
        var app = new CommandApp("app")
        {
            { "a|advanced", "Advanced", v => advanced = v != null },
            (ctx, _) => ValueTask.FromResult(0)
        };

        await app.RunAsync(["--advanced+"], new CommandRunConfig() { Out = TextWriter.Null, Error = TextWriter.Null });
        Assert.IsTrue(advanced);

        await app.RunAsync(["--advanced-"], new CommandRunConfig() { Out = TextWriter.Null, Error = TextWriter.Null });
        Assert.IsFalse(advanced);
    }

    [TestMethod]
    public async Task OptionalValueOption_WorksWithNoValue_InlineValue_AndSeparatedValue()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var values = new List<string?>();
        var app = new CommandApp("app")
        {
            { "o:", "Optional", v => values.Add(v) },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var config = new CommandRunConfig() { Out = TextWriter.Null, Error = TextWriter.Null };
        await app.RunAsync(["-o"], config);
        await app.RunAsync(["-oVALUE"], config);
        await app.RunAsync(["-o:VALUE2"], config);

        CollectionAssert.AreEqual(new string?[] { null, "VALUE", "VALUE2" }, values);
    }

    [TestMethod]
    public async Task ValueSplitting_RespectsMaxRemainingCount_SingleCharSeparators()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var values = new List<(string Key, string? Value)>();
        var app = new CommandApp("app")
        {
            { "M=", "Macro {0:key} {1:value}", (string k, string? v) => values.Add((k, v)) },
            (ctx, _) => ValueTask.FromResult(0)
        };

        await app.RunAsync(["-M", "KEY", "VALUE:SHOULD_NOT_SPLIT"], new CommandRunConfig() { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.HasCount(1, values);
        Assert.AreEqual("KEY", values[0].Key);
        Assert.AreEqual("VALUE:SHOULD_NOT_SPLIT", values[0].Value);
    }

    [TestMethod]
    public async Task ValueSplitting_RespectsMaxRemainingCount_MultiCharSeparators()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var values = new List<(string Key, string? Value)>();
        var app = new CommandApp("app")
        {
            { "P={->}", "Pair {0:key} {1:value}", (string k, string? v) => values.Add((k, v)) },
            (ctx, _) => ValueTask.FromResult(0)
        };

        await app.RunAsync(["-P", "KEY", "VALUE->SHOULD_NOT_SPLIT"], new CommandRunConfig() { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.HasCount(1, values);
        Assert.AreEqual("KEY", values[0].Key);
        Assert.AreEqual("VALUE->SHOULD_NOT_SPLIT", values[0].Value);
    }

    [TestMethod]
    public async Task UnknownCommand_WhenSubcommandsExist_Returns1()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new Command("known") { (ctx, _) => ValueTask.FromResult(0) }
        };

        var result = await app.RunAsync(["unknown"], new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("Unknown command or option: unknown", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task InactiveSubcommand_IsReportedAsUnknown()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new Command("known", active: () => false) { (ctx, _) => ValueTask.FromResult(0) }
        };

        var result = await app.RunAsync(["known"], new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("Unknown command or option: known", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task MissingAction_WritesUsageHint()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            { "n|name=", "Name", _ => { } }
        };

        var result = await app.RunAsync(Array.Empty<string>(), new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("Use `app --help` for usage.", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task LicenseHeader_IsPrintedWhenRunning()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            LicenseHeader = () => "LICENSE",
            Action = (ctx, _) =>
            {
                ctx.Out.WriteLine("RUN");
                return ValueTask.FromResult(0);
            }
        };

        var result = await app.RunAsync(["--"], new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("LICENSE", StringComparison.Ordinal));
        Assert.IsTrue(output.Contains("RUN", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task StrictOptionParsing_UnknownOption_IsAnError_EvenWhenArgumentsAllowIt()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var args = new List<string?>();
        var writer = new StringWriter();

        var app = new CommandApp("app")
        {
            { "<args>*", "Args", v => args.Add(v) },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--unknown"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("Unknown option: --unknown", StringComparison.Ordinal));
        Assert.HasCount(0, args);
    }

    [TestMethod]
    public async Task StrictOptionParsing_DoubleDash_AllowsOptionLikePositionalArguments()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var args = new List<string?>();
        var writer = new StringWriter();

        var app = new CommandApp("app")
        {
            { "<args>*", "Args", v => args.Add(v) },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--", "--unknown"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.HasCount(1, args);
        Assert.AreEqual("--unknown", args[0]);
    }

    [TestMethod]
    public async Task StrictOptionParsing_DoesNotApplyToSlashPrefixedValues()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var args = new List<string?>();
        var writer = new StringWriter();

        var app = new CommandApp("app")
        {
            { "<args>*", "Args", v => args.Add(v) },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["/mnt/home"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.HasCount(1, args);
        Assert.AreEqual("/mnt/home", args[0]);
    }
}
