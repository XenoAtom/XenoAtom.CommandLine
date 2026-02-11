using System.Globalization;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class ParseResultTests
{
    [TestMethod]
    public void Parse_ParsesValuesWithoutInvokingCommandAction()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        string? name = null;
        int port = 0;
        var actionCalled = false;

        var app = new CommandApp("app")
        {
            { "n|name=", "Name {NAME}", v => name = v },
            { "p|port=", "Port {PORT}", (int v) => port = v },
            (ctx, _) =>
            {
                actionCalled = true;
                return ValueTask.FromResult(0);
            }
        };

        var result = app.Parse(["--name", "Alice", "--port", "8080"]);

        Assert.IsFalse(actionCalled);
        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual("app", result.ResolvedCommandPath);
        Assert.AreEqual("Alice", name);
        Assert.AreEqual(8080, port);
        Assert.IsTrue(result.OptionValues.TryGetValue("name", out var nameValues));
        Assert.IsTrue(result.OptionValues.TryGetValue("port", out var portValues));
        Assert.AreEqual("Alice", nameValues[0]);
        Assert.AreEqual("8080", portValues[0]);
    }

    [TestMethod]
    public void Parse_ResolvesSubCommandWithoutInvokingCommandActions()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        string? message = null;
        var rootActionCalled = false;
        var subActionCalled = false;

        var app = new CommandApp("git")
        {
            new Command("commit")
            {
                { "m|message=", "Message {MESSAGE}", v => message = v },
                (ctx, _) =>
                {
                    subActionCalled = true;
                    return ValueTask.FromResult(0);
                }
            },
            (ctx, _) =>
            {
                rootActionCalled = true;
                return ValueTask.FromResult(0);
            }
        };

        var result = app.Parse(["commit", "--message", "fix"]);

        Assert.IsFalse(rootActionCalled);
        Assert.IsFalse(subActionCalled);
        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual("git commit", result.ResolvedCommandPath);
        Assert.AreEqual("fix", message);
        Assert.IsTrue(result.OptionValues.TryGetValue("message", out var messageValues));
        Assert.AreEqual("fix", messageValues[0]);
    }

    [TestMethod]
    public void Parse_CollectsErrors_ForUnknownOption()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var app = new CommandApp("app")
        {
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = app.Parse(["--unknown"]);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors[0].Message.Contains("Unknown option", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Parse_HelpRequest_DoesNotRenderHelp()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new HelpOption(),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = app.Parse(["--help"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.IsTrue(result.HelpRequested);
        Assert.IsFalse(result.VersionRequested);
        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(string.Empty, writer.ToString());
    }

    [TestMethod]
    public void Parse_VersionRequest_DoesNotRenderVersion()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new VersionOption("1.2.3"),
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = app.Parse(["--version"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.IsFalse(result.HelpRequested);
        Assert.IsTrue(result.VersionRequested);
        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(string.Empty, writer.ToString());
    }

    [TestMethod]
    public void Parse_CollectsOptionAndArgumentValues_WithEnvironmentFallback()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var files = new List<string>();
        int port = 0;
        var app = new CommandApp(
            "app",
            config: new CommandConfig
            {
                EnvironmentVariableResolver = name => name == "APP_PORT" ? "9000" : null
            });
        app.Add("p|port=", "Port {PORT}", (int v) => port = v, envVar: "APP_PORT");
        app.Add("<files>*", "Files", files);
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = app.Parse(["a.txt", "b.txt"]);

        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(9000, port);
        CollectionAssert.AreEqual(new[] { "a.txt", "b.txt" }, files);
        CollectionAssert.AreEqual(new[] { "a.txt", "b.txt" }, result.ArgumentValues.ToArray());
        Assert.IsTrue(result.OptionValues.TryGetValue("port", out var portValues));
        Assert.AreEqual("9000", portValues[0]);
    }

    [TestMethod]
    public void Parse_CollectsValidationErrors()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var app = new CommandApp("app");
        app.Add("p|port=", "Port {PORT}", (int _) => { }, validate: Validate.Range(1, 65535));
        app.Add((ctx, _) => ValueTask.FromResult(0));

        var result = app.Parse(["--port", "70000"]);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Errors[0].Message.Contains("Invalid value for option `--port`", StringComparison.Ordinal));
    }
}
