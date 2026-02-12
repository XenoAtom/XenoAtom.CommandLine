// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using XenoAtom.CommandLine.Terminal.Internals;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// An <see cref="ICommandOutput"/> that renders help and diagnostics with terminal markup.
/// </summary>
public class TerminalMarkupCommandOutput : ICommandOutput
{
    private readonly TerminalMarkupOutputOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="TerminalMarkupCommandOutput"/>.
    /// </summary>
    /// <param name="options">Optional output options.</param>
    public TerminalMarkupCommandOutput(TerminalMarkupOutputOptions? options = null)
    {
        _options = options ?? new TerminalMarkupOutputOptions();
    }

    /// <summary>
    /// Gets the options used by this output renderer.
    /// </summary>
    protected TerminalMarkupOutputOptions Options => _options;

    /// <inheritdoc />
    public virtual void WriteHelp(Command command, CommandRunConfig runConfig)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);

        var model = HelpModelBuilder.Build(command, preserveNodeOrder: true);
        HelpMarkupWriter.Write(command, runConfig, _options, model);
    }

    /// <inheritdoc />
    public virtual void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        ArgumentNullException.ThrowIfNull(exception);

        ErrorMarkupWriter.WriteError(command, _options, exception);
    }

    /// <inheritdoc />
    public virtual void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenKind kind, IReadOnlyList<UnknownTokenInfo> unknownTokens)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        ArgumentNullException.ThrowIfNull(unknownTokens);

        ErrorMarkupWriter.WriteUnknownTokens(command, _options, kind, unknownTokens);
    }

    /// <inheritdoc />
    public virtual void WriteVersion(Command command, CommandRunConfig runConfig, string version)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        ArgumentNullException.ThrowIfNull(version);

        MarkupAtomicWriter.WriteLine(version);
    }

    /// <inheritdoc />
    public virtual void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        ArgumentNullException.ThrowIfNull(licenseText);

        MarkupAtomicWriter.WriteLine(licenseText);
    }
}
