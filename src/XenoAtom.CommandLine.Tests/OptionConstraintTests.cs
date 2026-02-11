using System.Globalization;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class OptionConstraintTests
{
    [TestMethod]
    public async Task MutuallyExclusiveConstraint_ReturnsError()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            { "j|json", "Output JSON", _ => { } },
            { "x|xml", "Output XML", _ => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };
        app.AddMutuallyExclusive("json", "xml");

        var result = await app.RunAsync(["--json", "--xml"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("cannot be used together", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task RequiresConstraint_ReturnsError()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            { "u|user=", "User", _ => { } },
            { "p|password=", "Password", _ => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };
        app.AddRequires("password", "user");

        var result = await app.RunAsync(["--password", "secret"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("requires", StringComparison.Ordinal));
        Assert.IsTrue(output.Contains("--user", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Constraints_SkipInactiveOptions()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        bool advanced = false;
        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            { "a|advanced", "Enable advanced options", v => advanced = v != null },
            { "j|json", "Output JSON", _ => { } },
            new CommandGroup(() => advanced)
            {
                { "x|xml", "Output XML", _ => { } }
            },
            (ctx, _) => ValueTask.FromResult(0)
        };
        app.AddMutuallyExclusive("json", "xml");

        var result = await app.RunAsync(["--json"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task Constraints_IncludeEnvironmentFallbackValues()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = name => name == "APP_XML" ? "true" : null
            });
        app.Add("j|json", "Output JSON", _ => { });
        app.Add("x|xml", "Output XML", _ => { }, envVar: "APP_XML");
        app.AddMutuallyExclusive("json", "xml");
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(["--json"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("cannot be used together", StringComparison.Ordinal));
    }
}

