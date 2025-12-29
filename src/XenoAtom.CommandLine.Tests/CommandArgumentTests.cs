using System.Globalization;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class CommandArgumentTests
{
    [TestMethod]
    public async Task CommandArguments_AssignInOrder_AndPassRemainingToAction()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        string? file1 = null;
        string? file2 = null;
        var remaining = new List<string>();

        var app = new CommandApp("app")
        {
            { "<file1>", "First input file", v => file1 = v },
            { "<file2>", "Second input file", v => file2 = v },
            (ctx, args) =>
            {
                remaining.AddRange(args);
                return ValueTask.FromResult(0);
            }
        };

        var result = await app.RunAsync(["a.txt", "b.txt", "c.txt"], new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.AreEqual(0, result);
        Assert.AreEqual("a.txt", file1);
        Assert.AreEqual("b.txt", file2);
        CollectionAssert.AreEqual(new[] { "c.txt" }, remaining);
    }

    [TestMethod]
    public async Task CommandArguments_MissingRequiredArgument_Returns1_AndShowsError()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            { "<file1>", "First input file", _ => { } },
            { "<file2>", "Second input file", _ => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["a.txt"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("Missing required argument `<file2>`.", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CommandArguments_OptionalLastArgument_Works()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        string? input = null;
        string? output = null;
        var remaining = new List<string>();

        var app = new CommandApp("app")
        {
            { "<input>", "Input file", v => input = v },
            { "<output>?", "Output file (optional)", v => output = v },
            (ctx, args) =>
            {
                remaining.AddRange(args);
                return ValueTask.FromResult(0);
            }
        };

        await app.RunAsync(["in.txt"], new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });
        Assert.AreEqual("in.txt", input);
        Assert.IsNull(output);
        CollectionAssert.AreEqual(Array.Empty<string>(), remaining);

        input = null;
        output = null;
        remaining.Clear();

        await app.RunAsync(["in.txt", "out.txt", "extra.txt"], new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });
        Assert.AreEqual("in.txt", input);
        Assert.AreEqual("out.txt", output);
        CollectionAssert.AreEqual(new[] { "extra.txt" }, remaining);
    }

    [TestMethod]
    public void CommandArguments_OptionalMustBeLast()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            _ = new CommandApp("app")
            {
                { "<a>?", "Optional a", _ => { } },
                { "<b>", "Required b", _ => { } },
                (ctx, _) => ValueTask.FromResult(0)
            };
            Assert.Fail("Expected an InvalidOperationException.");
        }
        catch (InvalidOperationException)
        {
        }
    }

    [TestMethod]
    public async Task CommandArguments_CanBeCombinedWithDefaultArgumentHandler()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        string? input = null;
        var files = new List<string>();
        var actionArgs = new List<string>();

        var app = new CommandApp("app")
        {
            new HelpOption(),
            { "<input>", "Input file", v => input = v },
            { "<>", "[files]*", files },
            (ctx, args) =>
            {
                actionArgs.AddRange(args);
                return ValueTask.FromResult(0);
            }
        };

        var result = await app.RunAsync(["in.txt", "a.bin", "b.bin"], new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.AreEqual(0, result);
        Assert.AreEqual("in.txt", input);
        CollectionAssert.AreEqual(new[] { "a.bin", "b.bin" }, files);
        CollectionAssert.AreEqual(Array.Empty<string>(), actionArgs);
    }

    [TestMethod]
    public async Task CommandArguments_AppearInDefaultUsage()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new HelpOption(),
            { "<input>", "Input file", _ => { } },
            { "<output>?", "Output file (optional)", _ => { } },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(["--help"], new CommandRunConfig { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        var output = writer.ToString();
        Assert.IsTrue(output.Contains("Usage: app [options] <input> [<output>]", StringComparison.Ordinal));
        Assert.IsTrue(output.Contains("<input>", StringComparison.Ordinal));
        Assert.IsTrue(output.Contains("[<output>]", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CommandArguments_List_OneOrMore_WorksAndIsRequired()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var files = new List<string>();
        var actionArgs = new List<string>();

        var app = new CommandApp("app")
        {
            { "<files>+", "Input files", files },
            (ctx, args) =>
            {
                actionArgs.AddRange(args);
                return ValueTask.FromResult(0);
            }
        };

        var result = await app.RunAsync(["a.txt", "b.txt"], new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });

        Assert.AreEqual(0, result);
        CollectionAssert.AreEqual(new[] { "a.txt", "b.txt" }, files);
        CollectionAssert.AreEqual(Array.Empty<string>(), actionArgs);

        var writer = new StringWriter();
        result = await app.RunAsync(Array.Empty<string>(), new CommandRunConfig { Out = writer, Error = writer });
        Assert.AreEqual(1, result);
        Assert.IsTrue(writer.ToString().Contains("Missing required argument `<files>+`.", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CommandArguments_List_ZeroOrMore_WorksAndIsOptional()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var files = new List<string>();
        var app = new CommandApp("app")
        {
            { "<files>*", "Input files", files },
            (ctx, _) => ValueTask.FromResult(0)
        };

        var result = await app.RunAsync(Array.Empty<string>(), new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });
        Assert.AreEqual(0, result);
        CollectionAssert.AreEqual(Array.Empty<string>(), files);

        files.Clear();
        result = await app.RunAsync(["a.txt"], new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null });
        Assert.AreEqual(0, result);
        CollectionAssert.AreEqual(new[] { "a.txt" }, files);
    }

    [TestMethod]
    public void CommandArguments_List_MustBeLast()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            _ = new CommandApp("app")
            {
                { "<files>*", "Files", _ => { } },
                { "<x>", "X", _ => { } },
                (ctx, _) => ValueTask.FromResult(0)
            };
            Assert.Fail("Expected an InvalidOperationException.");
        }
        catch (InvalidOperationException)
        {
        }
    }
}
