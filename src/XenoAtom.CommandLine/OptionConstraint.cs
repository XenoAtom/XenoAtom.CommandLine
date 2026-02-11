// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XenoAtom.CommandLine;

/// <summary>
/// Defines a constraint between options in a command.
/// </summary>
public abstract class OptionConstraint : CommandNode
{
    internal OptionConstraint(Func<bool>? active = null) : base(active)
    {
    }
}

/// <summary>
/// Declares that the specified options cannot be used together.
/// </summary>
public sealed class MutuallyExclusiveConstraint : OptionConstraint
{
    /// <summary>
    /// Creates a new mutually exclusive constraint.
    /// </summary>
    /// <param name="optionNames">Two or more option names (without prefix).</param>
    /// <param name="active">A callback that indicates if this constraint is active.</param>
    public MutuallyExclusiveConstraint(string[] optionNames, Func<bool>? active = null) : base(active)
    {
        ArgumentNullException.ThrowIfNull(optionNames);
        if (optionNames.Length < 2)
            throw new ArgumentException("At least two option names are required.", nameof(optionNames));

        var values = new List<string>(optionNames.Length);
        for (var i = 0; i < optionNames.Length; i++)
        {
            var optionName = optionNames[i];
            ArgumentException.ThrowIfNullOrWhiteSpace(optionName);
            values.Add(optionName);
        }

        OptionNames = new ReadOnlyCollection<string>(values);
    }

    /// <summary>
    /// Creates a new mutually exclusive constraint.
    /// </summary>
    /// <param name="optionNames">Two or more option names (without prefix).</param>
    public MutuallyExclusiveConstraint(params string[] optionNames) : this(optionNames, null)
    {
    }

    /// <summary>
    /// Gets the option names in this exclusive group.
    /// </summary>
    public IReadOnlyList<string> OptionNames { get; }
}

/// <summary>
/// Declares that when the specified option is present, all required options must also be present.
/// </summary>
public sealed class RequiresConstraint : OptionConstraint
{
    /// <summary>
    /// Creates a new requires constraint.
    /// </summary>
    /// <param name="optionName">The option that triggers the requirement.</param>
    /// <param name="requiredOptionNames">One or more required option names.</param>
    /// <param name="active">A callback that indicates if this constraint is active.</param>
    public RequiresConstraint(string optionName, string[] requiredOptionNames, Func<bool>? active = null) : base(active)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optionName);
        ArgumentNullException.ThrowIfNull(requiredOptionNames);
        if (requiredOptionNames.Length == 0)
            throw new ArgumentException("At least one required option name must be provided.", nameof(requiredOptionNames));

        var values = new List<string>(requiredOptionNames.Length);
        for (var i = 0; i < requiredOptionNames.Length; i++)
        {
            var requiredOptionName = requiredOptionNames[i];
            ArgumentException.ThrowIfNullOrWhiteSpace(requiredOptionName);
            values.Add(requiredOptionName);
        }

        OptionName = optionName;
        RequiredOptionNames = new ReadOnlyCollection<string>(values);
    }

    /// <summary>
    /// Creates a new requires constraint.
    /// </summary>
    /// <param name="optionName">The option that triggers the requirement.</param>
    /// <param name="requiredOptionNames">One or more required option names.</param>
    public RequiresConstraint(string optionName, params string[] requiredOptionNames) : this(optionName, requiredOptionNames, null)
    {
    }

    /// <summary>
    /// Gets the option that triggers the requirement.
    /// </summary>
    public string OptionName { get; }

    /// <summary>
    /// Gets the options required when <see cref="OptionName"/> is present.
    /// </summary>
    public IReadOnlyList<string> RequiredOptionNames { get; }
}

