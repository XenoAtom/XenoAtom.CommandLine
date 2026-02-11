using System.Globalization;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class EnvironmentVariableFallbackTests
{
    [TestMethod]
    public async Task Option_UsesEnvironmentFallback_WhenNotProvided()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        int port = 0;
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = name => name == "APP_PORT" ? "8080" : null
            });
        app.Add("p|port=", "Server {PORT}", (int v) => port = v, envVar: "APP_PORT");
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(Array.Empty<string>(), new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.AreEqual(0, result);
        Assert.AreEqual(8080, port);
    }

    [TestMethod]
    public async Task CommandLineValue_TakesPrecedence_OverEnvironmentFallback()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        int port = 0;
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = name => name == "APP_PORT" ? "8080" : null
            });
        app.Add("p|port=", "Server {PORT}", (int v) => port = v, envVar: "APP_PORT");
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(["--port", "42"], new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.AreEqual(0, result);
        Assert.AreEqual(42, port);
    }

    [TestMethod]
    public async Task Help_IncludesEnvironmentVariableSuffix()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app");
        app.Add("t|token=", "API {TOKEN}", _ => { }, envVar: "MY_TOKEN");
        app.Add(new HelpOption());
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(["--help"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.IsTrue(writer.ToString().Contains("[env: MY_TOKEN]", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task EnvironmentDelimiter_SplitsMultipleValues()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var includes = new List<string>();
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = name => name == "APP_INCLUDES" ? "a;b;;c" : null
            });
        app.Add("i|include=", "Include {PATH}", includes, envVar: "APP_INCLUDES", envVarDelimiter: ';');
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(Array.Empty<string>(), new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.AreEqual(0, result);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, includes);
    }

    [TestMethod]
    public async Task EnvironmentFlag_ParsesBooleanValues()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        bool verbose = false;
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = name => name == "APP_VERBOSE" ? "yes" : null
            });
        app.Add("v|verbose", "Enable verbose mode", v => verbose = v != null, envVar: "APP_VERBOSE");
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(Array.Empty<string>(), new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.AreEqual(0, result);
        Assert.IsTrue(verbose);
    }

    [TestMethod]
    public async Task EnvironmentFlag_InvalidBoolean_ReturnsError()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = name => name == "APP_VERBOSE" ? "maybe" : null
            });
        app.Add("v|verbose", "Enable verbose mode", _ => { }, envVar: "APP_VERBOSE");
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(Array.Empty<string>(), new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("from environment variable `APP_VERBOSE`", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("maybe", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task HelpRequest_SkipsEnvironmentFallbackErrors()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = name => name == "APP_PORT" ? "invalid" : null
            });
        app.Add("p|port=", "Server {PORT}", (int _) => { }, envVar: "APP_PORT");
        app.Add(new HelpOption());
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = await app.RunAsync(["--help"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.IsTrue(writer.ToString().Contains("Usage: app [options]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EnvironmentFallback_ArgumentPrototype_Throws()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            var app = new CommandApp("app");
            app.Add("<file>", "Input {FILE}", _ => { }, envVar: "APP_FILE");
            app.Add((ctx, _) => ValueTask.FromResult(0));
            Assert.Fail("Expected ArgumentException.");
        }
        catch (ArgumentException)
        {
        }
    }
}
