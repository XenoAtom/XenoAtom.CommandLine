// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class CommandCompletionTests
{
    [TestMethod]
    public void GetCompletions_TopLevelCommands()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var app = new CommandApp("app")
        {
            new Command("hello"),
            new Command("world"),
        };

        var results = app.GetCompletions("he").ToArray();
        CollectionAssert.AreEqual(new[] { "hello" }, results);
    }

    [TestMethod]
    public void GetCompletions_OptionNames()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var app = new CommandApp("app")
        {
            { "n|name=", "Name", _ => { } },
            { "h|help", "Help", _ => { } },
        };

        var results = app.GetCompletions("--na").ToArray();
        CollectionAssert.AreEqual(new[] { "--name" }, results);
    }

    [TestMethod]
    public void GetCompletions_ShortAndLongPrefixPolicy()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var app = new CommandApp("app")
        {
            { "h|help", "Help", _ => { } },
        };

        var shortResults = app.GetCompletions("-h").ToArray();
        CollectionAssert.AreEqual(new[] { "-h" }, shortResults);

        var longResults = app.GetCompletions("--h").ToArray();
        CollectionAssert.AreEqual(new[] { "--help" }, longResults);
    }

    [TestMethod]
    public void GetCompletions_SubcommandContext()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var app = new CommandApp("app")
        {
            new Command("hello")
            {
                { "n|name=", "Name", _ => { } },
            },
        };

        var results = app.GetCompletions("hello --na").ToArray();
        CollectionAssert.AreEqual(new[] { "--name" }, results);
    }

    [TestMethod]
    public async Task CompletionCommands_CompleteRequest_PrintsCandidates_AndSkipsAction()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var ran = false;

        var app = new CommandApp("app")
        {
            new CompletionCommands(),
            new Command("hello"),
            (ctx, _) =>
            {
                ran = true;
                ctx.Out.WriteLine("RUN");
                return ValueTask.FromResult(0);
            }
        };

        var line = "app he";
        var cursor = line.Length;
        var result = await app.RunAsync(["__complete", $"--line={line}", $"--cursor={cursor}", "--command-name=app"], new CommandRunConfig() { Out = writer, Error = writer });

        Assert.AreEqual(0, result);
        Assert.IsFalse(ran);
        var output = writer.ToString().ReplaceLineEndings("\n");
        Assert.AreEqual("hello\n", output);
    }

    [TestMethod]
    public async Task CompletionCommands_Scripts_AreGenerated()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new CompletionCommands(),
            (ctx, _) => ValueTask.FromResult(0),
        };

        var result = await app.RunAsync(["completion", "bash"], new CommandRunConfig() { Out = writer, Error = writer });
        Assert.AreEqual(0, result);
        var script = writer.ToString();
        Assert.IsTrue(script.Contains("Bash completion for app", StringComparison.Ordinal));
        Assert.IsTrue(script.Contains("__complete", StringComparison.Ordinal));
        Assert.IsTrue(script.Contains("local -a cmd=(", StringComparison.Ordinal));
        Assert.IsTrue(script.Contains("\"${cmd[@]}\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CompletionCommands_ZshScript_CanBeSourced()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var writer = new StringWriter();
        var app = new CommandApp("app")
        {
            new CompletionCommands(),
            (ctx, _) => ValueTask.FromResult(0),
        };

        var result = await app.RunAsync(["completion", "zsh"], new CommandRunConfig() { Out = writer, Error = writer });
        Assert.AreEqual(0, result);
        var script = writer.ToString();
        Assert.IsTrue(script.Contains("source <(app completion zsh)", StringComparison.Ordinal));
        Assert.IsTrue(script.Contains("compdef _app_complete app", StringComparison.Ordinal));
        Assert.IsTrue(script.Contains("compadd", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CompletionCommands_Scripts_QuoteInvocationArguments()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var app = new CommandApp("app")
        {
            new CompletionCommands(),
            (ctx, _) => ValueTask.FromResult(0),
        };

        var invocationArgs = GetExpectedInvocationArguments("app");
        var expectedBashArray = string.Join(" ", invocationArgs.Select(QuotePosixSingle));
        var expectedPowerShellInvocation = "& " + string.Join(" ", invocationArgs.Select(QuotePowerShellSingle));

        var bashWriter = new StringWriter();
        var bashResult = await app.RunAsync(["completion", "bash"], new CommandRunConfig() { Out = bashWriter, Error = bashWriter });
        Assert.AreEqual(0, bashResult);
        var bashScript = bashWriter.ToString();
        Assert.IsTrue(bashScript.Contains($"local -a cmd=({expectedBashArray})", StringComparison.Ordinal));

        var psWriter = new StringWriter();
        var psResult = await app.RunAsync(["completion", "powershell"], new CommandRunConfig() { Out = psWriter, Error = psWriter });
        Assert.AreEqual(0, psResult);
        var psScript = psWriter.ToString();
        Assert.IsTrue(psScript.Contains(expectedPowerShellInvocation, StringComparison.Ordinal));
    }

    private static string[] GetExpectedInvocationArguments(string commandName)
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
            return [commandName];

        var fileName = Path.GetFileNameWithoutExtension(processPath);
        if (string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var commandLine = Environment.GetCommandLineArgs();
            for (var i = 1; i < commandLine.Length; i++)
            {
                var arg = commandLine[i];
                if (!arg.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!File.Exists(arg))
                    continue;
                return [processPath, arg];
            }
        }

        return [processPath];
    }

    private static string QuotePosixSingle(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length + 2);
        sb.Append('\'');
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '\'')
            {
                sb.Append("'\\''");
            }
            else
            {
                sb.Append(c);
            }
        }
        sb.Append('\'');
        return sb.ToString();
    }

    private static string QuotePowerShellSingle(string value)
    {
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
    }
}
