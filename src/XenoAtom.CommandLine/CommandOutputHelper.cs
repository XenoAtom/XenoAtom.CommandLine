// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace XenoAtom.CommandLine;

/// <summary>
/// Represents a rendered invocation and token-to-text mapping.
/// </summary>
public sealed class RenderedInvocation
{
    private readonly int[] _tokenStarts;
    private readonly int[] _tokenLengths;

    internal RenderedInvocation(string text, int[] tokenStarts, int[] tokenLengths)
    {
        Text = text;
        _tokenStarts = tokenStarts;
        _tokenLengths = tokenLengths;
    }

    /// <summary>
    /// Gets the rendered invocation text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the number of mapped tokens.
    /// </summary>
    public int TokenCount => _tokenStarts.Length;

    /// <summary>
    /// Tries to get the text bounds for a token index.
    /// </summary>
    /// <param name="tokenIndex">The 0-based token index.</param>
    /// <param name="start">The 0-based character start in <see cref="Text"/>.</param>
    /// <param name="length">The token length in characters.</param>
    /// <returns><c>true</c> if bounds are available; otherwise <c>false</c>.</returns>
    public bool TryGetTokenBounds(int tokenIndex, out int start, out int length)
    {
        if ((uint)tokenIndex >= (uint)_tokenStarts.Length)
        {
            start = 0;
            length = 0;
            return false;
        }

        start = _tokenStarts[tokenIndex];
        length = _tokenLengths[tokenIndex];
        return true;
    }
}

/// <summary>
/// Provides helper methods for building custom <see cref="ICommandOutput"/> implementations.
/// </summary>
public static class CommandOutputHelper
{
    /// <summary>
    /// Gets the full command path for a command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The full command path.</returns>
    public static string GetFullCommandPath(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command.GetFullCommandPath();
    }

    /// <summary>
    /// Gets the default usage syntax string for a command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The default usage syntax.</returns>
    public static string GetDefaultUsageSyntax(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command.GetDefaultUsageSyntax();
    }

    /// <summary>
    /// Gets the display name for an option value placeholder from its description.
    /// </summary>
    /// <param name="option">The option.</param>
    /// <param name="valueIndex">The 0-based value index for multi-value options.</param>
    /// <returns>The value name (for example <c>PORT</c>, <c>VALUE</c>).</returns>
    public static string GetOptionValueName(Option option, int valueIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (valueIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(valueIndex));
        return Command.GetArgumentNameCore(valueIndex, option.MaxValueCount, option.Description);
    }

    /// <summary>
    /// Gets only the description text from a descriptor description, stripping placeholder metadata.
    /// </summary>
    /// <param name="description">The descriptor description.</param>
    /// <returns>The normalized description text.</returns>
    public static string GetDescriptionText(string? description)
    {
        return Command.GetDescriptionCore(description);
    }

    /// <summary>
    /// Gets the active, visible options for a command (deduplicated across aliases).
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The visible options.</returns>
    public static IEnumerable<Option> GetVisibleOptions(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Command.GetActiveVisibleUniqueOptionsCore(command);
    }

    /// <summary>
    /// Gets the active, visible positional arguments for a command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The visible arguments.</returns>
    public static IEnumerable<CommandArgument> GetVisibleArguments(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        foreach (var argument in command.Arguments)
        {
            if (!argument.IsActive() || argument.Hidden)
                continue;
            yield return argument;
        }
    }

    /// <summary>
    /// Gets the active, visible sub-commands for a command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The visible sub-commands.</returns>
    public static IEnumerable<Command> GetVisibleSubCommands(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        foreach (var entry in command.SubCommands)
        {
            var sub = entry.Value;
            if (!sub.IsActive() || sub.Hidden)
                continue;
            yield return sub;
        }
    }

    /// <summary>
    /// Gets the default usage hint text for a command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>A plain-text usage hint.</returns>
    public static string GetHelpHint(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return $"Use `{command.GetFullCommandPath()} --help` for usage.";
    }

    /// <summary>
    /// Renders an invocation line by combining the command path and provided tokens.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="tokens">Invocation tokens to render.</param>
    /// <returns>A rendered invocation with token mappings.</returns>
    public static RenderedInvocation RenderInvocation(Command command, IReadOnlyList<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(tokens);

        var commandPath = command.GetFullCommandPath();
        var builder = new StringBuilder(commandPath);
        var starts = new int[tokens.Count];
        var lengths = new int[tokens.Count];

        for (var i = 0; i < tokens.Count; i++)
        {
            builder.Append(' ');
            starts[i] = builder.Length;
            var token = tokens[i] ?? string.Empty;
            builder.Append(token);
            lengths[i] = token.Length;
        }

        return new RenderedInvocation(builder.ToString(), starts, lengths);
    }

    /// <summary>
    /// Renders an underline marker for a token span.
    /// </summary>
    /// <param name="invocation">The rendered invocation.</param>
    /// <param name="span">The token span to underline.</param>
    /// <param name="underlineCharacter">The underline character.</param>
    /// <returns>The underline text, or an empty string when the span is invalid.</returns>
    public static string RenderUnderline(RenderedInvocation invocation, CommandTokenSpan span, char underlineCharacter = '^')
    {
        ArgumentNullException.ThrowIfNull(invocation);

        if (!invocation.TryGetTokenBounds(span.TokenIndex, out var tokenStart, out var tokenLength))
            return string.Empty;

        var relativeStart = span.Start;
        if (relativeStart < 0)
            relativeStart = 0;
        if (relativeStart > tokenLength)
            relativeStart = tokenLength;

        var length = span.Length;
        if (length <= 0)
            length = 1;
        if (relativeStart + length > tokenLength)
            length = Math.Max(1, tokenLength - relativeStart);

        return new string(' ', tokenStart + relativeStart) + new string(underlineCharacter, length);
    }
}
