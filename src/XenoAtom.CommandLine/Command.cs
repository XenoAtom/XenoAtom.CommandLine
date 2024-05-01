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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XenoAtom.CommandLine;

/// <summary>
/// This class represents a command that can be executed. It can contain sub-commands, options and argument sources.
/// </summary>
public partial class Command  : CommandContainer, ICommandNodeDescriptor
{
    private readonly Dictionary<string, Command> _subCommands = new();
    private readonly Dictionary<string, Option> _options = new();
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
            }
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
        _options.TryGetValue("<>", out var def);
        ArgumentEnumerator ae = new ArgumentEnumerator(arguments);
        foreach (string argument in ae)
        {
            ++c.OptionIndex;
            if (argument == "--")
            {
                if (!process)
                {
                    unprocessed.Add(argument);
                }
                process = false;
                continue;
            }

            if (_subCommands.ContainsKey(argument))
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
                    Unprocessed(unprocessed, def, c, argument);
            }
            else
            {
                Unprocessed(unprocessed, def, c, argument);
            }
        }
        if (c.Option != null)
            c.Option.Invoke(c);

        return unprocessed;
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

    private static bool Unprocessed(ICollection<string> extra, Option? def, OptionContext c, string argument)
    {
        if (def == null)
        {
            extra.Add(argument);
            return false;
        }
        c.OptionValues.Add(argument);
        c.Option = def;
        c.Option.Invoke(c);
        return false;
    }

    [GeneratedRegex(@"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$")]
    private static partial Regex ValueOption();

    private bool GetOptionParts(string argument, [NotNullWhen(true)] out string? flag, [NotNullWhen(true)] out string? name, out string? sep, out string? value)
    {
        flag = name = sep = value = null;
        var m = ValueOption().Match(argument);
        if (!m.Success)
        {
            return false;
        }
        flag = m.Groups["flag"].Value;
        name = m.Groups["name"].Value;
        if (m.Groups["sep"].Success && m.Groups["value"].Success)
        {
            sep = m.Groups["sep"].Value;
            value = m.Groups["value"].Value;
        }
        return true;
    }

    private bool ParseOption(string argument, OptionContext c)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (c.Option != null)
        {
            ParseValue(argument, c);
            return true;
        }

        if (!GetOptionParts(argument, out var flag, out var name, out var sep, out var value))
            return false;

        Option? p;
        if (_options.TryGetValue(name, out p) && p.IsActive())
        {
            c.OptionName = flag + name;
            c.Option = p;
            switch (p.OptionValueType)
            {
                case OptionValueType.None:
                    c.OptionValues.Add(name);
                    c.Option.Invoke(c);
                    break;
                case OptionValueType.Optional:
                case OptionValueType.Required:
                    ParseValue(value, c);
                    break;
            }
            return true;
        }

        // no match; is it a bool option?
        if (ParseBool(argument, name, c))
            return true;
        // is it a bundled option?
        if (ParseBundledValue(flag, $"{name}{sep}{value}", c))
            return true;

        return false;
    }

    private void ParseValue(string? option, OptionContext c)
    {
        if (option != null)
            foreach (string o in c.Option!.ValueSeparators != null
                         ? option.Split(c.Option.ValueSeparators, c.Option.MaxValueCount - c.OptionValues.Count, StringSplitOptions.None)
                         : new string[] { option })
            {
                c.OptionValues.Add(o);
            }
        if (c.OptionValues.Count == c.Option!.MaxValueCount ||
            c.Option.OptionValueType == OptionValueType.Optional)
            c.Option.Invoke(c);
        else if (c.OptionValues.Count > c.Option.MaxValueCount)
        {
            throw new OptionException(Config.Localizer(string.Format("Error: Found {0} option values when expecting {1}.", c.OptionValues.Count, c.Option.MaxValueCount)), c.OptionName!);
        }
    }

    private bool ParseBool(string option, string n, OptionContext c)
    {
        Option p;
        string rn;
        if (n.Length >= 1 && (n[^1] == '+' || n[^1] == '-') && _options.ContainsKey((rn = n.Substring(0, n.Length - 1))))
        {
            p = _options[rn];
            if (p.IsActive())
            {
                string? v = n[^1] == '+' ? option : null;
                c.OptionName = option;
                c.Option = p;
                c.OptionValues.Add(v);
                p.Invoke(c);
                return true;
            }
        }
        return false;
    }

    private bool ParseBundledValue(string f, string n, OptionContext c)
    {
        if (f != "-")
            return false;

        for (int i = 0; i < n.Length; ++i)
        {
            string opt = $"{f}{n[i]}";
            string rn = n[i].ToString();
            if (!_options.TryGetValue(rn, out var p) || !p.IsActive())
            {
                if (i == 0)
                    return false;
                throw new OptionException(string.Format(Config.Localizer("Cannot use unregistered option '{0}' in bundle '{1}'."), rn, f + n), string.Empty);
            }

            switch (p.OptionValueType)
            {
                case OptionValueType.None:
                    Invoke(c, opt, n, p);
                    break;
                case OptionValueType.Optional:
                case OptionValueType.Required:
                    {
                        string v = n.Substring(i + 1);
                        c.Option = p;
                        c.OptionName = opt;
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
        runConfig.Error.WriteLine(command.Config.Localizer($"Use `{fullCommandName} --help` for usage."));
    }

    private void WriteCommandException(CommandRunConfig runConfig, CommandException e)
    {
        var fullCommandName = GetFullCommandPath();
        runConfig.Error.WriteLine($"{fullCommandName}: {e.Message}");
        runConfig.Error.WriteLine(Config.Localizer($"Use `{fullCommandName} --help` for usage."));
    }

    private void WriteUnknownOptions(CommandRunConfig runConfig, Command command, List<string> unknownOptions)
    {
        var fullCommandName = GetFullCommandPath();
        foreach (var unknownOption in unknownOptions)
        {
            runConfig.Error.WriteLine(command.Config.Localizer($"{fullCommandName}: Unknown option: {unknownOption}"));
        }
        runConfig.Error.WriteLine(command.Config.Localizer($"Use `{fullCommandName} --help` for usage."));
    }

    private string GetDefaultUsage(CommandRunConfig runConfig)
    {
        var usage = new StringBuilder();
        var _ = Config.Localizer;
        usage.Append(_("Usage: "));
        usage.Append(GetFullCommandPath());
        if (_options.Count > 0)
        {
            usage.Append(" [");
            usage.Append(OptionsName);
            usage.Append(']');
        }

        if (SubCommands.Count > 0)
        {
            usage.Append(_(" COMMAND"));
        }

        if (_options.TryGetValue("<>", out var def) && def.Description != null)
        {
            usage.Append(' ');
            usage.Append(def.Description);
        }
        return usage.ToString();
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

        int i = GetNextOptionIndex(names, 0);
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

        for (i = GetNextOptionIndex(names, i + 1);
             i < names.Length;
             i = GetNextOptionIndex(names, i + 1))
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

    private static int GetNextOptionIndex(string[] names, int i)
    {
        while (i < names.Length && names[i] == "<>")
        {
            ++i;
        }
        return i;
    }

    private static void Write(TextWriter o, ref int n, string s)
    {
        n += s.Length;
        o.Write(s);
    }
    
    [GeneratedRegex(@"(?<=(?<!\{)\{)[^{}]*(?=\}(?!\}))")]
    private static partial Regex ArgumentNamePattern();
    
    private static string GetArgumentName(int index, int maxIndex, string? description)
    {

        if (description is not null)
        {
            var matches = ArgumentNamePattern().Matches(description); // ignore double braces 
            string? argName = null;
            foreach (Match match in matches)
            {
                var parts = match.Value.Split(':');
                // for maxIndex=1 it can be {foo} or {0:foo}
                if (maxIndex == 1)
                {
                    argName = parts[parts.Length - 1];
                }
                // look for {i:foo} if maxIndex > 1
                if (maxIndex > 1 && parts.Length == 2 &&
                    parts[0] == index.ToString(CultureInfo.InvariantCulture))
                {
                    argName = parts[1];
                }

                if (!string.IsNullOrEmpty(argName))
                {
                    return argName;
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