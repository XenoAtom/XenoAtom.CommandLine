// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XenoAtom.CommandLine;

/// <summary>
/// Represents the result of parsing command-line arguments.
/// </summary>
public sealed class ParseResult
{
    internal ParseResult(
        Command resolvedCommand,
        string resolvedCommandPath,
        IReadOnlyDictionary<string, IReadOnlyList<string?>> optionValues,
        IReadOnlyList<string> argumentValues,
        IReadOnlyList<string> remainingArguments,
        IReadOnlyList<CommandException> errors,
        bool helpRequested,
        bool versionRequested)
    {
        ArgumentNullException.ThrowIfNull(resolvedCommand);
        ArgumentNullException.ThrowIfNull(resolvedCommandPath);
        ArgumentNullException.ThrowIfNull(optionValues);
        ArgumentNullException.ThrowIfNull(argumentValues);
        ArgumentNullException.ThrowIfNull(remainingArguments);
        ArgumentNullException.ThrowIfNull(errors);

        ResolvedCommand = resolvedCommand;
        ResolvedCommandPath = resolvedCommandPath;

        var optionValuesCopy = new Dictionary<string, IReadOnlyList<string?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in optionValues)
        {
            optionValuesCopy.Add(entry.Key, new ReadOnlyCollection<string?>(new List<string?>(entry.Value)));
        }
        OptionValues = new ReadOnlyDictionary<string, IReadOnlyList<string?>>(optionValuesCopy);

        ArgumentValues = new ReadOnlyCollection<string>(new List<string>(argumentValues));
        RemainingArguments = new ReadOnlyCollection<string>(new List<string>(remainingArguments));
        Errors = new ReadOnlyCollection<CommandException>(new List<CommandException>(errors));
        HelpRequested = helpRequested;
        VersionRequested = versionRequested;
    }

    /// <summary>
    /// Gets the command resolved by parsing (the deepest matched sub-command).
    /// </summary>
    public Command ResolvedCommand { get; }

    /// <summary>
    /// Gets the resolved full command path.
    /// </summary>
    public string ResolvedCommandPath { get; }

    /// <summary>
    /// Gets parsed option values keyed by canonical option name.
    /// Values include command-line and environment-variable fallback occurrences.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string?>> OptionValues { get; }

    /// <summary>
    /// Gets parsed positional argument values.
    /// </summary>
    public IReadOnlyList<string> ArgumentValues { get; }

    /// <summary>
    /// Gets remaining unprocessed arguments.
    /// </summary>
    public IReadOnlyList<string> RemainingArguments { get; }

    /// <summary>
    /// Gets parsing errors collected during parsing.
    /// </summary>
    public IReadOnlyList<CommandException> Errors { get; }

    /// <summary>
    /// Gets a value indicating whether parsing produced errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Gets a value indicating whether help was requested.
    /// </summary>
    public bool HelpRequested { get; }

    /// <summary>
    /// Gets a value indicating whether version was requested.
    /// </summary>
    public bool VersionRequested { get; }
}
