// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XenoAtom.CommandLine;

/// <summary>
/// The default plain-text output handler that reproduces the built-in help and error formatting.
/// </summary>
public sealed class DefaultCommandOutput : ICommandOutput
{
    /// <summary>
    /// Gets the singleton instance of the default output handler.
    /// </summary>
    public static readonly DefaultCommandOutput Instance = new();

    private DefaultCommandOutput()
    {
    }

    /// <inheritdoc />
    public void WriteHelp(Command command, CommandRunConfig runConfig)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        command.WriteHelpCore(runConfig);
    }

    /// <inheritdoc />
    public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        ArgumentNullException.ThrowIfNull(exception);
        command.WriteCommandExceptionCore(runConfig, exception);
    }

    /// <inheritdoc />
    public void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenKind kind, IReadOnlyList<UnknownTokenInfo> unknownTokens)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        ArgumentNullException.ThrowIfNull(unknownTokens);
        command.WriteUnknownTokensCore(runConfig, kind, unknownTokens);
    }

    /// <inheritdoc />
    public void WriteVersion(Command command, CommandRunConfig runConfig, string version)
    {
        ArgumentNullException.ThrowIfNull(runConfig);
        runConfig.Out.WriteLine(version);
    }

    /// <inheritdoc />
    public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText)
    {
        ArgumentNullException.ThrowIfNull(runConfig);
        runConfig.Out.WriteLine(licenseText);
    }
}

