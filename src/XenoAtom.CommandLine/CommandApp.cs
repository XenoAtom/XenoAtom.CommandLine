// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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

    /// <summary>
    /// Parses the specified arguments without invoking the resolved command action.
    /// Option and argument actions are still invoked.
    /// </summary>
    /// <param name="arguments">The arguments to parse.</param>
    /// <param name="runConfig">
    /// Optional run configuration. When null, output streams are set to <see cref="TextWriter.Null"/>
    /// to minimize side effects while parsing.
    /// </param>
    /// <returns>A parse result describing resolved command, values, and parsing errors.</returns>
    public ParseResult Parse(IEnumerable<string> arguments, CommandRunConfig? runConfig = null)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        runConfig ??= new CommandRunConfig { Out = TextWriter.Null, Error = TextWriter.Null };
        return ParseCore(arguments, runConfig, CreateDeferredOutput(runConfig));
    }

    /// <summary>
    /// Gets completion candidates for the specified tokenized command line.
    /// </summary>
    /// <param name="commandLine">
    /// A partially typed command line, typically excluding the executable name (e.g. <c>"hello --na"</c>).
    /// If the executable name is included, it is ignored when it matches <paramref name="commandName"/> or <see cref="Command.Name"/>.
    /// </param>
    /// <param name="commandName">The invocation name (e.g. <c>"mytool"</c>) to strip from the beginning of the token stream.</param>
    /// <remarks>
    /// This API is intentionally non-executing: it does not invoke option actions. It only inspects the declared command tree.
    /// </remarks>
    public IEnumerable<string> GetCompletions(string? commandLine = null, string? commandName = null)
    {
        commandLine ??= string.Empty;
        return GetCompletionsCore(commandLine, cursorPosition: commandLine.Length, commandName);
    }

    /// <summary>
    /// Gets completion candidates for a full command line and a cursor position within it.
    /// </summary>
    /// <param name="commandLine">The full command line as typed in the shell, typically including the executable name.</param>
    /// <param name="cursorPosition">The 0-based cursor position within <paramref name="commandLine"/>.</param>
    /// <param name="commandName">The invocation name (e.g. <c>"mytool"</c>) to strip from the beginning of the token stream.</param>
    public IEnumerable<string> GetCompletionsForLine(string commandLine, int cursorPosition, string? commandName = null)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        return GetCompletionsCore(commandLine, cursorPosition, commandName);
    }

    /// <summary>
    /// Gets completion candidates for a tokenized command line and the index of the token being completed.
    /// </summary>
    /// <param name="tokens">A tokenized command line, typically including the executable name as the first token.</param>
    /// <param name="tokenIndex">
    /// The 0-based index of the token being completed. Use <c>tokens.Count</c> when completing a new token (cursor after whitespace).
    /// </param>
    /// <param name="commandName">The invocation name (e.g. <c>"mytool"</c>) to strip from the beginning of the token stream.</param>
    public IEnumerable<string> GetCompletionsForTokens(IReadOnlyList<string> tokens, int tokenIndex, string? commandName = null)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if ((uint)tokenIndex > (uint)tokens.Count)
            throw new ArgumentOutOfRangeException(nameof(tokenIndex));

        var startIndex = 0;
        if (tokens.Count > 0)
        {
            if (!string.IsNullOrEmpty(commandName) && string.Equals(tokens[0], commandName, StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 1;
            }
            else if (string.Equals(tokens[0], Name, StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 1;
            }
        }

        var effectiveCount = tokens.Count - startIndex;
        var effectiveTokens = new List<string>(capacity: Math.Max(effectiveCount, 0));
        for (var i = startIndex; i < tokens.Count; i++)
        {
            effectiveTokens.Add(tokens[i]);
        }

        var effectiveTokenIndex = tokenIndex - startIndex;
        if (effectiveTokenIndex < 0) effectiveTokenIndex = 0;
        if (effectiveTokenIndex > effectiveTokens.Count) effectiveTokenIndex = effectiveTokens.Count;

        string currentToken;
        var contextTokenCount = effectiveTokenIndex;
        if (effectiveTokenIndex == effectiveTokens.Count)
        {
            currentToken = string.Empty;
        }
        else
        {
            currentToken = effectiveTokens[effectiveTokenIndex];
        }

        var state = ResolveCompletionState(effectiveTokens, contextTokenCount);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stateCompletions = new List<string>();
        GetCompletionsForState(state, currentToken, stateCompletions);

        foreach (var completion in stateCompletions)
        {
            if (seen.Add(completion))
            {
                yield return completion;
            }
        }
    }

    private IEnumerable<string> GetCompletionsCore(string commandLine, int cursorPosition, string? commandName)
    {
        cursorPosition = Math.Clamp(cursorPosition, 0, commandLine.Length);
        var prefix = cursorPosition == commandLine.Length ? commandLine : commandLine.Substring(0, cursorPosition);

        var tokens = new List<string>();
        Tokenize(prefix, tokens, out var endsWithWhitespace);

        // Strip executable name if present.
        if (tokens.Count > 0)
        {
            if (!string.IsNullOrEmpty(commandName) && string.Equals(tokens[0], commandName, StringComparison.OrdinalIgnoreCase))
            {
                tokens.RemoveAt(0);
            }
            else if (string.Equals(tokens[0], Name, StringComparison.OrdinalIgnoreCase))
            {
                tokens.RemoveAt(0);
            }
        }

        string currentToken;
        int contextTokenCount;
        if (tokens.Count == 0)
        {
            currentToken = string.Empty;
            contextTokenCount = 0;
        }
        else if (endsWithWhitespace)
        {
            currentToken = string.Empty;
            contextTokenCount = tokens.Count;
        }
        else
        {
            currentToken = tokens[^1];
            contextTokenCount = tokens.Count - 1;
        }

        var state = ResolveCompletionState(tokens, contextTokenCount);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stateCompletions = new List<string>();
        GetCompletionsForState(state, currentToken, stateCompletions);

        foreach (var completion in stateCompletions)
        {
            if (seen.Add(completion))
            {
                yield return completion;
            }
        }
    }

    private readonly struct CompletionState
    {
        public readonly Command Command;
        public readonly Option? PendingOption;
        public readonly int PendingRemaining;
        public readonly int PendingMax;
        public readonly int PositionalCount;

        public CompletionState(Command command, Option? pendingOption, int pendingRemaining, int pendingMax, int positionalCount)
        {
            Command = command;
            PendingOption = pendingOption;
            PendingRemaining = pendingRemaining;
            PendingMax = pendingMax;
            PositionalCount = positionalCount;
        }
    }

    private void GetCompletionsForState(CompletionState state, string currentToken, List<string> completions)
    {
        ArgumentNullException.ThrowIfNull(completions);

        var hasPrefix = currentToken.Length > 0;
        var isOptionToken = currentToken.Length > 0 && (currentToken[0] == '-' || currentToken[0] == '/');
        var optionPrefix = GetOptionPrefix(currentToken);
        var optionNamePrefix = optionPrefix is null ? string.Empty : currentToken.Substring(optionPrefix.Length);

        if (state.PendingOption is not null)
        {
            var option = state.PendingOption;
            var provider = option.ValueCompleter;
            if (provider is null)
            {
                return;
            }

            var valueIndex = state.PendingMax - state.PendingRemaining;
            if ((uint)valueIndex >= (uint)state.PendingMax)
            {
                return;
            }

            foreach (var candidate in FilterByPrefix(provider(valueIndex, currentToken), currentToken))
            {
                completions.Add(candidate);
            }
            return;
        }

        if (!isOptionToken)
        {
            foreach (var entry in state.Command.SubCommands)
            {
                var sub = entry.Value;
                if (!sub.IsActive() || sub.Hidden)
                    continue;

                if (!hasPrefix || sub.Name.StartsWith(currentToken, StringComparison.OrdinalIgnoreCase))
                {
                    completions.Add(sub.Name);
                }
            }

            foreach (var node in state.Command.Nodes)
            {
                if (node is not ArgumentSource source)
                    continue;

                if (!source.IsActive())
                    continue;

                foreach (var name in source.GetNames())
                {
                    if (!hasPrefix || name.StartsWith(currentToken, StringComparison.OrdinalIgnoreCase))
                    {
                        completions.Add(name);
                    }
                }
            }
        }

        // Also suggest options when starting a token (empty) or completing an option token.
        if (!isOptionToken && currentToken.Length != 0)
        {
            // Allow positional argument completions as well.
            foreach (var completion in GetArgumentCompletions(state.Command, state.PositionalCount, currentToken))
            {
                completions.Add(completion);
            }
            return;
        }

        if (isOptionToken && TryGetOptionParts(currentToken, out var flagLength, out var flag, out var optionNameSpan, out var sepIndex, out var value))
        {
            // Inline value completion: `--opt=...`
            if (sepIndex >= 0 && TryGetOption(state.Command, optionNameSpan, out var option) && option.IsActive() && option.ValueCompleter is not null)
            {
                var (inlineTokenPrefix, valueIndex, valuePrefix) = GetInlineValueCompletionContext(currentToken, flagLength, optionNameSpan.Length, option, value);
                if (inlineTokenPrefix is not null)
                {
                    foreach (var candidate in FilterByPrefix(option.ValueCompleter(valueIndex, valuePrefix), valuePrefix))
                    {
                        completions.Add(inlineTokenPrefix + candidate);
                    }
                    return;
                }
            }
        }

        var prefix = optionPrefix ?? "--";
        foreach (var option in GetUniqueOptions(state.Command))
        {
            if (!option.IsActive() || option.Hidden)
                continue;

            foreach (var optionName in option.GetNames())
            {
                if (optionPrefix is not null)
                {
                    if (optionNamePrefix.Length > 0 && !optionName.StartsWith(optionNamePrefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Completion policy:
                    // - `--` suggests long options only (`--help`, not `--h`).
                    // - `-` suggests short options for single-letter prefixes (`-h`, not `-help`),
                    //   but allows completing long options (as `--name`) when the user already started typing a long name.
                    if (optionPrefix == "--")
                    {
                        if (optionName.Length == 1)
                            continue;
                        completions.Add("--" + optionName);
                    }
                    else if (optionPrefix == "-")
                    {
                        if (optionNamePrefix.Length <= 1)
                        {
                            if (optionName.Length != 1)
                                continue;
                            completions.Add("-" + optionName);
                        }
                        else
                        {
                            if (optionName.Length == 1)
                                continue;
                            completions.Add("--" + optionName);
                        }
                    }
                    else
                    {
                        // Keep `/` and other prefixes as-is.
                        completions.Add(prefix + optionName);
                    }
                }
                else
                {
                    // No leading '-' in the current token: only suggest options if starting a new token.
                    if (currentToken.Length != 0)
                        continue;

                    completions.Add((optionName.Length == 1 ? "-" : "--") + optionName);
                }
            }
        }

        if (!isOptionToken)
        {
            foreach (var completion in GetArgumentCompletions(state.Command, state.PositionalCount, currentToken))
            {
                completions.Add(completion);
            }
        }
    }

    private static IEnumerable<Option> GetUniqueOptions(Command command)
    {
        var options = new HashSet<Option>();
        foreach (var entry in command.Options)
        {
            options.Add(entry.Value);
        }
        return options;
    }

    private static string? GetOptionPrefix(string token)
    {
        if (token.StartsWith("--", StringComparison.Ordinal))
            return "--";
        if (token.StartsWith("-", StringComparison.Ordinal))
            return "-";
        if (token.StartsWith("/", StringComparison.Ordinal))
            return "/";
        return null;
    }

    private Command ResolveCommandContext(List<string> tokens, int count)
    {
        Command current = this;

        var processOptions = true;
        Option? pending = null;
        var pendingRemaining = 0;
        var canSelectSubcommand = true;

        for (int i = 0; i < count; i++)
        {
            var token = tokens[i];

            if (pending != null)
            {
                pendingRemaining -= ConsumeValueToken(token, pending, pendingRemaining);
                if (pendingRemaining <= 0)
                {
                    pending = null;
                    pendingRemaining = 0;
                }
                continue;
            }

            if (processOptions && token == "--")
            {
                processOptions = false;
                canSelectSubcommand = false;
                continue;
            }

            if (canSelectSubcommand && current.SubCommands.TryGetValue(token, out var sub) && sub.IsActive())
            {
                current = sub;
                processOptions = true;
                pending = null;
                pendingRemaining = 0;
                canSelectSubcommand = true;
                continue;
            }

            if (processOptions && TryConsumeBundledOptions(token, current, ref pending, ref pendingRemaining))
            {
                continue;
            }

            if (processOptions && TryGetOptionParts(token, out _, out _, out var name, out var sepIndex, out var value))
            {
                var hasSep = sepIndex >= 0;
                if (TryGetOption(current, name, out var option) && option.IsActive())
                {
                    if (option.OptionValueType == OptionValueType.Required)
                    {
                        var remaining = option.MaxValueCount;
                        if (hasSep)
                        {
                            remaining -= ConsumeInlineValue(value, option, remaining);
                        }
                        else
                        {
                            // Required option without inline value: next token(s) are values.
                        }

                        if (remaining > 0)
                        {
                            pending = option;
                            pendingRemaining = remaining;
                        }
                    }
                }
                else if (!hasSep && TryGetBoolSuffixOption(name, current, out option))
                {
                    // Bool suffix options (`--flag+` / `--flag-`) consume the token and do not affect context.
                }
                continue;
            }

            // Non-option token.
            canSelectSubcommand = false;
        }

        return current;
    }

    private CompletionState ResolveCompletionState(IReadOnlyList<string> tokens, int count)
    {
        Command current = this;

        var processOptions = true;
        Option? pending = null;
        var pendingRemaining = 0;
        var pendingMax = 0;
        var canSelectSubcommand = true;
        var positionalCount = 0;

        for (int i = 0; i < count; i++)
        {
            var token = tokens[i];

            if (pending != null)
            {
                var consumed = ConsumeValueToken(token, pending, pendingRemaining);
                pendingRemaining -= consumed;
                if (pendingRemaining <= 0)
                {
                    pending = null;
                    pendingRemaining = 0;
                    pendingMax = 0;
                }
                continue;
            }

            if (processOptions && token == "--")
            {
                processOptions = false;
                canSelectSubcommand = false;
                continue;
            }

            if (canSelectSubcommand && current.SubCommands.TryGetValue(token, out var sub) && sub.IsActive())
            {
                current = sub;
                processOptions = true;
                pending = null;
                pendingRemaining = 0;
                pendingMax = 0;
                canSelectSubcommand = true;
                positionalCount = 0;
                continue;
            }

            if (processOptions && TryConsumeBundledOptions(token, current, ref pending, ref pendingRemaining))
            {
                if (pending is not null)
                {
                    pendingMax = pending.MaxValueCount;
                }
                continue;
            }

            if (processOptions && TryGetOptionParts(token, out _, out _, out var name, out var sepIndex, out var value))
            {
                var hasSep = sepIndex >= 0;
                if (TryGetOption(current, name, out var option) && option.IsActive())
                {
                    if (option.OptionValueType == OptionValueType.Required)
                    {
                        pendingMax = option.MaxValueCount;
                        var remaining = option.MaxValueCount;
                        if (hasSep)
                        {
                            remaining -= ConsumeInlineValue(value, option, remaining);
                        }

                        if (remaining > 0)
                        {
                            pending = option;
                            pendingRemaining = remaining;
                        }
                    }
                }
                else if (!hasSep && TryGetBoolSuffixOption(name, current, out option))
                {
                    // Bool suffix options (`--flag+` / `--flag-`) consume the token and do not affect context.
                }
                else
                {
                    // Unknown option token: treat it as a positional token for argument completion context.
                    canSelectSubcommand = false;
                    positionalCount++;
                }
                continue;
            }

            // Non-option token.
            canSelectSubcommand = false;
            positionalCount++;
        }

        return new CompletionState(current, pending, pendingRemaining, pendingMax, positionalCount);
    }

    private static IEnumerable<string> GetArgumentCompletions(Command command, int positionalIndex, string currentToken)
    {
        if (command.SubCommands.Count > 0)
            yield break;

        CommandArgument? argument = null;

        var activeArgs = new List<CommandArgument>();
        foreach (var arg in command.Arguments)
        {
            if (!arg.IsActive() || arg.Hidden)
                continue;
            activeArgs.Add(arg);
        }

        if (activeArgs.Count == 0)
            yield break;

        if (activeArgs[^1].IsRemainder)
        {
            argument = activeArgs[^1];
        }
        else if (activeArgs[^1].IsList)
        {
            var fixedCount = activeArgs.Count - 1;
            argument = positionalIndex < fixedCount ? activeArgs[positionalIndex] : activeArgs[^1];
        }
        else
        {
            if ((uint)positionalIndex >= (uint)activeArgs.Count)
                yield break;
            argument = activeArgs[positionalIndex];
        }

        var provider = argument.ValueCompleter;
        if (provider is null)
            yield break;

        foreach (var candidate in FilterByPrefix(provider(positionalIndex, currentToken), currentToken))
        {
            yield return candidate;
        }
    }

    private static IEnumerable<string> FilterByPrefix(IEnumerable<string> candidates, string prefix)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (prefix.Length == 0 || candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return candidate;
            }
        }
    }

    private static (string? TokenPrefix, int ValueIndex, string ValuePrefix) GetInlineValueCompletionContext(string token, int flagLength, int nameLength, Option option, ReadOnlySpan<char> fullValue)
    {
        var separators = option.ValueSeparators;
        if (separators == null || separators.Length == 0 || option.MaxValueCount <= 1)
        {
            var prefixLen = flagLength + nameLength + 1; // include '=' or ':'
            var inlineTokenPrefix = token.Substring(0, prefixLen);
            return (inlineTokenPrefix, 0, fullValue.ToString());
        }

        var lastSepIndex = -1;
        var lastSepLength = 0;
        for (var i = 0; i < separators.Length; i++)
        {
            var sep = separators[i].AsSpan();
            if (sep.Length == 0)
                continue;
            var idx = fullValue.LastIndexOf(sep, StringComparison.Ordinal);
            if (idx >= 0)
            {
                if (idx > lastSepIndex)
                {
                    lastSepIndex = idx;
                    lastSepLength = sep.Length;
                }
            }
        }

        if (lastSepIndex < 0)
        {
            var prefixLen = flagLength + nameLength + 1;
            var inlineTokenPrefix = token.Substring(0, prefixLen);
            return (inlineTokenPrefix, 0, fullValue.ToString());
        }

        var completedPrefix = fullValue[..lastSepIndex];
        var valueIndex = completedPrefix.Length == 0 ? 0 : CountSplitSegments(completedPrefix, separators, int.MaxValue);
        if ((uint)valueIndex >= (uint)option.MaxValueCount)
        {
            return (null, 0, string.Empty);
        }

        var segmentStart = lastSepIndex + lastSepLength;
        var tokenPrefixLen = (token.Length - fullValue.Length) + segmentStart;
        var inlineTokenPrefix2 = tokenPrefixLen <= token.Length ? token.Substring(0, tokenPrefixLen) : null;
        var valuePrefix = fullValue[segmentStart..].ToString();
        return (inlineTokenPrefix2, valueIndex, valuePrefix);
    }

    private static bool TryGetBoolSuffixOption(ReadOnlySpan<char> name, Command command, [NotNullWhen(true)] out Option? option)
    {
        option = null;
        if (name.Length < 2)
            return false;

        var last = name[^1];
        if (last != '+' && last != '-')
            return false;

        var baseName = name[..^1];
        if (!TryGetOption(command, baseName, out option))
            return false;

        return option.IsActive();
    }

    private static bool TryConsumeBundledOptions(string token, Command current, ref Option? pending, ref int pendingRemaining)
    {
        if (token.Length < 3)
            return false;

        if (token[0] != '-' || token[1] == '-')
            return false;

        if (token.IndexOfAny([':', '=']) >= 0)
            return false;

        for (int i = 1; i < token.Length; i++)
        {
            if (!TryGetOption(current, token.AsSpan(i, 1), out var option) || !option.IsActive())
            {
                return i != 1;
            }

            if (option.OptionValueType == OptionValueType.None)
            {
                continue;
            }

            // Remaining characters are the value for the option.
            var value = token.AsSpan(i + 1);

            if (option.OptionValueType == OptionValueType.Required)
            {
                var remaining = option.MaxValueCount;
                if (value.Length > 0)
                {
                    remaining -= ConsumeInlineValue(value, option, remaining);
                }

                if (remaining > 0)
                {
                    pending = option;
                    pendingRemaining = remaining;
                }
            }

            return true;
        }

        return true;
    }

    private static int ConsumeInlineValue(ReadOnlySpan<char> value, Option option, int maxValues)
    {
        if (maxValues <= 0)
            return 0;

        var separators = option.ValueSeparators;
        if (separators == null || separators.Length == 0)
            return 1;

        return CountSplitSegments(value, separators, maxValues);
    }

    private static int ConsumeValueToken(string token, Option option, int maxValues)
    {
        return ConsumeInlineValue(token.AsSpan(), option, maxValues);
    }

    private static int CountSplitSegments(ReadOnlySpan<char> value, string[] separators, int maxSegments)
    {
        if (maxSegments <= 1)
            return 1;

        // Fast path: all separators are single characters.
        var hasOnlySingleCharSeparators = true;
        for (var i = 0; i < separators.Length; i++)
        {
            if (separators[i].Length != 1)
            {
                hasOnlySingleCharSeparators = false;
                break;
            }
        }

        var segments = 1;
        if (hasOnlySingleCharSeparators)
        {
            Span<char> sepChars = separators.Length <= 8 ? stackalloc char[separators.Length] : new char[separators.Length];
            for (var i = 0; i < separators.Length; i++)
            {
                sepChars[i] = separators[i][0];
            }

            var start = 0;
            while (segments < maxSegments)
            {
                var next = value[start..].IndexOfAny(sepChars);
                if (next < 0)
                    break;
                start += next + 1;
                segments++;
            }
            return segments;
        }

        var searchStart = 0;
        while (segments < maxSegments)
        {
            int nextIndex = -1;
            int nextSepLength = 0;
            for (var i = 0; i < separators.Length; i++)
            {
                var sep = separators[i];
                var idx = value[searchStart..].IndexOf(sep.AsSpan(), StringComparison.Ordinal);
                if (idx < 0)
                    continue;
                idx += searchStart;
                if (nextIndex < 0 || idx < nextIndex)
                {
                    nextIndex = idx;
                    nextSepLength = sep.Length;
                }
            }

            if (nextIndex < 0)
                break;

            searchStart = nextIndex + nextSepLength;
            segments++;
        }

        return segments;
    }

    private static void Tokenize(string commandLine, List<string> tokens, out bool endsWithWhitespace)
    {
        endsWithWhitespace = commandLine.Length == 0 || char.IsWhiteSpace(commandLine[^1]);

        var current = new System.Text.StringBuilder(32);
        char quote = '\0';

        for (int i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];

            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }
                else
                {
                    current.Append(c);
                }
                continue;
            }

            if (c == '"' || c == '\'')
            {
                quote = c;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Length = 0;
                }
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }
    }

    private static string GetDefaultAppCommand()
    {
        // TODO: Fix this once there is a solution for https://github.com/dotnet/runtime/issues/101837
        return PathHelper.GetExeName(Environment.ProcessPath) ?? PathHelper.GetExeName(Assembly.GetEntryAssembly()?.GetName().Name) ?? "command";
    }
}
