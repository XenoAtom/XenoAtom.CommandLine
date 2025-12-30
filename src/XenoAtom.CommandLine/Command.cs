// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace XenoAtom.CommandLine;

/// <summary>
/// This class represents a command that can be executed. It can contain sub-commands, options and argument sources.
/// </summary>
public partial class Command  : CommandContainer, ICommandNodeDescriptor
{
    private readonly Dictionary<string, Command> _subCommands = new();
    private readonly Dictionary<string, Option> _options = new();
    private readonly Dictionary<char, Option> _shortOptions = new();
    private readonly List<CommandArgument> _arguments = new();
    private readonly List<ArgumentSource> _sources = new();
    private bool _hasCommandUsage;

    /// <summary>
    /// Initializes a new instance of <see cref="Command"/>.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="help">The help description of the command.</param>
    /// <param name="active">The active function to determine if the command is active.</param>
    public Command(string name, string? help = null, Func<bool>? active = null) : base(active)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Options = new ReadOnlyDictionary<string, Option>(_options);
        SubCommands = new ReadOnlyDictionary<string, Command>(_subCommands);
        Arguments = new ReadOnlyCollection<CommandArgument>(_arguments);

        Name = NormalizeCommandName(name);
        OptionsName = "Options";
        Description = help;
        Config = CommandConfig.Default;
    }

    /// <summary>
    /// Gets the name of this command.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets a boolean indicating if this command is hidden from help.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Gets the description of this command.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the options of this command.
    /// </summary>
    public ReadOnlyDictionary<string, Option> Options { get; }

    /// <summary>
    /// Gets the name of the options used when creating the usage help for this command.
    /// </summary>
    public string OptionsName { get; set; }

    /// <summary>
    /// Gets the sub-commands of this command.
    /// </summary>
    public ReadOnlyDictionary<string, Command> SubCommands { get; }

    /// <summary>
    /// Gets the positional arguments of this command.
    /// </summary>
    public ReadOnlyCollection<CommandArgument> Arguments { get; }

    /// <summary>
    /// Gets the configuration of this command inherited from the parent command.
    /// </summary>
    public CommandConfig Config { get; internal set; }

    /// <summary>
    /// Gets or sets the action to run when this command is executed.
    /// </summary>
    public Func<CommandRunContext, string[], ValueTask<int>>? Action { get; set; }

    /// <inheritdoc />
    protected override void AddImpl(CommandNode node)
    {
        base.AddImpl(node);

        if (node is Command command)
        {
            _subCommands.Add(command.Name, command);
            command.Config = Config;
        }
        else if (node is Option option)
        {
            foreach (var name in option.Names)
            {
                _options.Add(name, option);
                if (name.Length == 1)
                {
                    _shortOptions.Add(name[0], option);
                }
            }
        }
        else if (node is CommandArgument argument)
        {
            if (_arguments.Count > 0 && (_arguments[^1].Optional || _arguments[^1].IsList))
            {
                throw new InvalidOperationException($"Cannot add an argument `{argument}` after the last argument `{_arguments[^1]}` (only the last argument can be optional or a list).");
            }

            _arguments.Add(argument);
        }
        else if (node is ArgumentSource source)
        {
            _sources.Add(source);
        }
        else if (node is CommandUsage)
        {
            _hasCommandUsage = true;
        }
    }

    /// <summary>
    /// Creates a new option context for this command.
    /// </summary>
    /// <param name="runContext">The command run context.</param>
    /// <returns>A new option context for this command.</returns>
    protected virtual OptionContext CreateOptionContext(CommandRunContext runContext)
    {
        return new OptionContext(runContext, this);
    }

    /// <summary>
    /// Creates a new command context for this command.
    /// </summary>
    /// <param name="config">The command config.</param>
    /// <returns>A new command run context for this command.</returns>
    protected virtual CommandRunContext CreateCommandContext(CommandRunConfig config)
    {
        return new CommandRunContext(config);
    }

    /// <summary>
    /// Runs this command with the specified arguments and optional run configuration.
    /// </summary>
    /// <param name="arguments">The arguments for this command.</param>
    /// <param name="runConfig">The optional run configuration (for stdout, stderr...)</param>
    /// <returns>The result code of running this command.</returns>
    public virtual async ValueTask<int> RunAsync(IEnumerable<string> arguments, CommandRunConfig? runConfig = null)
    {
        runConfig ??= new CommandRunConfig();

        try
        {
            var commandContext = CreateCommandContext(runConfig);
            commandContext.ShouldShowHelp = false;
            commandContext.ShouldRunAfterParsingOptions = true;
            
            var extra = ParseOptions(commandContext, arguments);

            if (commandContext.ShouldShowHelp)
            {
                ShowHelp(runConfig);
                return 0;
            }

            if (SubCommands.Count > 0 && extra.Count > 0)
            {
                var subCommandName = extra[0];
                if (SubCommands.TryGetValue(subCommandName, out var subCommand) && subCommand.IsActive())
                {
                    extra.RemoveAt(0);
                    return await subCommand.RunAsync(extra, runConfig);
                }

                WriteUnknownCommandOrOption(runConfig, this, subCommandName);
                return 1;
            }

            if (commandContext.ShouldRunAfterParsingOptions)
            {
                extra = ParseArgumentsAndDefaultOption(commandContext, extra);
                if (Action == null)
                {
                    WriteUnknownOptions(runConfig,this, extra);
                    return 1;
                }
                else
                {
                    if (commandContext.ShouldShowLicenseOnRun)
                    {
                        var appCommand = GetCommandApp();
                        if (appCommand != null)
                        {
                            var licenseHeader = appCommand.LicenseHeader;
                            if (licenseHeader != null)
                            {
                                runConfig.Out.WriteLine(licenseHeader());
                            }
                        }
                    }
                    return await Action.Invoke(commandContext, extra.ToArray());
                }
            }
            else
            {
                return 0;
            }
        }
        catch (CommandException e)
        {
            WriteCommandException(runConfig, e);
            return 1;
        }
    }


    /// <summary>
    /// Gets the root command app from this command.
    /// </summary>
    /// <returns>The root command app from this command. Might be null if a command is not yet attached to a <see cref="CommandApp"/>.</returns>
    public CommandApp? GetCommandApp()
    {
        for (var c = (CommandNode)this; c != null; c = c.Parent)
        {
            if (c is CommandApp appCommand)
            {
                return appCommand;
            }
        }
        return null;
    }

    /// <summary>
    /// Shows the help for this command.
    /// </summary>
    /// <param name="runConfig">The runtime configuration for stdout/stderr.</param>
    public void ShowHelp(CommandRunConfig? runConfig = null)
    {
        runConfig ??= new CommandRunConfig();
        var _ = Config.Localizer;
        var o = runConfig.Out;

        if (this is CommandApp appCommand)
        {
            var header = appCommand.LicenseHeader;
            if (header != null)
            {
                o.WriteLine(header());
            }
        }

        if (!_hasCommandUsage)
        {
            o.WriteLine(GetDefaultUsage(runConfig));
        }

        foreach (var p in Nodes)
        {
            int written = 0;

            // If the node is not active, we skip it
            if (!p.IsActive())
            {
                continue;
            }

            if (p is Command co)
            {
                if (co.Hidden)
                {
                    continue;
                }

                ShowHelp(runConfig, co, co.Name);
                continue;
            }

            bool isIndented = false;

            if (p is Option op)
            {
                if (op.Hidden)
                    continue;

                if (!WriteOptionPrototype(o, op, ref written))
                    continue;

                isIndented = true;
            }
            else if (p is ArgumentSource src)
            {
                string[] names = src.GetNames();
                Write(o, ref written, "  ");
                Write(o, ref written, names[0]);
                for (int i = 1; i < names.Length; ++i)
                {
                    Write(o, ref written, ", ");
                    Write(o, ref written, names[i]);
                }

                isIndented = true;
            }
            else if (p is CommandArgument arg)
            {
                if (arg.Hidden)
                    continue;

                Write(o, ref written, "  ");
                Write(o, ref written, arg.GetDisplayName());
                isIndented = true;
            }

            if (isIndented)
            {
                if (written < runConfig.OptionWidth)
                    o.Write(new string(' ', runConfig.OptionWidth - written));
                else
                {
                    o.WriteLine();
                    o.Write(new string(' ', runConfig.OptionWidth));
                }
            }

            if (p is ICommandNodeDescriptor descriptor)
            {
                if (isIndented)
                {
                    WriteDescription(o, descriptor.Description, new string(' ', runConfig.OptionWidth + 2), runConfig.DescriptionFirstWidth, runConfig.DescriptionRemWidth);
                }
                else
                {
                    var description = descriptor.Description;
                    if (description is null && descriptor is CommandUsage)
                    {
                        description = GetDefaultUsage(runConfig);
                    }
                    WriteDescription(o, description, "", runConfig.Width, runConfig.Width);
                }
            }
        }
    }

    /// <summary>
    /// Gets the full command path from this command as a string. E.g `myexe mycommand subcommand` 
    /// </summary>
    /// <returns></returns>
    public string GetFullCommandPath()
    {
        var path = new Stack<string>();
        for (var c = (CommandNode)this; c != null; c = c.Parent)
        {
            if (c is Command command)
            {
                path.Push(command.Name);
            }
        }
        return string.Join(" ", path);
    }

    private List<string> ParseOptions(CommandRunContext runContext, IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        OptionContext c = CreateOptionContext(runContext);
        c.OptionIndex = -1;
        bool process = true;

        List<string> unprocessed = new List<string>();
        ArgumentEnumerator ae = new ArgumentEnumerator(arguments);
        foreach (string argument in ae)
        {
            ++c.OptionIndex;

            if (process && c.Option != null)
            {
                ParseValue(argument, c);
                continue;
            }

            if (argument == "--")
            {
                if (!process)
                {
                    unprocessed.Add(argument);
                }
                process = false;
                continue;
            }

            if (process && _subCommands.ContainsKey(argument))
            {
                unprocessed.Add(argument);
                process = false;
                continue;
            }

            if (process)
            {
                if (AddSource(ae, argument))
                    continue;

                if (!ParseOption(argument, c))
                {
                    if (Config.StrictOptionParsing && TryGetOptionParts(argument, out _, out _, out _, out _, out _))
                    {
                        throw new OptionException(Config.Localizer($"Unknown option: {argument}"), argument);
                    }

                    unprocessed.Add(argument);
                }
            }
            else
            {
                unprocessed.Add(argument);
            }
        }
        if (c.Option != null)
            c.Option.Invoke(c);

        return unprocessed;
    }

    private List<string> ParseArgumentsAndDefaultOption(CommandRunContext runContext, List<string> arguments)
    {
        if (_arguments.Count == 0)
        {
            if (arguments.Count > 0)
            {
                throw new CommandException(Config.Localizer($"Unexpected argument `{arguments[0]}`."));
            }
            return arguments;
        }

        var activeArgs = new List<CommandArgument>();
        foreach (var argument in _arguments)
        {
            if (!argument.IsActive())
                continue;
            activeArgs.Add(argument);
        }

        if (activeArgs.Count == 0)
        {
            if (arguments.Count > 0)
            {
                throw new CommandException(Config.Localizer($"Unexpected argument `{arguments[0]}`."));
            }

            return arguments;
        }

        if (activeArgs[^1].IsRemainder)
        {
            var fixedCount = activeArgs.Count - 1;

            if (arguments.Count < fixedCount)
            {
                var missing = activeArgs[arguments.Count];
                throw new CommandException(Config.Localizer($"Missing required argument `{missing.GetDisplayName()}`."));
            }

            var argumentContext = new CommandArgumentContext(runContext, this)
            {
                ArgumentIndex = -1
            };

            for (var i = 0; i < fixedCount; i++)
            {
                argumentContext.Argument = activeArgs[i];
                argumentContext.ArgumentValue = arguments[i];
                argumentContext.ArgumentIndex = i;
                activeArgs[i].Invoke(argumentContext);
            }

            var remainingArguments = new List<string>();
            for (var i = fixedCount; i < arguments.Count; i++)
            {
                remainingArguments.Add(arguments[i]);
            }

            return remainingArguments;
        }

        if (activeArgs[^1].IsList)
        {
            var listArg = activeArgs[^1];
            var fixedCount = activeArgs.Count - 1;
            var minTotal = fixedCount + listArg.MinValueCount;

            if (arguments.Count < minTotal)
            {
                var missing = arguments.Count < fixedCount ? activeArgs[arguments.Count] : listArg;
                throw new CommandException(Config.Localizer($"Missing required argument `{missing.GetDisplayName()}`."));
            }

            var argumentContext = new CommandArgumentContext(runContext, this)
            {
                ArgumentIndex = -1
            };

            for (var i = 0; i < fixedCount; i++)
            {
                argumentContext.Argument = activeArgs[i];
                argumentContext.ArgumentValue = arguments[i];
                argumentContext.ArgumentIndex = i;
                activeArgs[i].Invoke(argumentContext);
            }

            for (var i = fixedCount; i < arguments.Count; i++)
            {
                argumentContext.Argument = listArg;
                argumentContext.ArgumentValue = arguments[i];
                argumentContext.ArgumentIndex = i;
                listArg.Invoke(argumentContext);
            }

            return new List<string>();
        }

        var requiredCount = activeArgs.Count;
        if (activeArgs[^1].Optional)
            requiredCount--;

        if (arguments.Count < requiredCount)
        {
            var missing = activeArgs[arguments.Count];
            throw new CommandException(Config.Localizer($"Missing required argument `{missing.GetDisplayName()}`."));
        }

        var ctx = new CommandArgumentContext(runContext, this)
        {
            ArgumentIndex = -1
        };

        var consumed = Math.Min(arguments.Count, activeArgs.Count);
        for (var i = 0; i < consumed; i++)
        {
            ctx.Argument = activeArgs[i];
            ctx.ArgumentValue = arguments[i];
            ctx.ArgumentIndex = i;
            activeArgs[i].Invoke(ctx);
        }

        for (var i = consumed; i < activeArgs.Count; i++)
        {
            var argument = activeArgs[i];
            if (!argument.Optional)
                throw new InvalidOperationException($"Missing required argument `{argument.Prototype}` should have been detected.");

            ctx.Argument = argument;
            ctx.ArgumentValue = null;
            ctx.ArgumentIndex = i;
            argument.Invoke(ctx);
        }

        var remaining = new List<string>();
        for (var i = consumed; i < arguments.Count; i++)
        {
            remaining.Add(arguments[i]);
        }

        if (remaining.Count > 0)
        {
            throw new CommandException(Config.Localizer($"Unexpected argument `{remaining[0]}`."));
        }

        return new List<string>();
    }

    private bool AddSource(ArgumentEnumerator ae, string argument)
    {
        foreach (ArgumentSource source in _sources)
        {
            if (!source.TryGetArguments(argument, out var replacement))
                continue;
            ae.Add(replacement);
            return true;
        }
        return false;
    }

    private bool AddSource(List<IEnumerator<string>> sources, string argument)
    {
        foreach (ArgumentSource source in _sources)
        {
            if (!source.TryGetArguments(argument, out var replacement))
                continue;
            sources.Add(replacement.GetEnumerator());
            return true;
        }
        return false;
    }

    private protected static bool TryGetOptionParts(
        string argument,
        out int flagLength,
        [NotNullWhen(true)] out string? flag,
        out ReadOnlySpan<char> name,
        out int sepIndex,
        out ReadOnlySpan<char> value)
    {
        flagLength = 0;
        flag = null;
        name = default;
        sepIndex = -1;
        value = default;

        if (string.IsNullOrEmpty(argument))
            return false;

        ReadOnlySpan<char> span = argument.AsSpan();
        if (span.Length >= 2 && span[0] == '-' && span[1] == '-')
        {
            flagLength = 2;
            flag = "--";
        }
        else if (span[0] == '-' || span[0] == '/')
        {
            flagLength = 1;
            flag = span[0] == '-' ? "-" : "/";
        }
        else
        {
            return false;
        }

        if (span.Length <= flagLength)
            return false;

        var rest = span[flagLength..];
        sepIndex = rest.IndexOfAny(':', '=');
        if (sepIndex < 0)
        {
            name = rest;
            return name.Length > 0;
        }

        name = rest[..sepIndex];
        value = rest[(sepIndex + 1)..];
        return name.Length > 0;
    }

    private protected static bool TryGetOption(Command command, ReadOnlySpan<char> name, [NotNullWhen(true)] out Option? option)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command.TryGetOption(name, out option);
    }

    private bool ParseOption(string argument, OptionContext c)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (c.Option != null)
        {
            ParseValue(argument, c);
            return true;
        }

        if (!TryGetOptionParts(argument, out var flagLength, out var flag, out var name, out var sepIndex, out var value))
            return false;

        Option? p;
        if (TryGetOption(name, out p) && p.IsActive())
        {
            c.OptionName = sepIndex < 0 ? argument : argument.Substring(0, flagLength + name.Length);
            c.Option = p;
            switch (p.OptionValueType)
            {
                case OptionValueType.None:
                    c.OptionValues.Add(name.ToString());
                    c.Option.Invoke(c);
                    break;
                case OptionValueType.Optional:
                case OptionValueType.Required:
                    ParseValue(sepIndex < 0 ? null : value.ToString(), c);
                    break;
            }
            return true;
        }

        // no match; is it a bool option?
        if (TryParseBool(argument, name, c))
            return true;
        // is it a bundled option?
        if (TryParseBundledValue(flag, argument.AsSpan(flagLength), c, argument))
            return true;

        return false;
    }

    private void ParseValue(string? option, OptionContext c)
    {
        if (option != null)
        {
            var separators = c.Option!.ValueSeparators;
            if (separators == null)
            {
                c.OptionValues.Add(option);
            }
            else
            {
                AddSplitOptionValues(option, separators, c.Option.MaxValueCount - c.OptionValues.Count, c.OptionValues);
            }
        }

        if (c.OptionValues.Count == c.Option!.MaxValueCount ||
            c.Option.OptionValueType == OptionValueType.Optional)
            c.Option.Invoke(c);
        else if (c.OptionValues.Count > c.Option.MaxValueCount)
        {
            throw new OptionException(Config.Localizer(string.Format("Error: Found {0} option values when expecting {1}.", c.OptionValues.Count, c.Option.MaxValueCount)), c.OptionName!);
        }
    }

    private bool TryParseBool(string option, ReadOnlySpan<char> name, OptionContext c)
    {
        if (name.Length < 2)
            return false;

        var last = name[^1];
        if (last != '+' && last != '-')
            return false;

        var baseName = name[..^1];
        if (!TryGetOption(baseName, out var p) || !p.IsActive())
            return false;

        string? v = last == '+' ? option : null;
        c.OptionName = option;
        c.Option = p;
        c.OptionValues.Add(v);
        p.Invoke(c);
        return true;
    }

    private bool TryGetOption(ReadOnlySpan<char> name, [NotNullWhen(true)] out Option? option)
    {
        if (name.Length == 1)
        {
            return _shortOptions.TryGetValue(name[0], out option);
        }

#if NET10_0_OR_GREATER
        return _options.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out option);
