using System.IO;
using BenchmarkDotNet.Attributes;
using XenoAtom.CommandLine;

namespace XenoAtom.CommandLine.Benchmarks;

[MemoryDiagnoser]
public class ParsingBenchmarks
{
    private CommandApp _app = null!;
    private CommandRunConfig _runConfig = null!;

    private string[] _argsShortBundled = null!;
    private string[] _argsLong = null!;
    private string[] _argsKeyValue = null!;

    [GlobalSetup]
    public void Setup()
    {
        const string _ = "";

        _app = new CommandApp("bench")
        {
            _,
            { "n|name=", "Name", _ => { } },
            { "a|age=", "Age", (int _) => { } },
            { "f=", "Input file", _ => { } },
            { "x", "Extract", _ => { } },
            { "c", "Create", _ => { } },
            { "t", "List", _ => { } },
            { "D:", "Define {0:key} and optional {1:value}", (_, __) => { } },
            { "P={->}", "Pair {0:key} {1:value}", (string _, string? __) => { } },
            (ctx, _) => ValueTask.FromResult(0),
        };

        _runConfig = new CommandRunConfig()
        {
            Out = TextWriter.Null,
            Error = TextWriter.Null,
        };

        _argsShortBundled = ["-txc", "-f", "input.txt", "-a50", "--name", "John"];
        _argsLong = ["--name", "John", "--age", "50", "--", "--not-an-option", "value"];
        _argsKeyValue = ["-DHELLO", "-DTEST=WORLD", "-P", "name1->value2"];
    }

    [Benchmark]
    public int ShortBundled()
        => _app.RunAsync(_argsShortBundled, _runConfig).GetAwaiter().GetResult();

    [Benchmark]
    public int LongOptions()
        => _app.RunAsync(_argsLong, _runConfig).GetAwaiter().GetResult();

    [Benchmark]
    public int KeyValuePairs()
        => _app.RunAsync(_argsKeyValue, _runConfig).GetAwaiter().GetResult();
}
