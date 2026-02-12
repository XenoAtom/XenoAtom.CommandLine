// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XenoAtom.CommandLine;

/// <summary>
/// Extension methods for <see cref="Command"/> and <see cref="CommandContainer"/>.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Sets an action attached to the specified command.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="action">The action to set for this command.</param>
    /// <returns>The command.</returns>
    public static TCommand Add<TCommand>(this TCommand command, Func<CommandRunContext, string[], ValueTask<int>> action)
        where TCommand : Command
    {
        command.Action = action;
        return command;
    }

    /// <summary>
    /// Sets an action attached to the specified command.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="action">The action to set for this command.</param>
    /// <returns>The command.</returns>
    public static TCommand Add<TCommand>(this TCommand command, Func<string[], ValueTask<int>> action)
        where TCommand : Command
    {
        command.Action = (_, enumerable) => action(enumerable);
        return command;
    }

    /// <summary>
    /// Adds a text to the command.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="text">The text to add to this container.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, string text)
        where TCommand: CommandContainer
    {
        ArgumentNullException.ThrowIfNull(text);
        command.Add(new TextNode(text));
        return command;
    }

    /// <summary>
    /// Adds a plain text line to the command.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command container.</param>
    /// <param name="text">The text line to add.</param>
    /// <returns>The command container.</returns>
    public static TCommand AddText<TCommand>(this TCommand command, string text)
        where TCommand : CommandContainer =>
        Add(command, text);

    /// <summary>
    /// Adds a section header line to the command help.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command container.</param>
    /// <param name="header">The section header text.</param>
    /// <returns>The command container.</returns>
    public static TCommand AddSection<TCommand>(this TCommand command, string header)
        where TCommand : CommandContainer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        var normalizedHeader = header.EndsWith(':') ? header : $"{header}:";
        return Add(command, normalizedHeader);
    }

    /// <summary>
    /// Adds a remainder positional argument (<c>&lt;&gt;</c>) to this command container.
    /// All remaining arguments are forwarded to the command action callback.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command container.</param>
    /// <param name="description">The help description for this remainder argument.</param>
    /// <returns>The command container.</returns>
    public static TCommand AddRemainder<TCommand>(this TCommand command, string? description = null)
        where TCommand : CommandContainer
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Add(new RemainderArgument(description));
        return command;
    }

    /// <summary>
    /// Adds the remainder positional argument (<c>&lt;&gt;</c>) to this command container.
    /// All remaining arguments are passed unprocessed to the command action.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command container.</param>
    /// <param name="prototype">Must be <c>"&lt;&gt;"</c>.</param>
    /// <param name="description">The help description for this remainder argument.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, string prototype, string? description)
        where TCommand : CommandContainer
    {
        ArgumentException.ThrowIfNullOrEmpty(prototype);

        if (!string.Equals(prototype, "<>", StringComparison.Ordinal))
            throw new ArgumentException("This overload can only be used with the remainder argument '<>'.", nameof(prototype));

        return AddRemainder(command, description);
    }

    /// <summary>
    /// Adds an option to this command container.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="option">The option to add to this container.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, Option option)
        where TCommand : CommandContainer
    {
        ArgumentNullException.ThrowIfNull(option);
        command.Add(option);
        return command;
    }

    /// <summary>
    /// Adds an option to this command container.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="action">The associated action</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, string prototype, Action<string?> action)
        where TCommand : CommandContainer
    {
        return Add(command, prototype, null, action);
    }

    /// <summary>
    /// Adds an option to this command container.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, string prototype, string? description, Action<string?> action)
        where TCommand : CommandContainer
    {
        return Add(command, prototype, description, action, false);
    }

    /// <summary>
    /// Adds an option to this command container.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action</param>
    /// <param name="hidden">A boolean indicating if this option is hidden from the help.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, string prototype, string? description, Action<string?> action, bool hidden)
        where TCommand : CommandContainer
    {
        ArgumentNullException.ThrowIfNull(action);
        if (string.Equals(prototype, "<>", StringComparison.Ordinal))
            throw new ArgumentException("The remainder argument '<>' cannot be bound to an action. Add it with { \"<>\", \"description\" } and read it from the command action arguments.", nameof(prototype));

        if (CommandArgument.IsArgumentPrototype(prototype))
        {
            command.Add(new ActionArgument(prototype, description, action, hidden));
        }
        else
        {
            Option p = new ActionOption(prototype, description, 1, delegate (OptionValueCollection v) { action(v[0]); }, hidden);
            command.Add(p);
        }
        return command;
    }

    /// <summary>
    /// Adds an option to this command container with an environment variable fallback.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the option to.</param>
    /// <param name="prototype">The prototype of the option. E.g. "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action.</param>
    /// <param name="envVar">The environment variable used as a fallback value.</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <param name="hidden">A boolean indicating if this option is hidden from help.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(
        this TCommand command,
        string prototype,
        string? description,
        Action<string?> action,
        string envVar,
        char? envVarDelimiter = null,
        bool hidden = false)
        where TCommand : CommandContainer
    {
        ArgumentNullException.ThrowIfNull(action);
        if (CommandArgument.IsArgumentPrototype(prototype))
            throw new ArgumentException("Environment variable fallback is only supported for options, not positional arguments.", nameof(prototype));

        ArgumentException.ThrowIfNullOrWhiteSpace(envVar);
        var option = new ActionOption(prototype, description, 1, delegate (OptionValueCollection v) { action(v[0]); }, hidden)
        {
            EnvironmentVariable = envVar,
            EnvironmentVariableDelimiter = envVarDelimiter
        };
        command.Add(option);
        return command;
    }

    /// <summary>
    /// Adds an option or argument with validation.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the option or argument to.</param>
    /// <param name="prototype">The prototype of the option or argument.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The associated action.</param>
    /// <param name="validate">The optional validator.</param>
    /// <param name="envVar">The optional environment variable fallback (options only).</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <param name="hidden">A boolean indicating if this option or argument is hidden from help.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(
        this TCommand command,
        string prototype,
        string? description,
        Action<string?> action,
        OptionValidator<string>? validate,
        string? envVar = null,
        char? envVarDelimiter = null,
        bool hidden = false)
        where TCommand : CommandContainer
    {
        ArgumentNullException.ThrowIfNull(action);
        if (string.Equals(prototype, "<>", StringComparison.Ordinal))
            throw new ArgumentException("The remainder argument '<>' cannot be bound to an action. Add it with { \"<>\", \"description\" } and read it from the command action arguments.", nameof(prototype));

        if (CommandArgument.IsArgumentPrototype(prototype))
        {
            if (!string.IsNullOrWhiteSpace(envVar))
                throw new ArgumentException("Environment variable fallback is only supported for options, not positional arguments.", nameof(envVar));

            command.Add(new ActionArgument(prototype, description, action, hidden, validate));
            return command;
        }

        var normalizedEnvVar = string.IsNullOrWhiteSpace(envVar) ? null : envVar;
        var option = new ActionOption(prototype, description, 1, delegate (OptionValueCollection v) { action(v[0]); }, hidden, validate)
        {
            EnvironmentVariable = normalizedEnvVar,
            EnvironmentVariableDelimiter = normalizedEnvVar is null ? null : envVarDelimiter
        };
        command.Add(option);
        return command;
    }

    /// <summary>
    /// Adds to this command container an option which expect a pair of string value.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="action">The associated action</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, string prototype, Action<string, string?> action)
        where TCommand : CommandContainer =>
        Add(command, prototype, null, action);

    /// <summary>
    /// Adds to this command container an option which expect a pair of string value.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, string prototype, string? description, Action<string, string?> action)
        where TCommand : CommandContainer
    {
        return Add(command, prototype, description, action, false);
    }

    /// <summary>
    /// Adds to this command container an option which expect a pair of string value.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action</param>
    /// <param name="hidden">A boolean indicating if this option is hidden from the help.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, string prototype, string? description, Action<string, string?> action, bool hidden)
        where TCommand : CommandContainer
    {
        ArgumentNullException.ThrowIfNull(action);

        Option p = new ActionOption(prototype, description, 2,
            delegate (OptionValueCollection v) { action(v[0]!, v[1]); }, hidden);
        command.Add(p);
        return command;
    }

    /// <summary>
    /// Adds to this command container an option which expect a specified type for its value.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value of the option.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="action">The associated action</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(this TCommand command, string prototype, Action<T> action)
        where TCommand : CommandContainer
        where T : ISpanParsable<T> =>
        Add(command, prototype, null, action);

    /// <summary>
    /// Adds to this command container an option which expect a specified type for its value.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value of the option.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(this TCommand command, string prototype, string? description, Action<T> action)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedAction(command, prototype, description, action, validate: null, envVar: null, envVarDelimiter: null, hidden: false);
    }

    /// <summary>
    /// Adds to this command container an option which expects a specified type for its value.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value of the option.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action</param>
    /// <param name="hidden">A boolean indicating if this option or argument is hidden from help.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(this TCommand command, string prototype, string? description, Action<T> action, bool hidden)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedAction(command, prototype, description, action, validate: null, envVar: null, envVarDelimiter: null, hidden);
    }

    /// <summary>
    /// Adds an option which expects a specified type for its value with an environment variable fallback.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value type of the option.</typeparam>
    /// <param name="command">The command to add the option to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action.</param>
    /// <param name="envVar">The environment variable used as a fallback value.</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(
        this TCommand command,
        string prototype,
        string? description,
        Action<T> action,
        string envVar,
        char? envVarDelimiter = null)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedAction(command, prototype, description, action, validate: null, envVar, envVarDelimiter, hidden: false);
    }

    /// <summary>
    /// Adds an option which expects a specified type for its value with an environment variable fallback.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value type of the option.</typeparam>
    /// <param name="command">The command to add the option to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action.</param>
    /// <param name="envVar">The environment variable used as a fallback value.</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <param name="hidden">A boolean indicating if this option is hidden from help.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(
        this TCommand command,
        string prototype,
        string? description,
        Action<T> action,
        string envVar,
        char? envVarDelimiter,
        bool hidden)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedAction(command, prototype, description, action, validate: null, envVar, envVarDelimiter, hidden);
    }

    /// <summary>
    /// Adds an option or argument with validation for typed values.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="command">The command to add the node to.</param>
    /// <param name="prototype">The prototype of the option or argument.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The associated action.</param>
    /// <param name="validate">The optional validator.</param>
    /// <param name="envVar">The optional environment variable fallback (options only).</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(
        this TCommand command,
        string prototype,
        string? description,
        Action<T> action,
        OptionValidator<T>? validate,
        string? envVar = null,
        char? envVarDelimiter = null)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedAction(command, prototype, description, action, validate, envVar, envVarDelimiter, hidden: false);
    }

    /// <summary>
    /// Adds an option or argument with validation for typed values.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="command">The command to add the node to.</param>
    /// <param name="prototype">The prototype of the option or argument.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The associated action.</param>
    /// <param name="validate">The optional validator.</param>
    /// <param name="hidden">A boolean indicating if this option or argument is hidden from help.</param>
    /// <param name="envVar">The optional environment variable fallback (options only).</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(
        this TCommand command,
        string prototype,
        string? description,
        Action<T> action,
        OptionValidator<T>? validate,
        bool hidden,
        string? envVar = null,
        char? envVarDelimiter = null)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedAction(command, prototype, description, action, validate, envVar, envVarDelimiter, hidden);
    }

    /// <summary>
    /// Adds to this command container an option which expects a specified type and will add the value to the specified list.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value of the option.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="list">The associated list to receive the value of this option</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(this TCommand command, string prototype, ICollection<T> list)
        where TCommand : CommandContainer
        where T : ISpanParsable<T> =>
        Add(command, prototype, null, (T v) => list.Add(v));


    /// <summary>
    /// Adds to this command container an option which expects a specified type and will add the value to the specified list.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value of the option.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="list">The associated list to receive the value of this option</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(this TCommand command, string prototype, string? description, ICollection<T> list)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedList(command, prototype, description, list, validate: null, envVar: null, envVarDelimiter: null, hidden: false);
    }

    /// <summary>
    /// Adds to this command container an option which expects a specified type and will add the value to the specified list.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value of the option.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="list">The associated list to receive the value of this option.</param>
    /// <param name="hidden">A boolean indicating if this option or argument is hidden from help.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(this TCommand command, string prototype, string? description, ICollection<T> list, bool hidden)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedList(command, prototype, description, list, validate: null, envVar: null, envVarDelimiter: null, hidden);
    }

    /// <summary>
    /// Adds an option which expects a specified type and appends parsed values to the specified list,
    /// with an environment variable fallback.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value type of the option.</typeparam>
    /// <param name="command">The command to add the option to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="list">The associated list receiving option values.</param>
    /// <param name="envVar">The environment variable used as a fallback value.</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(
        this TCommand command,
        string prototype,
        string? description,
        ICollection<T> list,
        string envVar,
        char? envVarDelimiter = null)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedList(command, prototype, description, list, validate: null, envVar, envVarDelimiter, hidden: false);
    }

    /// <summary>
    /// Adds an option which expects a specified type and appends parsed values to the specified list,
    /// with an environment variable fallback.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value type of the option.</typeparam>
    /// <param name="command">The command to add the option to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="list">The associated list receiving option values.</param>
    /// <param name="envVar">The environment variable used as a fallback value.</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <param name="hidden">A boolean indicating if this option is hidden from help.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(
        this TCommand command,
        string prototype,
        string? description,
        ICollection<T> list,
        string envVar,
        char? envVarDelimiter,
        bool hidden)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedList(command, prototype, description, list, validate: null, envVar, envVarDelimiter, hidden);
    }

    /// <summary>
    /// Adds an option or argument that appends values to a list with optional validation.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="command">The command to add the node to.</param>
    /// <param name="prototype">The prototype of the option or argument.</param>
    /// <param name="description">The help description.</param>
    /// <param name="list">The list that receives parsed values.</param>
    /// <param name="validate">The optional validator.</param>
    /// <param name="envVar">The optional environment variable fallback (options only).</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(
        this TCommand command,
        string prototype,
        string? description,
        ICollection<T> list,
        OptionValidator<T>? validate,
        string? envVar = null,
        char? envVarDelimiter = null)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedList(command, prototype, description, list, validate, envVar, envVarDelimiter, hidden: false);
    }

    /// <summary>
    /// Adds an option or argument that appends values to a list with optional validation.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="command">The command to add the node to.</param>
    /// <param name="prototype">The prototype of the option or argument.</param>
    /// <param name="description">The help description.</param>
    /// <param name="list">The list that receives parsed values.</param>
    /// <param name="validate">The optional validator.</param>
    /// <param name="hidden">A boolean indicating if this option or argument is hidden from help.</param>
    /// <param name="envVar">The optional environment variable fallback (options only).</param>
    /// <param name="envVarDelimiter">Optional delimiter used to split multiple fallback values.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand, T>(
        this TCommand command,
        string prototype,
        string? description,
        ICollection<T> list,
        OptionValidator<T>? validate,
        bool hidden,
        string? envVar = null,
        char? envVarDelimiter = null)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        return AddTypedList(command, prototype, description, list, validate, envVar, envVarDelimiter, hidden);
    }

    /// <summary>
    /// Adds to this command container an option which expect a pair of key/value.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="action">The associated action</param>
    /// <returns>The command container.</returns>
    /// <remarks>
    /// Environment-variable fallback and validation delegates are intentionally not supported for key/value option overloads.
    /// </remarks>
    public static TCommand Add<TCommand, TKey, TValue>(this TCommand command, string prototype, Action<TKey, TValue> action)
        where TCommand : CommandContainer
        where TKey : ISpanParsable<TKey>
        where TValue : ISpanParsable<TValue> =>
        Add(command, prototype, null, action);

    /// <summary>
    /// Adds to this command container an option which expect a pair of key/value.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="prototype">The prototype of the option. E.g "v|version".</param>
    /// <param name="description">The help description for this option.</param>
    /// <param name="action">The associated action</param>
    /// <returns>The command container.</returns>
    /// <remarks>
    /// Environment-variable fallback and validation delegates are intentionally not supported for key/value option overloads.
    /// </remarks>
    public static TCommand Add<TCommand, TKey, TValue>(this TCommand command, string prototype, string? description, Action<TKey, TValue> action)
        where TCommand : CommandContainer
        where TKey : ISpanParsable<TKey>
        where TValue : ISpanParsable<TValue>
    {
        command.Add(new ActionOption<TKey, TValue>(prototype, description, action));
        return command;
    }


    /// <summary>
    /// Adds the specified argument source to this command container.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command to add the action to.</param>
    /// <param name="source">The argument source providing arguments.</param>
    /// <returns>The command container.</returns>
    public static TCommand Add<TCommand>(this TCommand command, ArgumentSource source)
        where TCommand : CommandContainer
    {
        ArgumentNullException.ThrowIfNull(source);
        command.Add(source);
        return command;
    }

    /// <summary>
    /// Declares that the specified options cannot be used together.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command container.</param>
    /// <param name="optionNames">Two or more option names (without prefix).</param>
    /// <returns>The command container.</returns>
    public static TCommand AddMutuallyExclusive<TCommand>(this TCommand command, params string[] optionNames)
        where TCommand : CommandContainer
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Add(new MutuallyExclusiveConstraint(optionNames));
        return command;
    }

    /// <summary>
    /// Declares that when <paramref name="optionName"/> is present, all <paramref name="requiredOptionNames"/> must also be present.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <param name="command">The command container.</param>
    /// <param name="optionName">The option that triggers the requirement (without prefix).</param>
    /// <param name="requiredOptionNames">One or more required option names (without prefix).</param>
    /// <returns>The command container.</returns>
    public static TCommand AddRequires<TCommand>(this TCommand command, string optionName, params string[] requiredOptionNames)
        where TCommand : CommandContainer
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Add(new RequiresConstraint(optionName, requiredOptionNames));
        return command;
    }

    private static TCommand AddTypedAction<TCommand, T>(
        TCommand command,
        string prototype,
        string? description,
        Action<T> action,
        OptionValidator<T>? validate,
        string? envVar,
        char? envVarDelimiter,
        bool hidden)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsurePrototypeIsNotRemainder(prototype, isListBinding: false);

        if (CommandArgument.IsArgumentPrototype(prototype))
        {
            if (!string.IsNullOrWhiteSpace(envVar))
                throw new ArgumentException("Environment variable fallback is only supported for options, not positional arguments.", nameof(envVar));

            command.Add(new ActionArgument<T>(prototype, description, action, hidden, validate));
            return command;
        }

        var normalizedEnvVar = string.IsNullOrWhiteSpace(envVar) ? null : envVar;
        var option = new ActionOption<T>(prototype, description, action, validate, hidden)
        {
            EnvironmentVariable = normalizedEnvVar,
            EnvironmentVariableDelimiter = normalizedEnvVar is null ? null : envVarDelimiter
        };

        command.Add(option);
        return command;
    }

    private static TCommand AddTypedList<TCommand, T>(
        TCommand command,
        string prototype,
        string? description,
        ICollection<T> list,
        OptionValidator<T>? validate,
        string? envVar,
        char? envVarDelimiter,
        bool hidden)
        where TCommand : CommandContainer
        where T : ISpanParsable<T>
    {
        ArgumentNullException.ThrowIfNull(list);
        EnsurePrototypeIsNotRemainder(prototype, isListBinding: true);

        if (CommandArgument.IsArgumentPrototype(prototype))
        {
            if (!string.IsNullOrWhiteSpace(envVar))
                throw new ArgumentException("Environment variable fallback is only supported for options, not positional arguments.", nameof(envVar));

            command.Add(new ActionArgument<T>(prototype, description, list.Add, hidden, validate));
            return command;
        }

        var normalizedEnvVar = string.IsNullOrWhiteSpace(envVar) ? null : envVar;
        var option = new ActionOption<T>(prototype, description, list.Add, validate, hidden)
        {
            EnvironmentVariable = normalizedEnvVar,
            EnvironmentVariableDelimiter = normalizedEnvVar is null ? null : envVarDelimiter
        };

        command.Add(option);
        return command;
    }

    private static void EnsurePrototypeIsNotRemainder(string prototype, bool isListBinding)
    {
        if (string.Equals(prototype, "<>", StringComparison.Ordinal))
        {
            var bindingKind = isListBinding ? "list" : "action";
            throw new ArgumentException($"The remainder argument '<>' cannot be bound to a {bindingKind}. Add it with {{ \"<>\", \"description\" }} and read it from the command action arguments.", nameof(prototype));
        }
    }

    private sealed class ActionOption<T> : Option
        where T : ISpanParsable<T>
    {
        private readonly Action<T> _action;
        private readonly OptionValidator<T>? _validate;

        public ActionOption(string prototype, string? description, Action<T> action, OptionValidator<T>? validate = null, bool hidden = false)
            : base(prototype, description, 1, hidden)
        {
            ArgumentNullException.ThrowIfNull(action);
            _action = action;
            _validate = validate;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            var rawValue = c.OptionValues[0];
            var parsedValue = Parse<T>(rawValue, c);

            if (_validate is not null && rawValue is not null)
            {
                var validationError = _validate(parsedValue);
                if (validationError is not null)
                {
                    throw CreateOptionValidationException(c, validationError);
                }
            }

            _action(parsedValue);
        }
    }

    private sealed class ActionOption<TKey, TValue> : Option
        where TKey : ISpanParsable<TKey>
        where TValue : ISpanParsable<TValue>
    {
        private readonly Action<TKey, TValue> _action;

        public ActionOption(string prototype, string? description, Action<TKey, TValue> action)
            : base(prototype, description, 2)
        {
            ArgumentNullException.ThrowIfNull(action);
            _action = action;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            _action(Parse<TKey>(c.OptionValues[0], c), Parse<TValue>(c.OptionValues[1], c));
        }
    }

    private sealed class ActionOption : Option
    {
        private readonly Action<OptionValueCollection> _action;
        private readonly OptionValidator<string>? _validate;

        public ActionOption(string prototype, string? description, int count, Action<OptionValueCollection> action, bool hidden, OptionValidator<string>? validate = null)
            : base(prototype, description, count, hidden)
        {
            ArgumentNullException.ThrowIfNull(action);
            this._action = action;
            _validate = validate;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            if (_validate is not null && c.OptionValues.Count > 0 && c.OptionValues[0] is not null)
            {
                var validationError = _validate(c.OptionValues[0]!);
                if (validationError is not null)
                {
                    throw CreateOptionValidationException(c, validationError);
                }
            }

            _action(c.OptionValues);
        }
    }

    private sealed class TextNode(string description) : CommandNode, ICommandNodeDescriptor
    {
        public string Description { get; } = description;
    }

    private sealed class ActionArgument : CommandArgument
    {
        private readonly Action<string?> _action;
        private readonly OptionValidator<string>? _validate;

        public ActionArgument(string prototype, string? description, Action<string?> action, bool hidden, OptionValidator<string>? validate = null) : base(prototype, description, hidden)
        {
            ArgumentNullException.ThrowIfNull(action);
            _action = action;
            _validate = validate;
        }

        protected override void OnParseComplete(CommandArgumentContext c)
        {
            if (_validate is not null && c.ArgumentValue is not null)
            {
                var validationError = _validate(c.ArgumentValue);
                if (validationError is not null)
                {
                    throw CreateArgumentValidationException(c, validationError);
                }
            }

            _action(c.ArgumentValue);
        }
    }

    private sealed class ActionArgument<T> : CommandArgument
        where T : ISpanParsable<T>
    {
        private readonly Action<T> _action;
        private readonly OptionValidator<T>? _validate;

        public ActionArgument(string prototype, string? description, Action<T> action, bool hidden = false, OptionValidator<T>? validate = null)
            : base(prototype, description, hidden)
        {
            ArgumentNullException.ThrowIfNull(action);
            _action = action;
            _validate = validate;
        }

        protected override void OnParseComplete(CommandArgumentContext c)
        {
            if (c.ArgumentValue is null)
            {
                return;
            }

            var parsedValue = Parse<T>(c.ArgumentValue, c);
            if (_validate is not null)
            {
                var validationError = _validate(parsedValue);
                if (validationError is not null)
                {
                    throw CreateArgumentValidationException(c, validationError);
                }
            }

            _action(parsedValue);
        }
    }

    private sealed class RemainderArgument : CommandArgument
    {
        public RemainderArgument(string? description) : base("<>", description)
        {
        }

        protected override void OnParseComplete(CommandArgumentContext c)
        {
        }
    }

    private static CommandOptionException CreateOptionValidationException(OptionContext context, string validationError)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(validationError);

        var option = context.Option;
        var optionDisplayName = option?.GetDisplayName() ?? context.OptionName ?? "option";
        var fromEnvironment = context.DiagnosticSource == CommandDiagnosticSource.EnvironmentVariable &&
                              !string.IsNullOrWhiteSpace(context.DiagnosticSourceName);

        var message = fromEnvironment
            ? context.Command.Config.Localizer($"Invalid value for option `{optionDisplayName}` (from environment variable `{context.DiagnosticSourceName}`): {validationError}")
            : context.Command.Config.Localizer($"Invalid value for option `{optionDisplayName}`: {validationError}");

        CommandTokenSpan? span = null;
        if (context.OptionIndex >= 0)
        {
            var raw = context.OptionValues.Count > 0 ? context.OptionValues[0] : null;
            span = new CommandTokenSpan(context.OptionIndex, 0, Math.Max(1, raw?.Length ?? 0));
        }

        return new CommandOptionException(message, optionDisplayName)
        {
            Diagnostic = new CommandDiagnostic(
                context.DiagnosticSource,
                context.DiagnosticSourceName,
                option,
                context.CommandRunContext.InvocationTokens,
                span)
        };
    }

    private static CommandArgumentException CreateArgumentValidationException(CommandArgumentContext context, string validationError)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(validationError);

        var argumentDisplayName = context.Argument?.GetDisplayName() ?? context.Argument?.Prototype ?? "argument";
        var message = context.Command.Config.Localizer($"Invalid value for argument `{argumentDisplayName}`: {validationError}");
        CommandTokenSpan? span = null;
        if (context.ArgumentIndex >= 0)
        {
            span = new CommandTokenSpan(context.ArgumentIndex, 0, Math.Max(1, context.ArgumentValue?.Length ?? 0));
        }

        return new CommandArgumentException(message, argumentDisplayName)
        {
            Diagnostic = new CommandDiagnostic(
                CommandDiagnosticSource.CommandLine,
                null,
                context.Argument,
                context.CommandRunContext.InvocationTokens,
                span)
        };
    }
}
