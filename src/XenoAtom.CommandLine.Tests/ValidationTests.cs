using System.Globalization;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class ValidationTests
{
    [TestMethod]
    public async Task OptionValidation_Range_ReturnsError()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app");
        app.Add("p|port=", "Server {PORT}", (int _) => { }, validate: Validate.Range(1, 65535));
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(["--port", "70000"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("Invalid value for option `--port`", StringComparison.Ordinal));
        Assert.IsTrue(output.Contains("between 1 and 65535", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task OptionValidation_Chain_UsesFirstFailure()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app");
        app.Add(
            "c|count=",
            "Iteration {COUNT}",
            (int _) => { },
            validate: Validate.Chain(
                Validate.Positive<int>(),
                Validate.Range(1, 1000)));
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(["--count", "-5"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("The value must be positive.", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ArgumentValidation_FileExists_ReturnsError()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app");
        app.Add("<input>", "Input {FILE}", (Action<string?>)(_ => { }), validate: Validate.FileExists(), hidden: false);
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(["missing-file-that-does-not-exist.txt"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("Invalid value for argument `<input>`", StringComparison.Ordinal));
        Assert.IsTrue(output.Contains("does not exist", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task OptionalValueValidation_IsSkipped_WhenValueIsMissing()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var observed = new List<string>();
        var app = new CommandApp("app");
        app.Add("n|name:", "Optional {NAME}", (Action<string?>)(v => observed.Add(v ?? string.Empty)), validate: Validate.NonEmpty(), hidden: false);
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(["--name"], new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.AreEqual(0, result);
        Assert.HasCount(1, observed);
        Assert.AreEqual(string.Empty, observed[0]);
    }

    [TestMethod]
    public async Task ValidationError_FromEnvironmentFallback_IncludesEnvironmentVariableName()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = name => name == "APP_PORT" ? "99999" : null
            });
        app.Add("p|port=", "Server {PORT}", (int _) => { }, validate: Validate.Range(1, 65535), envVar: "APP_PORT");
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(Array.Empty<string>(), new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("Invalid value for option `--port` (from environment variable `APP_PORT`)", StringComparison.Ordinal));
        Assert.IsTrue(output.Contains("between 1 and 65535", StringComparison.Ordinal));
    }
}
