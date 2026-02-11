using System;
using System.Collections.Generic;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal static class HelpModelBuilder
{
    public static HelpModel Build(Command command, bool preserveNodeOrder)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lines = new List<HelpLine>();
        if (preserveNodeOrder)
        {
            BuildPreserveNodeOrder(command, lines);
        }
        else
        {
            BuildStructured(command, lines);
        }

        if (HasVisibleSubCommands(command))
        {
            lines.Add(new HelpLine(HelpLineKind.FooterHint, CommandOutputHelper.GetHelpHint(command)));
        }

        return new HelpModel(lines);
    }

    private static void BuildPreserveNodeOrder(Command command, List<HelpLine> lines)
    {
        if (!HasActiveUsageNode(command))
        {
            lines.Add(new HelpLine(HelpLineKind.Usage, GetDefaultUsageText(command)));
        }

        foreach (var node in command.Nodes)
        {
            if (!node.IsActive())
            {
                continue;
            }

            switch (node)
            {
                case CommandUsage usage:
                    lines.Add(new HelpLine(HelpLineKind.Usage, GetText(usage.Description)));
                    continue;

                case Option option when !option.Hidden:
                    lines.Add(new HelpLine(
                        HelpLineKind.Row,
                        Row: new HelpRow(
                            HelpRowKind.Option,
                            PrototypeFormatter.FormatOptionPrototype(option),
                            GetOptionDescription(option))));
                    continue;

                case CommandArgument argument when !argument.Hidden:
                    lines.Add(new HelpLine(
                        HelpLineKind.Row,
                        Row: new HelpRow(
                            HelpRowKind.Argument,
                            "  " + argument.GetDisplayName(),
                            GetDescription(argument.Description))));
                    continue;

                case ArgumentSource source:
                    lines.Add(new HelpLine(
                        HelpLineKind.Row,
                        Row: new HelpRow(
                            HelpRowKind.Source,
                            PrototypeFormatter.FormatArgumentSourcePrototype(source),
                            GetDescription(source.Description))));
                    continue;

                case Command subCommand when !subCommand.Hidden:
                    lines.Add(new HelpLine(
                        HelpLineKind.Row,
                        Row: new HelpRow(
                            HelpRowKind.Command,
                            "  " + subCommand.Name,
                            GetDescription(subCommand.Description))));
                    continue;

                case ICommandNodeDescriptor descriptor:
                    AddDescriptorLine(lines, descriptor.Description);
                    continue;
            }
        }
    }

    private static void BuildStructured(Command command, List<HelpLine> lines)
    {
        if (!HasActiveUsageNode(command))
        {
            lines.Add(new HelpLine(HelpLineKind.Usage, GetDefaultUsageText(command)));
        }
        else
        {
            foreach (var node in command.Nodes)
            {
                if (!node.IsActive() || node is not CommandUsage usage)
                {
                    continue;
                }

                lines.Add(new HelpLine(HelpLineKind.Usage, GetText(usage.Description)));
            }
        }

        AddRowSection(
            lines,
            "Options:",
            HelpRowKind.Option,
            BuildOptions(command));

        AddRowSection(
            lines,
            "Arguments:",
            HelpRowKind.Argument,
            BuildArguments(command));

        AddRowSection(
            lines,
            "Available commands:",
            HelpRowKind.Command,
            BuildCommands(command));
    }

    private static IEnumerable<HelpRow> BuildOptions(Command command)
    {
        foreach (var option in CommandOutputHelper.GetVisibleOptions(command))
        {
            yield return new HelpRow(HelpRowKind.Option, PrototypeFormatter.FormatOptionPrototype(option), GetOptionDescription(option));
        }
    }

    private static IEnumerable<HelpRow> BuildArguments(Command command)
    {
        foreach (var argument in CommandOutputHelper.GetVisibleArguments(command))
        {
            yield return new HelpRow(HelpRowKind.Argument, "  " + argument.GetDisplayName(), GetDescription(argument.Description));
        }
    }

    private static IEnumerable<HelpRow> BuildCommands(Command command)
    {
        foreach (var subCommand in CommandOutputHelper.GetVisibleSubCommands(command))
        {
            yield return new HelpRow(HelpRowKind.Command, "  " + subCommand.Name, GetDescription(subCommand.Description));
        }
    }

    private static void AddRowSection(List<HelpLine> lines, string sectionTitle, HelpRowKind kind, IEnumerable<HelpRow> rows)
    {
        var hasAny = false;
        foreach (var row in rows)
        {
            if (!hasAny)
            {
                if (lines.Count > 0)
                {
                    lines.Add(new HelpLine(HelpLineKind.Blank));
                }

                lines.Add(new HelpLine(HelpLineKind.Text, sectionTitle, IsSectionHeader: true));
                hasAny = true;
            }

            lines.Add(new HelpLine(HelpLineKind.Row, Row: row with { Kind = kind }));
        }
    }

    private static void AddDescriptorLine(List<HelpLine> lines, string? text)
    {
        if (text is null)
        {
            return;
        }

        if (text.Length == 0)
        {
            lines.Add(new HelpLine(HelpLineKind.Blank));
            return;
        }

        lines.Add(new HelpLine(HelpLineKind.Text, GetText(text), IsSectionHeader: true));
    }

    private static bool HasActiveUsageNode(Command command)
    {
        foreach (var node in command.Nodes)
        {
            if (!node.IsActive())
            {
                continue;
            }

            if (node is CommandUsage)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasVisibleSubCommands(Command command)
    {
        foreach (var subCommand in CommandOutputHelper.GetVisibleSubCommands(command))
        {
            _ = subCommand;
            return true;
        }

        return false;
    }

    private static string GetDefaultUsageText(Command command)
    {
        var path = CommandOutputHelper.GetFullCommandPath(command);
        var syntax = CommandOutputHelper.GetDefaultUsageSyntax(command);
        return syntax.Length == 0 ? $"Usage: {path}" : $"Usage: {path} {syntax}";
    }

    private static string GetOptionDescription(Option option)
    {
        ArgumentNullException.ThrowIfNull(option);
        var description = GetDescription(option.Description);

        if (!string.IsNullOrWhiteSpace(option.EnvironmentVariable))
        {
            description = description.Length == 0
                ? $"[env: {option.EnvironmentVariable}]"
                : $"{description} [env: {option.EnvironmentVariable}]";
        }

        return description;
    }

    private static string GetDescription(string? description) => CommandOutputHelper.GetDescriptionText(description);

    private static string GetText(string? text) => text ?? string.Empty;
}
