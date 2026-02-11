// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace XenoAtom.CommandLine;

/// <summary>
/// Context used when running a command.
/// </summary>
public class CommandRunContext
{
    internal CommandRunContext(CommandRunConfig config)
    {
        RunConfig = config;
        ShouldShowLicenseOnRun = config.ShowLicenseOnRun;
    }

    internal ICommandOutput Output { get; set; } = DefaultCommandOutput.Instance;

    internal IReadOnlyList<string>? InvocationTokens { get; set; }

    internal bool CaptureParseValues { get; set; }

    internal bool IsParsingOnly { get; set; }

    internal Dictionary<string, List<string?>> ParsedOptionValues { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal List<string> ParsedArgumentValues { get; } = new();

    internal bool VersionRequested { get; set; }

    /// <summary>
    /// Gets or sets a boolean indicating if the license should be displayed when running the command.
    /// </summary>
    public bool ShouldShowLicenseOnRun { get; set; }

    /// <summary>
    /// Gets or sets a boolean indicating if the help should be displayed when running the command.
    /// </summary>
    public bool ShouldShowHelp { get; set; }

    /// <summary>
    /// Gets or sets a boolean indicating if the command should run after parsing options.
    /// </summary>
    public bool ShouldRunAfterParsingOptions { get; set; }

    /// <summary>
    /// Gets the configuration for running the command.
    /// </summary>
    public CommandRunConfig RunConfig { get; }

    /// <summary>
    /// Gets the output stream for the command.
    /// </summary>
    public TextWriter Out => RunConfig.Out;

    /// <summary>
    /// Gets the error stream for the command.
    /// </summary>
    public TextWriter Error => RunConfig.Error;

    internal void RecordOptionValues(Option option, IEnumerable<string?> values)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(values);
        if (!CaptureParseValues)
            return;

        var key = option.GetCanonicalName();
        if (!ParsedOptionValues.TryGetValue(key, out var list))
        {
            list = new List<string?>();
            ParsedOptionValues.Add(key, list);
        }

        foreach (var value in values)
        {
            list.Add(value);
        }
    }

    internal void RecordArgumentValue(string? value)
    {
        if (!CaptureParseValues)
            return;

        if (value is not null)
        {
            ParsedArgumentValues.Add(value);
        }
    }
}
