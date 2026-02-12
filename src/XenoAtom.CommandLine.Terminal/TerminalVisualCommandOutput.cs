// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using XenoAtom.CommandLine.Terminal.Internals;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// An <see cref="ICommandOutput"/> that renders help as Terminal.UI visuals.
/// </summary>
public sealed class TerminalVisualCommandOutput : TerminalMarkupCommandOutput
{
    private readonly TerminalVisualOutputOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="TerminalVisualCommandOutput"/>.
    /// </summary>
    /// <param name="options">Optional output options.</param>
    public TerminalVisualCommandOutput(TerminalVisualOutputOptions? options = null)
        : base(options ?? new TerminalVisualOutputOptions())
    {
        _options = options ?? new TerminalVisualOutputOptions();
    }

    /// <inheritdoc />
    public override void WriteHelp(Command command, CommandRunConfig runConfig)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);

        var visual = command.ToHelpVisual(_options);
        WriteVisual(visual);
    }

    /// <inheritdoc />
    public override void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        ArgumentNullException.ThrowIfNull(exception);

        if (!_options.UseErrorGroups)
        {
            base.WriteError(command, runConfig, exception);
            return;
        }

        var sourceContext = GetSourceContext(exception.Diagnostic);
        var title = sourceContext.Length == 0 ? "Error" : $"Error {sourceContext}";

        var content = new VStack();
        content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.DescriptionStyle, exception.Message)));

        if (_options.ShowDiagnosticUnderline)
        {
            AddDiagnosticUnderline(content, command, exception.Diagnostic);
        }

        content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.HintStyle, CommandOutputHelper.GetHelpHint(command))));

        WriteErrorGroup(title, content);
    }

    /// <inheritdoc />
    public override void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenReport report)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        ArgumentNullException.ThrowIfNull(report.UnknownTokens);

        if (!_options.UseErrorGroups)
        {
            base.WriteUnknownTokens(command, runConfig, report);
            return;
        }

        var messagePrefix = report.Kind == UnknownTokenKind.UnknownCommandOrOption ? "Unknown command or option" : "Unknown option";
        var invocationTokens = report.InvocationTokens;

        var content = new VStack();
        for (var index = 0; index < report.UnknownTokens.Count; index++)
        {
            var unknownToken = report.UnknownTokens[index];

            if (index > 0)
            {
                content.Add(new TextBlock(string.Empty));
            }

            content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.ErrorStyle, $"{messagePrefix}: {unknownToken.Token}")));

            if (_options.ShowDiagnosticUnderline && invocationTokens is { Count: > 0 } && unknownToken.TokenSpan is { } tokenSpan)
            {
                var invocation = CommandOutputHelper.RenderInvocation(command, invocationTokens);
                content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.HintStyle, invocation.Text)));

                var underline = CommandOutputHelper.RenderUnderline(invocation, tokenSpan);
                if (!string.IsNullOrWhiteSpace(underline))
                {
                    content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.ErrorStyle, underline)));
                }
            }

            if (!string.IsNullOrWhiteSpace(unknownToken.InactiveMatchMessage))
            {
                content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.HintStyle, unknownToken.InactiveMatchMessage)));
            }

            if (unknownToken.Suggestions.Count > 0)
            {
                content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.HintStyle, $"Did you mean: {string.Join(", ", unknownToken.Suggestions)}")));
            }
        }

        content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.HintStyle, CommandOutputHelper.GetHelpHint(command))));

        WriteErrorGroup("Error", content);
    }

    private void AddDiagnosticUnderline(VStack content, Command command, CommandDiagnostic? diagnostic)
    {
        if (diagnostic is not { Tokens: { Count: > 0 } tokens, TokenSpan: { } tokenSpan })
        {
            return;
        }

        var invocation = CommandOutputHelper.RenderInvocation(command, tokens);
        content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.HintStyle, invocation.Text)));

        var underline = CommandOutputHelper.RenderUnderline(invocation, tokenSpan);
        if (!string.IsNullOrWhiteSpace(underline))
        {
            content.Add(new Markup(MarkupStyleHelper.ApplyStyle(_options.ErrorStyle, underline)));
        }
    }

    private void WriteErrorGroup(string title, VStack content)
    {
        var titleVisual = new Markup(MarkupStyleHelper.ApplyStyle(_options.ErrorStyle, title));
        var group = new Group(titleVisual, content);
        group.Style(_options.ErrorGroupStyle);
        if (_options.ErrorGroupMinWidth > 0)
        {
            group.MinWidth = _options.ErrorGroupMinWidth;
        }

        WriteVisual(group);
    }

    private void WriteVisual(Visual visual)
    {
        if (_options.Theme is not null)
        {
            visual.SetStyle(_options.Theme);
        }

        XenoAtom.Terminal.Terminal.Write(visual);
    }

    private static string GetSourceContext(CommandDiagnostic? diagnostic)
    {
        if (diagnostic is not { } value || value.Source == CommandDiagnosticSource.CommandLine)
        {
            return string.Empty;
        }

        return value.Source switch
        {
            CommandDiagnosticSource.ResponseFile => value.SourceName is null
                ? "(in response file)"
                : $"(in response file '{value.SourceName}')",
            CommandDiagnosticSource.EnvironmentVariable => value.SourceName is null
                ? "(in environment variable)"
                : $"(in environment variable '{value.SourceName}')",
            CommandDiagnosticSource.Other => value.SourceName is null
                ? "(in additional source)"
                : $"(in source '{value.SourceName}')",
            _ => string.Empty,
        };
    }
}
