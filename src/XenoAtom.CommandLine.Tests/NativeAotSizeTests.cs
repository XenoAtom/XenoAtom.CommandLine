// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class NativeAotSizeTests
{
#if NET10_0_OR_GREATER
    [TestMethod]
    [TestCategory("NativeAot")]
    public void PublishAot_Size_IsUnderLimit()
    {
        var commandLineProject = FindCommandLineProject();

        var root = Path.Combine(AppContext.BaseDirectory, "csharp_tests", "nativeaot_size");
        var projectDir = Path.Combine(root, "app");
        if (Directory.Exists(projectDir))
        {
            Directory.Delete(projectDir, recursive: true);
        }
        Directory.CreateDirectory(projectDir);

        var projectPath = Path.Combine(projectDir, "NativeAotSizeApp.csproj");
        var programPath = Path.Combine(projectDir, "Program.cs");
        var publishDir = Path.Combine(projectDir, "publish");

        File.WriteAllText(projectPath, CreateProjectFile(commandLineProject));
        File.WriteAllText(programPath, CreateProgramFile());

        var publishArgs = $"publish \"{projectPath}\" -c Release " + $" -o \"{publishDir}\"";

        var result = RunProcess("dotnet", publishArgs, projectDir);
        if (result.ExitCode != 0)
        {
            Assert.Fail($"dotnet publish failed with exit code {result.ExitCode}\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");
        }

        var exePath = Path.Combine(publishDir, "NativeAotSizeApp" + (OperatingSystem.IsWindows() ? ".exe" : ""));
        Assert.IsTrue(File.Exists(exePath), $"Published executable not found at `{exePath}`\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");

        var exeSize = new FileInfo(exePath).Length;
        //Console.WriteLine($"Executable size: {exeSize:N0} bytes");
        long maxBytes = OperatingSystem.IsWindows() ? 1_250_000 :
            OperatingSystem.IsMacOS() ? 1_450_000 :
            1_500_000; // Linux (To check);
        Assert.IsLessThanOrEqualTo(maxBytes, exeSize, $"NativeAOT size regression: {exeSize:N0} bytes > {maxBytes:N0} bytes. Output: `{exePath}`");
    }

    private static string FindCommandLineProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidateFromRepoRoot = Path.Combine(dir.FullName, "src", "XenoAtom.CommandLine", "XenoAtom.CommandLine.csproj");
            if (File.Exists(candidateFromRepoRoot))
                return candidateFromRepoRoot;

            var candidateFromSrc = Path.Combine(dir.FullName, "XenoAtom.CommandLine", "XenoAtom.CommandLine.csproj");
            if (File.Exists(candidateFromSrc))
                return candidateFromSrc;

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Unable to locate `XenoAtom.CommandLine.csproj` from the test base directory.", AppContext.BaseDirectory);
    }

    private static string CreateProjectFile(string commandLineProjectPath)
    {
        var include = EscapeXml(commandLineProjectPath);
        //language=XML
        return
            $"""
             <Project Sdk="Microsoft.NET.Sdk">
               <PropertyGroup>
                 <OutputType>Exe</OutputType>
                 <TargetFramework>net10.0</TargetFramework>
                 <ImplicitUsings>enable</ImplicitUsings>
                 <Nullable>enable</Nullable>
                 <AssemblyName>NativeAotSizeApp</AssemblyName>
                 <PublishAot>true</PublishAot>
                 <InvariantGlobalization>true</InvariantGlobalization>
               </PropertyGroup>

               <ItemGroup>
                 <ProjectReference Include="{include}"/>
               </ItemGroup>
             </Project>
             """;
    }

    private static string CreateProgramFile()
    {
        return
            """
            using XenoAtom.CommandLine;

            bool verbose = false;
            string? name = null;

            var app = new CommandApp("NativeAotSizeApp", "NativeAOT size smoke test")
            {
                { "n|name=", "The {NAME} to greet", v => name = v },
                { "v|verbose", "Enable verbose output", _ => verbose = true },
                new HelpOption(),
                (ctx, _) =>
                {
                    if (verbose)
                    {
                        ctx.Out.WriteLine("verbose");
                    }

                    ctx.Out.WriteLine($"Hello {name ?? "world"}");
                    return ValueTask.FromResult(0);
                }
            };

            var exitCode = await app.RunAsync(args);
            return exitCode;
            """;
    }

    private static (int ExitCode, string StdOut, string StdErr) RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);
        if (process is null)
            throw new InvalidOperationException($"Failed to start process `{fileName}`.");

        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stdOut, stdErr);
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
    }
#endif
}
