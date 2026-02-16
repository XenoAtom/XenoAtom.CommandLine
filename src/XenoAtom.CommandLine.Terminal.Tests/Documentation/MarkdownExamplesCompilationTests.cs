using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XenoAtom.CommandLine.Terminal.Tests.Documentation;

[TestClass]
public sealed class MarkdownExamplesCompilationTests
{
    private static readonly HashSet<string> SnippetsExcludedFromCompilation = new(StringComparer.Ordinal)
    {
        // This snippet intentionally documents the pre-2.0 style that no longer compiles in 2.0.
        "site_docs_migration_2_0_md_001"
    };

    private static readonly string[] DefaultUsingLines =
    [
        "using System;",
        "using System.Collections.Generic;",
        "using System.IO;",
        "using System.Linq;",
        "using System.Threading.Tasks;",
        "using XenoAtom.CommandLine;",
        "using XenoAtom.CommandLine.Terminal;",
        "using XenoAtom.Terminal;",
        "using XenoAtom.Terminal.UI;",
        "using XenoAtom.Terminal.UI.Controls;",
        "using XenoAtom.Terminal.UI.Figlet;",
        "using XenoAtom.Terminal.UI.Styling;",
        "using MarkdownSnippetSupport;",
        "using static MarkdownSnippetSupport.MarkdownSnippetGlobals;",
        "using static MarkdownSnippetSupport.MarkdownSnippetHelpers;"
    ];

    private const string SupportSource = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;

namespace MarkdownSnippetSupport;

public static class MarkdownSnippetHelpers
{
    public static Task SomeAsyncWork() => Task.CompletedTask;
}

public static class MarkdownSnippetGlobals
{
    public const string _ = "";

    public static string? target;
    public static string? name;
    public static string? email;
    public static string? input;
    public static string? output;
    public static string? dir;
    public static string? token;
    public static string? format;
    public static string? user;
    public static string? password;
    public static string? level;
    public static string? file;

    public static int port;
    public static int age;
    public static int threads;
    public static int retries;
    public static int count;

    public static bool advanced;
    public static bool json;
    public static bool xml;
    public static bool quiet;
    public static bool verbose;
    public static bool a;
    public static bool b;
    public static bool c;

    public static readonly List<string> includes = [];
    public static readonly List<string> files = [];
    public static readonly List<string> extraFiles = [];
    public static readonly List<string> names = [];
    public static readonly List<int> ports = [];
    public static readonly List<string> messages = [];
    public static readonly List<string> commitFiles = [];
    public static readonly List<(string, string?)> keyValues = [];
    public static readonly List<object> colors = [];

    public static CommandApp app = new("myexe");
    public static Command command = new("cmd");
    public static readonly string[] args = [];
}

public sealed class MyOutputRenderer : ICommandOutput
{
    public void WriteHelp(Command command, CommandRunConfig runConfig) { }
    public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception) { }
    public void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenReport report) { }
    public void WriteVersion(Command command, CommandRunConfig runConfig, string version) { }
    public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText) { }
}

public static class MyLocalizationService
{
    public static string Translate(string text) => text;
}

