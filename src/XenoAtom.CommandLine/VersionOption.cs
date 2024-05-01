// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;

namespace XenoAtom.CommandLine;

/// <summary>
/// An option that shows the version of the command.
/// </summary>
/// <param name="version">The version to display. Default will extract the version from the entry point assembly.</param>
/// <param name="prototype">The prototype for this option. Default is "v|version".</param>
/// <param name="help">The help for this option.</param>
public class VersionOption(string? version = null, string prototype = "v|version", string help = "Show the version of this command") : Option(prototype, help)
{
    /// <summary>
    /// Gets the version to display.
    /// </summary>
    public string Version { get; } = version ?? GetDefaultVersion();

    /// <inheritdoc />
    protected override void OnParseComplete(OptionContext c)
    {
        var commandContext = c.CommandRunContext;
        commandContext.ShouldRunAfterParsingOptions = false;
        commandContext.RunConfig.Out.WriteLine(Version);
    }

    /// <summary>
    /// Gets the default version from the entry assembly.
    /// </summary>
    public static string GetDefaultVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        string? version = null;
        if (assembly != null)
        {
            version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (string.IsNullOrEmpty(version))
            {
                version = assembly.GetName().Version?.ToString();
            }
        }

        return version ?? "0.0.0";
    }
}