#else
        return _options.TryGetValue(name.ToString(), out option);
#endif
    }

    private static void AddSplitOptionValues(string option, string[] separators, int maxSplitCount, OptionValueCollection target)
    {
        if (maxSplitCount <= 1 || separators.Length == 0)
        {
            target.Add(option);
            return;
        }

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

        if (hasOnlySingleCharSeparators)
        {
            Span<char> sepChars = separators.Length <= 8 ? stackalloc char[separators.Length] : new char[separators.Length];
            for (var i = 0; i < separators.Length; i++)
            {
                sepChars[i] = separators[i][0];
            }

            var start = 0;
            var remainingSegments = maxSplitCount;
            while (remainingSegments > 1)
            {
                var next = option.AsSpan(start).IndexOfAny(sepChars);
                if (next < 0)
                    break;
                next += start;
                target.Add(option.Substring(start, next - start));
                start = next + 1;
                remainingSegments--;
            }

            target.Add(start == 0 ? option : option.Substring(start));
            return;
        }

        // Fallback for multi-character separators.
        var segmentStart = 0;
        var remaining = maxSplitCount;
        while (remaining > 1)
        {
            int nextIndex = -1;
            int nextSepLength = 0;
            for (var i = 0; i < separators.Length; i++)
            {
                var sep = separators[i];
                var idx = option.IndexOf(sep, segmentStart, StringComparison.Ordinal);
                if (idx < 0)
                    continue;
                if (nextIndex < 0 || idx < nextIndex)
                {
                    nextIndex = idx;
                    nextSepLength = sep.Length;
                }
            }

            if (nextIndex < 0)
                break;

            target.Add(option.Substring(segmentStart, nextIndex - segmentStart));
            segmentStart = nextIndex + nextSepLength;
            remaining--;
        }

        target.Add(segmentStart == 0 ? option : option.Substring(segmentStart));
    }

    private bool TryParseBundledValue(string f, ReadOnlySpan<char> bundle, OptionContext c, string originalToken)
    {
        if (f != "-")
            return false;

        string? bundleString = null;
        for (int i = 0; i < bundle.Length; ++i)
        {
            char shortName = bundle[i];
            string rn = shortName.ToString();
            if (!_shortOptions.TryGetValue(shortName, out var p) || !p.IsActive())
            {
                if (i == 0)
                    return false;
                throw new OptionException(string.Format(Config.Localizer("Cannot use unregistered option '{0}' in bundle '{1}'."), rn, originalToken), string.Empty);
            }

            switch (p.OptionValueType)
            {
                case OptionValueType.None:
                    bundleString ??= bundle.ToString();
                    Invoke(c, string.Concat('-', shortName), bundleString, p);
                    break;
                case OptionValueType.Optional:
                case OptionValueType.Required:
                    {
                        string v = bundle[(i + 1)..].ToString();
                        c.Option = p;
                        c.OptionName = string.Concat('-', shortName);
                        ParseValue(v.Length != 0 ? v : null, c);
                        return true;
                    }
                default:
                    throw new InvalidOperationException("Unknown OptionValueType: " + p.OptionValueType);
            }
        }
        return true;
    }

    private static void Invoke(OptionContext c, string name, string value, Option option)
    {
        c.OptionName = name;
        c.Option = option;
        c.OptionValues.Add(value);
        option.Invoke(c);
    }

    private void WriteUnknownCommandOrOption(CommandRunConfig runConfig, Command command, string unknownCommand)
    {
        var fullCommandName = GetFullCommandPath();
        runConfig.Error.WriteLine(command.Config.Localizer($"{fullCommandName}: Unknown command or option: {unknownCommand}"));

        if (TryGetUnknownTokenDetails(command, unknownCommand, out var details))
        {
            if (details.InactiveExactMatchMessage is not null)
            {
                runConfig.Error.WriteLine(command.Config.Localizer(details.InactiveExactMatchMessage));
            }

            if (details.Suggestions.Count > 0)
            {
                runConfig.Error.WriteLine(command.Config.Localizer($"Did you mean: {string.Join(", ", details.Suggestions)}"));
            }
        }
        runConfig.Error.WriteLine(command.Config.Localizer($"Use `{fullCommandName} --help` for usage."));
    }

    private void WriteCommandException(CommandRunConfig runConfig, CommandException e)
    {
        var fullCommandName = GetFullCommandPath();
        runConfig.Error.WriteLine($"{fullCommandName}: {e.Message}");

        if (e is OptionException optionException &&
            !string.IsNullOrWhiteSpace(optionException.OptionName) &&
            TryGetUnknownTokenDetails(this, optionException.OptionName, out var details))
        {
            if (details.InactiveExactMatchMessage is not null)
            {
                runConfig.Error.WriteLine(Config.Localizer(details.InactiveExactMatchMessage));
            }

            if (details.Suggestions.Count > 0)
            {
                runConfig.Error.WriteLine(Config.Localizer($"Did you mean: {string.Join(", ", details.Suggestions)}"));
            }
        }
        runConfig.Error.WriteLine(Config.Localizer($"Use `{fullCommandName} --help` for usage."));
    }

    private void WriteUnknownOptions(CommandRunConfig runConfig, Command command, List<string> unknownOptions)
    {
        var fullCommandName = GetFullCommandPath();
        foreach (var unknownOption in unknownOptions)
        {
            runConfig.Error.WriteLine(command.Config.Localizer($"{fullCommandName}: Unknown option: {unknownOption}"));

            if (TryGetUnknownTokenDetails(command, unknownOption, out var details))
            {
                if (details.InactiveExactMatchMessage is not null)
                {
                    runConfig.Error.WriteLine(command.Config.Localizer(details.InactiveExactMatchMessage));
                }

                if (details.Suggestions.Count > 0)
                {
                    runConfig.Error.WriteLine(command.Config.Localizer($"Did you mean: {string.Join(", ", details.Suggestions)}"));
                }
            }
        }
        runConfig.Error.WriteLine(command.Config.Localizer($"Use `{fullCommandName} --help` for usage."));
    }

    private readonly struct UnknownTokenDetails
    {
        public readonly List<string> Suggestions;
        public readonly string? InactiveExactMatchMessage;

        public UnknownTokenDetails(List<string> suggestions, string? inactiveExactMatchMessage)
        {
            Suggestions = suggestions;
            InactiveExactMatchMessage = inactiveExactMatchMessage;
        }
    }

    private static bool TryGetUnknownTokenDetails(Command command, string token, out UnknownTokenDetails details)
    {
        details = default;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var suggestions = new List<string>(capacity: 3);
        string? inactiveExactMatchMessage = null;

        // Option-like token?
        if (TryGetOptionParts(token, out _, out var flag, out var name, out _, out _))
        {
            if (TryGetOption(command, name, out var exactOption))
            {
                if (exactOption.IsActive())
                {
                    // Not an unknown option (may still have failed parsing for other reasons).
                    return false;
                }

                inactiveExactMatchMessage = $"Note: `{token}` matches an option that is currently inactive in this context.";
            }

            foreach (var suggested in GetOptionSuggestions(command, flag!, name.ToString()))
            {
                suggestions.Add(suggested);
                if (suggestions.Count >= 3) break;
            }

            details = new UnknownTokenDetails(suggestions, inactiveExactMatchMessage);
            return suggestions.Count > 0 || inactiveExactMatchMessage is not null;
        }

        // Command-like token.
        if (command.SubCommands.TryGetValue(token, out var exactCommand))
        {
            if (exactCommand.IsActive())
            {
                // Not an unknown command (may have failed for other reasons).
                return false;
            }

            inactiveExactMatchMessage = $"Note: `{token}` matches a command that is currently inactive in this context.";
        }

        foreach (var suggested in GetCommandSuggestions(command, token))
        {
            suggestions.Add(suggested);
            if (suggestions.Count >= 3) break;
        }

        details = new UnknownTokenDetails(suggestions, inactiveExactMatchMessage);
        return suggestions.Count > 0 || inactiveExactMatchMessage is not null;
    }

    private static IEnumerable<string> GetCommandSuggestions(Command command, string token)
    {
        return GetPrefixSuggestions(token, GetActiveVisibleSubCommands(command), maxSuggestions: 3);
    }

    private static IEnumerable<string> GetOptionSuggestions(Command command, string flag, string typedName)
    {
        // Match completion policy:
        // - `--` suggests long options only (`--help`, not `--h`).
        // - `-` suggests short options for single-letter prefixes (`-h`, not `-help`),
        //   but allows completing long options (as `--name`) when the user already started typing a long name.
        // - `/` keeps `/` as the prefix for all suggestions.
        if (flag == "--")
        {
            foreach (var name in GetPrefixSuggestions(typedName, GetActiveVisibleLongOptionNames(command), maxSuggestions: 3))
            {
                yield return "--" + name;
            }
            yield break;
        }

        if (flag == "-")
        {
            if (typedName.Length <= 1)
            {
                foreach (var name in GetPrefixSuggestions(typedName, GetActiveVisibleShortOptionNames(command), maxSuggestions: 3))
                {
                    yield return "-" + name;
                }
                yield break;
            }

            foreach (var name in GetPrefixSuggestions(typedName, GetActiveVisibleLongOptionNames(command), maxSuggestions: 3))
            {
                yield return "--" + name;
            }
            yield break;
        }

        foreach (var name in GetPrefixSuggestions(typedName, GetActiveVisibleAllOptionNames(command), maxSuggestions: 3))
        {
            yield return flag + name;
        }
    }

    private static IEnumerable<string> GetActiveVisibleSubCommands(Command command)
    {
        foreach (var entry in command.SubCommands)
        {
            var sub = entry.Value;
            if (!sub.IsActive() || sub.Hidden)
                continue;
            yield return sub.Name;
        }
    }

    private static IEnumerable<string> GetActiveVisibleAllOptionNames(Command command)
    {
        foreach (var option in GetActiveVisibleUniqueOptions(command))
        {
            foreach (var name in option.GetNames())
            {
                yield return name;
            }
        }
    }

    private static IEnumerable<string> GetActiveVisibleLongOptionNames(Command command)
    {
        foreach (var option in GetActiveVisibleUniqueOptions(command))
        {
            foreach (var name in option.GetNames())
            {
                if (name.Length > 1) yield return name;
            }
        }
    }

    private static IEnumerable<string> GetActiveVisibleShortOptionNames(Command command)
    {
        foreach (var option in GetActiveVisibleUniqueOptions(command))
        {
            foreach (var name in option.GetNames())
            {
                if (name.Length == 1) yield return name;
            }
        }
    }

    private static IEnumerable<Option> GetActiveVisibleUniqueOptions(Command command)
    {
        var seen = new HashSet<Option>();
        foreach (var entry in command.Options)
        {
            var option = entry.Value;
            if (!seen.Add(option))
                continue;
            if (!option.IsActive() || option.Hidden)
                continue;
            yield return option;
        }
    }

    private static IEnumerable<string> GetPrefixSuggestions(string token, IEnumerable<string> candidates, int maxSuggestions)
    {
        if (token.Length == 0)
            yield break;

        foreach (var candidate in candidates)
        {
            if (!candidate.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return candidate;
            if (--maxSuggestions == 0)
                yield break;
        }
    }

    private string GetDefaultUsage(CommandRunConfig runConfig)
    {
        var usage = new StringBuilder();
        var _ = Config.Localizer;
        usage.Append(_("Usage: "));
        usage.Append(GetFullCommandPath());
        var syntax = GetDefaultUsageSyntax();
        if (syntax.Length > 0)
        {
            usage.Append(' ');
            usage.Append(syntax);
        }
        return usage.ToString();
    }

    internal string GetDefaultUsageSyntax()
    {
        var _ = Config.Localizer;

        var hasVisibleOptions = false;
        foreach (var node in Nodes)
        {
            if (node is not Option option)
                continue;
            if (!option.IsActive() || option.Hidden)
                continue;
            hasVisibleOptions = true;
            break;
        }

        var hasVisibleSubCommands = false;
        foreach (var node in Nodes)
        {
            if (node is not Command command)
                continue;
            if (!command.IsActive() || command.Hidden)
                continue;
            hasVisibleSubCommands = true;
            break;
        }

        var sb = new StringBuilder();

        if (hasVisibleOptions)
        {
            sb.Append(_("[options]"));
        }

        if (hasVisibleSubCommands)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(_("<command>"));
            return sb.ToString();
        }

        var hasListArgument = false;
        foreach (var argument in _arguments)
        {
            if (!argument.IsActive() || argument.Hidden)
                continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(argument.GetDisplayName());
            hasListArgument = argument.IsList;
        }

        return sb.ToString();
    }
    

    private void ShowHelp(CommandRunConfig runConfig, Command c, string commandName)
    {
        var o = runConfig.Out;
        var name = new string(' ', 2) + (commandName ?? c.Name);
        if (name.Length < runConfig.OptionWidth - 1)
        {
            WriteDescription(o, name + new string(' ', runConfig.OptionWidth - name.Length) + c.Description, runConfig.CommandHelpIndentRemaining, runConfig.Width, runConfig.DescriptionRemWidth);
        }
        else
        {
            WriteDescription(o, name, "", runConfig.Width, runConfig.Width);
            WriteDescription(o, runConfig.CommandHelpIndentStart + c.Description, runConfig.CommandHelpIndentRemaining, runConfig.Width, runConfig.DescriptionRemWidth);
        }
    }

    private void WriteDescription(TextWriter o, string? value, string prefix, int firstWidth, int remWidth)
    {
        bool indent = false;
        foreach (string line in GetLines(Config.Localizer(GetDescription(value)), firstWidth, remWidth))
        {
            if (indent)
                o.Write(prefix);
            o.WriteLine(line);
            indent = true;
        }
    }

    private bool WriteOptionPrototype(TextWriter o, Option p, ref int written)
    {
        string[] names = p.Names;

        int i = 0;
        if (i == names.Length)
            return false;

        if (names[i].Length == 1)
        {
            Write(o, ref written, "  -");
            Write(o, ref written, names[0]);
        }
        else
        {
            Write(o, ref written, "      --");
            Write(o, ref written, names[0]);
        }

        for (i += 1;
             i < names.Length;
             i++)
        {
            Write(o, ref written, ", ");
            Write(o, ref written, names[i].Length == 1 ? "-" : "--");
            Write(o, ref written, names[i]);
        }

        if (p.OptionValueType == OptionValueType.Optional ||
            p.OptionValueType == OptionValueType.Required)
        {
            if (p.OptionValueType == OptionValueType.Optional)
            {
                Write(o, ref written, Config.Localizer("["));
            }

            Write(o, ref written, Config.Localizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
            string sep = p.ValueSeparators != null && p.ValueSeparators.Length > 0
                ? p.ValueSeparators[0]
                : " ";
            for (int c = 1; c < p.MaxValueCount; ++c)
            {
                Write(o, ref written, Config.Localizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
            }

            if (p.OptionValueType == OptionValueType.Optional)
            {
                Write(o, ref written, Config.Localizer("]"));
            }
        }

        return true;
    }

    private static void Write(TextWriter o, ref int n, string s)
    {
        n += s.Length;
        o.Write(s);
    }
    
    private static string GetArgumentName(int index, int maxIndex, string? description)
    {
        if (description is not null)
        {
            var indexText = maxIndex > 1 ? index.ToString(CultureInfo.InvariantCulture) : null;

            for (int i = 0; i < description.Length; i++)
            {
                if (description[i] == '{')
                {
                    // Ignore escaped "{{".
                    if (i + 1 < description.Length && description[i + 1] == '{')
                    {
                        i++;
                        continue;
                    }

                    var start = i + 1;
                    var end = -1;
                    for (int j = start; j < description.Length; j++)
                    {
                        var c = description[j];
                        if (c == '{')
                        {
                            // Not a simple placeholder (nested '{'), skip.
                            break;
                        }

                        if (c == '}')
                        {
                            // Ignore escaped "}}".
                            if (j + 1 < description.Length && description[j + 1] == '}')
                            {
                                break;
                            }

                            end = j;
                            break;
                        }
                    }

                    if (end < 0)
                        continue;

                    i = end;
                    var content = description.AsSpan(start, end - start);
                    if (content.Length == 0)
                        continue;

                    if (maxIndex == 1)
                    {
                        var lastColon = content.LastIndexOf(':');
                        var argNameSpan = lastColon >= 0 ? content[(lastColon + 1)..] : content;
                        if (argNameSpan.Length > 0)
                            return argNameSpan.ToString();
                        continue;
                    }

                    var colonIndex = content.IndexOf(':');
                    if (colonIndex <= 0)
                        continue;

                    // Only accept "{i:name}" (exactly one ':').
                    if (content[(colonIndex + 1)..].IndexOf(':') >= 0)
                        continue;

                    if (!content[..colonIndex].SequenceEqual(indexText.AsSpan()))
                        continue;

                    var argName = content[(colonIndex + 1)..];
                    if (argName.Length > 0)
                        return argName.ToString();
                }
                else if (description[i] == '}' && i + 1 < description.Length && description[i + 1] == '}')
                {
                    // Ignore escaped "}}".
                    i++;
                }
            }
        }

        return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
    }

    private static string GetDescription(string? description)
    {
        if (description is null)
            return string.Empty;

        StringBuilder sb = new StringBuilder(description.Length);
        int start = -1;
        for (int i = 0; i < description.Length; ++i)
        {
            switch (description[i])
            {
                case '{':
                    if (i == start)
                    {
                        sb.Append('{');
                        start = -1;
                    }
                    else if (start < 0)
                        start = i + 1;
                    break;
                case '}':
                    if (start < 0)
                    {
                        if ((i + 1) == description.Length || description[i + 1] != '}')
                            throw new InvalidOperationException("Invalid option description: " + description);
                        ++i;
                        sb.Append("}");
                    }
                    else
                    {
                        sb.Append(description.Substring(start, i - start));
                        start = -1;
                    }
                    break;
                case ':':
                    if (start < 0)
                        goto default;
                    start = i + 1;
                    break;
                default:
                    if (start < 0)
                        sb.Append(description[i]);
                    break;
            }
        }
        return sb.ToString();
    }

    private static IEnumerable<string> GetLines(string description, int firstWidth, int remWidth)
    {
        return StringCoda.WrappedLines(description, firstWidth, remWidth);
    }

    private static string NormalizeCommandName(string name)
    {
        var value = new StringBuilder(name.Length);
        var space = false;
        for (int i = 0; i < name.Length; ++i)
        {
            if (!char.IsWhiteSpace(name, i))
            {
                space = false;
                value.Append(name[i]);
            }
            else if (!space)
            {
                space = true;
                value.Append(' ');
            }
        }
        return value.ToString();
    }

    private class ArgumentEnumerator : IEnumerable<string>
    {
        private readonly List<IEnumerator<string>> _sources = new List<IEnumerator<string>>();

        public ArgumentEnumerator(IEnumerable<string> arguments)
        {
            _sources.Add(arguments.GetEnumerator());
        }

        public void Add(IEnumerable<string> arguments)
        {
            _sources.Add(arguments.GetEnumerator());
        }

        public IEnumerator<string> GetEnumerator()
        {
            do
            {
                IEnumerator<string> c = _sources[_sources.Count - 1];
                if (c.MoveNext())
                    yield return c.Current;
                else
                {
                    c.Dispose();
                    _sources.RemoveAt(_sources.Count - 1);
                }
            } while (_sources.Count > 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
