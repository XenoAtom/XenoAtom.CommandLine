// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
}