public sealed class JsonOutputRenderer : ICommandOutput
{
    public void WriteHelp(Command command, CommandRunConfig runConfig) { }
    public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception) { }
    public void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenReport report) { }
    public void WriteVersion(Command command, CommandRunConfig runConfig, string version) { }
    public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText) { }
}
""";

    [TestMethod]
    public void AllMarkdownSnippetsCompile()
    {
        var snippetsPath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Documentation",
                "MarkdownExamples.Snippets.cs"));

        Assert.IsTrue(File.Exists(snippetsPath), $"Missing snippets file: {snippetsPath}");

        var snippets = LoadSnippets(snippetsPath);
        Assert.IsTrue(snippets.Count > 0, "No snippets were found in MarkdownExamples.Snippets.cs.");

        var references = BuildReferences();
        var failures = new List<string>();

        foreach (var snippet in snippets)
        {
            if (SnippetsExcludedFromCompilation.Contains(snippet.Key))
            {
                continue;
            }

            if (TryCompileSnippet(snippet, references, out _))
            {
                continue;
            }

            TryCompileSnippet(snippet, references, out var diagnostics);
            failures.Add($"{snippet.Key}{Environment.NewLine}{diagnostics}{Environment.NewLine}{snippet.Code}");
        }

        if (failures.Count > 0)
        {
            Assert.Fail(
                $"The following snippets failed compiler validation:{Environment.NewLine}{Environment.NewLine}{string.Join($"{Environment.NewLine}{Environment.NewLine}", failures)}");
        }
    }

    private static bool TryCompileSnippet(Snippet snippet, IReadOnlyList<MetadataReference> references, out string diagnostics)
    {
        var diagnosticsByCandidate = new List<string>();
        foreach (var candidate in BuildCompilationCandidates(snippet.Code))
        {
            if (TryCompile(candidate.Source, candidate.OutputKind, references, out var candidateDiagnostics))
            {
                diagnostics = string.Empty;
                return true;
            }

            diagnosticsByCandidate.Add($"[{candidate.Name}]{Environment.NewLine}{candidateDiagnostics}");
        }

        diagnostics = string.Join($"{Environment.NewLine}{Environment.NewLine}", diagnosticsByCandidate);
        return false;
    }

    private static IReadOnlyList<Candidate> BuildCompilationCandidates(string snippetCode)
    {
        var (snippetUsings, snippetBody) = ExtractLeadingUsings(snippetCode);
        var candidates = new List<Candidate>
        {
            new("TopLevelProgram", BuildTopLevelCandidate(snippetUsings, snippetBody), OutputKind.ConsoleApplication),
            new("TopLevelLibrary", BuildTopLevelCandidate(snippetUsings, snippetBody), OutputKind.DynamicallyLinkedLibrary),
            new("MethodBody", BuildMethodBodyCandidate(snippetUsings, snippetBody), OutputKind.DynamicallyLinkedLibrary),
            new("CollectionInitializer", BuildCollectionInitializerCandidate(snippetUsings, snippetBody), OutputKind.DynamicallyLinkedLibrary),
            new("CollectionInitializerNormalized", BuildCollectionInitializerCandidate(snippetUsings, NormalizeCollectionEntries(snippetBody)), OutputKind.DynamicallyLinkedLibrary)
        };

        if (TrySplitPreambleAndCollection(snippetBody, out var preamble, out var collectionEntries))
        {
            candidates.Add(new(
                "SplitPreambleAndCollection",
                BuildSplitPreambleAndCollectionCandidate(snippetUsings, preamble, NormalizeCollectionEntries(collectionEntries)),
                OutputKind.DynamicallyLinkedLibrary));
        }

        if (TryExtractSingleExpression(snippetBody, out var expression))
        {
            candidates.Add(new("SingleExpression", BuildSingleExpressionCandidate(snippetUsings, expression), OutputKind.DynamicallyLinkedLibrary));
        }

        if (LooksLikeStandaloneLambda(snippetBody))
        {
            candidates.Add(new("LambdaContextAndArgs", BuildLambdaCandidate(snippetUsings, snippetBody, "Func<CommandRunContext, string[], ValueTask<int>>"), OutputKind.DynamicallyLinkedLibrary));
            candidates.Add(new("LambdaArgsOnly", BuildLambdaCandidate(snippetUsings, snippetBody, "Func<string[], ValueTask<int>>"), OutputKind.DynamicallyLinkedLibrary));
            candidates.Add(new("LambdaAsyncInt", BuildLambdaCandidate(snippetUsings, snippetBody, "Func<CommandRunContext, string[], Task<int>>"), OutputKind.DynamicallyLinkedLibrary));
        }

        return candidates;
    }

    private static string BuildTopLevelCandidate(IReadOnlyList<string> snippetUsings, string snippetBody)
    {
        var builder = new StringBuilder();
        AppendUsingBlock(builder, snippetUsings);
        AppendSnippet(builder, snippetBody, string.Empty);
        return builder.ToString();
    }

    private static string BuildMethodBodyCandidate(IReadOnlyList<string> snippetUsings, string snippetBody)
    {
        var builder = new StringBuilder();
        AppendUsingBlock(builder, snippetUsings);
        builder.AppendLine("namespace MarkdownSnippetMethodBody;");
        builder.AppendLine("public static class Host");
        builder.AppendLine("{");
        builder.AppendLine("    public static async Task Run(string[] args)");
        builder.AppendLine("    {");
        AppendSnippet(builder, snippetBody, "        ");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string BuildCollectionInitializerCandidate(IReadOnlyList<string> snippetUsings, string snippetBody)
    {
        var builder = new StringBuilder();
        AppendUsingBlock(builder, snippetUsings);
        builder.AppendLine("namespace MarkdownSnippetCollection;");
        builder.AppendLine("public static class Host");
        builder.AppendLine("{");
        builder.AppendLine("    public static async Task Run(string[] args)");
        builder.AppendLine("    {");
        builder.AppendLine("        var localApp = new CommandApp(\"myexe\")");
        builder.AppendLine("        {");
        AppendSnippet(builder, snippetBody, "            ");
        builder.AppendLine("        };");
        builder.AppendLine("        await Task.CompletedTask;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string BuildSplitPreambleAndCollectionCandidate(IReadOnlyList<string> snippetUsings, string preamble, string collectionEntries)
    {
        var builder = new StringBuilder();
        AppendUsingBlock(builder, snippetUsings);
        builder.AppendLine("namespace MarkdownSnippetSplit;");
        builder.AppendLine("public static class Host");
        builder.AppendLine("{");
        builder.AppendLine("    public static async Task Run(string[] args)");
        builder.AppendLine("    {");
        AppendSnippet(builder, preamble, "        ");
        builder.AppendLine("        var localApp = new CommandApp(\"myexe\")");
        builder.AppendLine("        {");
        AppendSnippet(builder, collectionEntries, "            ");
        builder.AppendLine("        };");
        builder.AppendLine("        await Task.CompletedTask;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string BuildSingleExpressionCandidate(IReadOnlyList<string> snippetUsings, string expression)
    {
        var builder = new StringBuilder();
        AppendUsingBlock(builder, snippetUsings);
        builder.AppendLine("namespace MarkdownSnippetExpression;");
        builder.AppendLine("public static class Host");
        builder.AppendLine("{");
        builder.AppendLine("    public static void Run()");
        builder.AppendLine("    {");
        builder.Append("        var ignored = ");
        builder.Append(expression);
        builder.AppendLine(";");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string BuildLambdaCandidate(IReadOnlyList<string> snippetUsings, string lambdaExpression, string delegateType)
    {
        var builder = new StringBuilder();
        AppendUsingBlock(builder, snippetUsings);
        builder.AppendLine("namespace MarkdownSnippetLambda;");
        builder.AppendLine("public static class Host");
        builder.AppendLine("{");
        builder.AppendLine("    public static void Run()");
        builder.AppendLine("    {");
        builder.Append("        ");
        builder.Append(delegateType);
        builder.Append(" handler = ");
        builder.Append(lambdaExpression.Trim());
        builder.AppendLine(";");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendUsingBlock(StringBuilder builder, IReadOnlyList<string> snippetUsings)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var defaultUsing in DefaultUsingLines)
        {
            AppendUsingLine(builder, defaultUsing, seen);
        }

        foreach (var snippetUsing in snippetUsings)
        {
            AppendUsingLine(builder, snippetUsing, seen);
        }

        builder.AppendLine();
    }

    private static void AppendUsingLine(StringBuilder builder, string line, ISet<string> seen)
    {
        var normalized = line.Trim();
        if (normalized.Length == 0 || !normalized.StartsWith("using ", StringComparison.Ordinal))
        {
            return;
        }

        if (!normalized.EndsWith(";", StringComparison.Ordinal))
        {
            normalized = $"{normalized};";
        }

        if (seen.Add(normalized))
        {
            builder.AppendLine(normalized);
        }
    }

    private static void AppendSnippet(StringBuilder builder, string snippetCode, string indent)
    {
        foreach (var line in snippetCode.Split(['\r', '\n'], StringSplitOptions.None))
        {
            builder.Append(indent);
            builder.AppendLine(line);
        }
    }

    private static (IReadOnlyList<string> SnippetUsings, string SnippetBody) ExtractLeadingUsings(string snippetCode)
    {
        var normalizedSnippet = snippetCode.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalizedSnippet.Split('\n');

        var snippetUsings = new List<string>();
        var bodyStart = 0;

        while (bodyStart < lines.Length)
        {
            var trimmed = lines[bodyStart].Trim();
            if (trimmed.Length == 0)
            {
                bodyStart++;
                continue;
            }

            if (trimmed.StartsWith("using ", StringComparison.Ordinal) && trimmed.EndsWith(";", StringComparison.Ordinal))
            {
                snippetUsings.Add(trimmed);
                bodyStart++;
                continue;
            }

            break;
        }

        var body = string.Join(Environment.NewLine, lines.Skip(bodyStart)).Trim();
        return (snippetUsings, body);
    }

    private static bool TrySplitPreambleAndCollection(string snippetBody, out string preamble, out string collectionEntries)
    {
        var lines = snippetBody.Split(['\r', '\n'], StringSplitOptions.None);
        var firstCollectionLineIndex = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                firstCollectionLineIndex = i;
                break;
            }

            if (!trimmed.EndsWith(";", StringComparison.Ordinal))
            {
                preamble = string.Empty;
                collectionEntries = string.Empty;
                return false;
            }
        }

        if (firstCollectionLineIndex < 0)
        {
            preamble = string.Empty;
            collectionEntries = string.Empty;
            return false;
        }

        preamble = string.Join(Environment.NewLine, lines.Take(firstCollectionLineIndex)).Trim();
        collectionEntries = string.Join(Environment.NewLine, lines.Skip(firstCollectionLineIndex)).Trim();
        return collectionEntries.Length > 0;
    }

    private static bool TryExtractSingleExpression(string snippetBody, out string expression)
    {
        var trimmed = snippetBody.Trim();
        if (trimmed.Length == 0 || trimmed.Contains('\n') || trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            expression = string.Empty;
            return false;
        }

        expression = trimmed.EndsWith(";", StringComparison.Ordinal)
            ? trimmed[..^1].TrimEnd()
            : trimmed;

        return expression.Length > 0;
    }

    private static bool LooksLikeStandaloneLambda(string snippetBody)
    {
        var trimmed = snippetBody.TrimStart();
        if (!trimmed.Contains("=>", StringComparison.Ordinal))
        {
            return false;
        }

        return trimmed.StartsWith("(", StringComparison.Ordinal) || trimmed.StartsWith("async (", StringComparison.Ordinal);
    }

    private static string NormalizeCollectionEntries(string snippetCode)
    {
        var lines = snippetCode
            .Split(['\r', '\n'], StringSplitOptions.None)
            .ToList();

        var nonEmptyIndexes = new List<int>();
        for (var i = 0; i < lines.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                nonEmptyIndexes.Add(i);
            }
        }

        if (nonEmptyIndexes.Count == 0)
        {
            return snippetCode;
        }

        var allSingleLineCollectionEntries = true;
        foreach (var index in nonEmptyIndexes)
        {
            var trimmed = lines[index].Trim();
            if (!(trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal)))
            {
                allSingleLineCollectionEntries = false;
                break;
            }
        }

        if (!allSingleLineCollectionEntries)
        {
            return snippetCode;
        }

        for (var i = 0; i < nonEmptyIndexes.Count - 1; i++)
        {
            var index = nonEmptyIndexes[i];
            if (!lines[index].TrimEnd().EndsWith(",", StringComparison.Ordinal))
            {
                lines[index] = $"{lines[index]},";
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool TryCompile(string source, OutputKind outputKind, IReadOnlyList<MetadataReference> references, out string diagnostics)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var supportTree = CSharpSyntaxTree.ParseText(SupportSource, parseOptions, path: "SnippetSupport.g.cs");
        var snippetTree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "SnippetCandidate.g.cs");

        var compilation = CSharpCompilation.Create(
            assemblyName: $"MarkdownSnippet_{Guid.NewGuid():N}",
            syntaxTrees: [supportTree, snippetTree],
            references: references,
            options: new CSharpCompilationOptions(
                outputKind,
                nullableContextOptions: NullableContextOptions.Enable));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (result.Success)
        {
            diagnostics = string.Empty;
            return true;
        }

        diagnostics = string.Join(
            Environment.NewLine,
            result.Diagnostics
                .Where(_ => _.Severity == DiagnosticSeverity.Error)
                .Select(_ => _.ToString()));
        return false;
    }

    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (!string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    paths.Add(path);
                }
            }
        }

        AddAssemblyPath(typeof(CommandApp).Assembly, paths);
        AddAssemblyPath(typeof(TerminalVisualCommandOutput).Assembly, paths);
        AddAssemblyPath(typeof(XenoAtom.Terminal.Terminal).Assembly, paths);
        AddAssemblyPath(typeof(Enumerable).Assembly, paths);
        AddAssemblyPath(typeof(ValueTask).Assembly, paths);

        return paths
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
    }

    private static void AddAssemblyPath(Assembly assembly, ISet<string> paths)
    {
        if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location))
        {
            return;
        }

        if (File.Exists(assembly.Location))
        {
            paths.Add(assembly.Location);
        }
    }

    private static IReadOnlyList<Snippet> LoadSnippets(string snippetsPath)
    {
        var snippets = new List<Snippet>();
        string? key = null;
        var lines = new List<string>();

        foreach (var rawLine in File.ReadLines(snippetsPath))
        {
            if (TryGetBeginSnippetKey(rawLine, out var beginKey))
            {
                if (key is not null)
                {
                    throw new InvalidOperationException($"Nested snippet markers are not supported. Current: {key}, New: {beginKey}");
                }

                key = beginKey;
                lines.Clear();
                continue;
            }

            if (key is not null && rawLine.Contains("end-snippet", StringComparison.Ordinal))
            {
                snippets.Add(new(key, string.Join(Environment.NewLine, lines)));
                key = null;
                lines.Clear();
                continue;
            }

            if (key is not null)
            {
                lines.Add(rawLine);
            }
        }

        if (key is not null)
        {
            throw new InvalidOperationException($"Missing end-snippet marker for '{key}'.");
        }

        return snippets;
    }

    private static bool TryGetBeginSnippetKey(string line, out string key)
    {
        const string marker = "begin-snippet:";
        var index = line.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            key = string.Empty;
            return false;
        }

        var raw = line[(index + marker.Length)..].Trim();
        var metadataStart = raw.IndexOf('(');
        if (metadataStart >= 0)
        {
            raw = raw[..metadataStart].Trim();
        }

        key = raw;
        return key.Length > 0;
    }

    private readonly record struct Candidate(string Name, string Source, OutputKind OutputKind);
    private sealed record Snippet(string Key, string Code);
}
