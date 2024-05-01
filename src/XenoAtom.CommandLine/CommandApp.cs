// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Reflection;

namespace XenoAtom.CommandLine;

/// <summary>
/// The main entry point for a command line application.
/// </summary>
public class CommandApp : Command
{
    /// <summary>
    /// Creates a new instance of <see cref="CommandApp"/>.
    /// </summary>
    /// <param name="config">The configuration for this command line application.</param>
    public CommandApp(CommandConfig? config = null) : this(GetDefaultAppCommand(), string.Empty, config)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="CommandApp"/>.
    /// </summary>
    /// <param name="name">The name of the command line application. Default is the exe name returned by <see cref="Environment.ProcessPath"/>.</param>
    /// <param name="help">The optional help for this command.</param>
    /// <param name="config">The configuration for this command line application.</param>
    public CommandApp(string name, string? help = null, CommandConfig? config = null) : base(name, help)
    {
        Config = config ?? CommandConfig.Default;
    }

    /// <summary>
    /// The license header for this command line application.
    /// </summary>
    public Func<string>? LicenseHeader { get; set; }

    //public IEnumerable<string> GetCompletions(string? prefix = null)
    //{
    //    string rest;
    //    prefix ??= "";
    //    ExtractToken(ref prefix, out rest);

    //    foreach (var command in this)
    //    {
    //        if (command.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    //        {
    //            yield return command.Name;
    //        }
    //    }

    //    if (NestedCommandSets == null)
    //        yield break;

    //    foreach (var subset in NestedCommandSets)
    //    {
    //        if (subset.Suite.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    //        {
    //            foreach (var c in subset.GetCompletions(rest))
    //            {
    //                yield return $"{subset.Suite} {c}";
    //            }
    //        }
    //    }
    //}
    
    //private static void ExtractToken(ref string input, out string rest)
    //{
    //    rest = "";

    //    int top = input.Length;
    //    for (int i = 0; i < top; i++)
    //    {
    //        if (char.IsWhiteSpace(input[i]))
    //            continue;

    //        for (int j = i; j < top; j++)
    //        {
    //            if (char.IsWhiteSpace(input[j]))
    //            {
    //                rest = input.Substring(j).Trim();
    //                input = input.Substring(i, j).Trim();
    //                return;
    //            }
    //        }
    //        rest = "";
    //        if (i != 0)
    //            input = input.Substring(i).Trim();
    //        return;
    //    }
    //}

    private static string GetDefaultAppCommand()
    {
        // TODO: Fix this once there is a solution for https://github.com/dotnet/runtime/issues/101837
        return PathHelper.GetExeName(Environment.ProcessPath) ?? PathHelper.GetExeName(Assembly.GetEntryAssembly()?.GetName().Name) ?? "command";
    }
}