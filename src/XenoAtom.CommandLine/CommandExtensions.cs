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

        Option p = new ActionOption(prototype, description, 1, delegate (OptionValueCollection v) { action(v[0]); }, hidden);
        command.Add(p);
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
        return Add(command, new ActionOption<T>(prototype, description, action));
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
        command.Add(new ActionOption<T>(prototype, description, list.Add));
        return command;
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
    
    private sealed class ActionOption<T> : Option
        where T : ISpanParsable<T>
    {
        private readonly Action<T> _action;

        public ActionOption(string prototype, string? description, Action<T> action)
            : base(prototype, description, 1)
        {
            ArgumentNullException.ThrowIfNull(action);
            _action = action;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            _action(Parse<T>(c.OptionValues[0], c));
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

        public ActionOption(string prototype, string? description, int count, Action<OptionValueCollection> action, bool hidden)
            : base(prototype, description, count, hidden)
        {
            ArgumentNullException.ThrowIfNull(action);
            this._action = action;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            _action(c.OptionValues);
        }
    }

    private sealed class TextNode(string description) : CommandNode, ICommandNodeDescriptor
    {
        public string Description { get; } = description;
    }
}