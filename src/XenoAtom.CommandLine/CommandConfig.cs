// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// The configuration for a <see cref="CommandApp"/>>.
/// </summary>
public record CommandConfig()
{
    /// <summary>
    /// The default configuration.
    /// </summary>
    public static readonly CommandConfig Default = new();

    /// <summary>
    /// The localizer for this command line application.
    /// </summary>
    public Converter<string, string> Localizer { get; init; } = static s => s;

    /// <summary>
    /// Gets a boolean indicating whether unknown option-like tokens (e.g. <c>--unknown</c>, <c>-x</c>, <c>/unknown</c>)
    /// should immediately fail parsing instead of being treated as positional arguments.
    /// </summary>
    /// <remarks>
    /// If you need to pass an argument that starts with <c>-</c> or <c>/</c>, use <c>--</c> to stop option parsing (e.g. <c>mytool -- -5</c>).
    /// </remarks>
    public bool StrictOptionParsing { get; init; } = true;

    /// <summary>
    /// Gets a delegate used to resolve environment variables for option fallbacks.
    /// </summary>
    public Func<string, string?> EnvironmentVariableResolver { get; init; } = Environment.GetEnvironmentVariable;

    /// <summary>
    /// Gets a factory used to create the output handler that renders help, errors,
    /// version, and other user-visible output for an invocation.
    /// </summary>
    /// <remarks>
    /// The factory receives the effective <see cref="CommandRunConfig"/> for the current invocation.
    /// Return a singleton for stateless implementations, or create one instance per invocation for stateful renderers.
    /// </remarks>
    public Func<CommandRunConfig, ICommandOutput>? OutputFactory { get; init; }
}
