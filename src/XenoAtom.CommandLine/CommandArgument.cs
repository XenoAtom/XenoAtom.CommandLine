// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Globalization;

namespace XenoAtom.CommandLine;

/// <summary>
/// Defines a positional command argument.
/// </summary>
public abstract class CommandArgument : CommandNode, ICommandNodeDescriptor
{
    private readonly string? _description;

    /// <summary>
    /// Defines the value cardinality for an argument.
    /// </summary>
    public enum ValueCardinality
    {
        /// <summary>
        /// Exactly one value (<c>&lt;name&gt;</c>).
        /// </summary>
        Single,

        /// <summary>
        /// Zero or one value (<c>&lt;name&gt;?</c>).
        /// </summary>
        Optional,

        /// <summary>
        /// Zero or more values (<c>&lt;name&gt;*</c>).
        /// </summary>
        ZeroOrMore,

        /// <summary>
        /// One or more values (<c>&lt;name&gt;+</c>).
        /// </summary>
        OneOrMore
    }

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    /// <param name="prototype">The prototype of this argument. E.g <c>"&lt;file&gt;"</c> or <c>"&lt;file&gt;?"</c>.</param>
    /// <param name="description">The description of this argument.</param>
    /// <param name="hidden">A boolean indicating if this argument is hidden.</param>
    protected CommandArgument(string prototype, string? description, bool hidden = false) : base()
    {
        ArgumentException.ThrowIfNullOrEmpty(prototype);

        if (!TryParsePrototype(prototype, out var normalizedPrototype, out var basePrototype, out var cardinality))
            throw new ArgumentException($"Invalid argument prototype `{prototype}`. Expected `<name>`, `<name>?`, `<name>*` or `<name>+`.", nameof(prototype));

        Prototype = normalizedPrototype;
        BasePrototype = basePrototype;
        Cardinality = cardinality;
        _description = description;
        Optional = cardinality is ValueCardinality.Optional or ValueCardinality.ZeroOrMore;
        IsList = cardinality is ValueCardinality.ZeroOrMore or ValueCardinality.OneOrMore;
        MinValueCount = cardinality is ValueCardinality.Optional or ValueCardinality.ZeroOrMore ? 0 : 1;
        MaxValueCount = IsList ? int.MaxValue : 1;
        Hidden = hidden;
    }

    /// <summary>
    /// Gets the prototype of this argument. E.g <c>"&lt;file&gt;"</c>.
    /// </summary>
    public string Prototype { get; }

    /// <summary>
    /// Gets the base prototype of this argument without any cardinality suffix. E.g <c>"&lt;file&gt;"</c>.
    /// </summary>
    public string BasePrototype { get; }

    /// <summary>
    /// Gets the cardinality for this argument.
    /// </summary>
    public ValueCardinality Cardinality { get; }

    /// <summary>
    /// Gets the description of this argument.
    /// </summary>
    public string? Description => _description;

    /// <summary>
    /// Gets a boolean indicating if this argument is optional.
    /// </summary>
    public bool Optional { get; }

    /// <summary>
    /// Gets a boolean indicating if this argument accepts multiple values.
    /// </summary>
    public bool IsList { get; }

    /// <summary>
    /// Gets a boolean indicating if this argument represents a remainder pass-through (<c>&lt;&gt;</c>) that is forwarded to the command action.
    /// </summary>
    public bool IsRemainder => BasePrototype == "<>";

    /// <summary>
    /// Gets the minimum number of values for this argument.
    /// </summary>
    public int MinValueCount { get; }

    /// <summary>
    /// Gets the maximum number of values for this argument.
    /// </summary>
    public int MaxValueCount { get; }

    /// <summary>
    /// Gets a boolean indicating if this argument is hidden.
    /// </summary>
    public bool Hidden { get; }

    /// <summary>
    /// Invoke this argument after the parsing is complete.
    /// </summary>
    /// <param name="c">The parsing context.</param>
    public void Invoke(CommandArgumentContext c)
    {
        OnParseComplete(c);
        c.Argument = null;
        c.ArgumentValue = null;
        c.ArgumentIndex = -1;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Prototype;
    }

    /// <summary>
    /// Gets the display text for this argument for usage/help.
    /// </summary>
    public string GetDisplayName()
    {
        if (IsRemainder)
        {
            return "[args]...";
        }

        return Cardinality switch
        {
            ValueCardinality.Optional => $"[{BasePrototype}]",
            ValueCardinality.ZeroOrMore => $"{BasePrototype}*",
            ValueCardinality.OneOrMore => $"{BasePrototype}+",
            _ => BasePrototype
        };
    }

    /// <summary>
    /// Called when the parsing is complete.
    /// </summary>
    /// <param name="c">The parsing context.</param>
    protected abstract void OnParseComplete(CommandArgumentContext c);

    /// <summary>
    /// Parses a value for this argument.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <param name="value">A string representation of the value.</param>
    /// <param name="c">The parsing context.</param>
    /// <returns>The parsed value</returns>
    /// <exception cref="CommandArgumentException">If an exception occurs while parsing.</exception>
    protected static T Parse<T>(string? value, CommandArgumentContext c) where T : ISpanParsable<T>
    {
        if (typeof(T) == typeof(string))
            return (T)(object)(value ?? string.Empty);

        T result = default!;
        try
        {
            if (value != null)
            {
                result = T.Parse(value, CultureInfo.InvariantCulture);
            }
        }
        catch (Exception e)
        {
            var name = c.Argument?.Prototype ?? "VALUE";
            var args = new object[] { name };
            throw new CommandArgumentException(string.Format(c.Command.Config.Localizer($"{e.Message} for argument `{{0}}`"), args), name, e);
        }

        return result;
    }

    internal static bool TryParsePrototype(string prototype, out string normalizedPrototype, out string basePrototype, out ValueCardinality cardinality)
    {
        normalizedPrototype = prototype;
        basePrototype = prototype;
        cardinality = ValueCardinality.Single;

        if (prototype == "<>")
        {
            normalizedPrototype = "<>";
            basePrototype = "<>";
            cardinality = ValueCardinality.ZeroOrMore;
            return true;
        }

        if (prototype.Length < 3 || prototype[0] != '<')
            return false;

        var suffix = prototype[^1];
        if (suffix is '?' or '*' or '+')
        {
            prototype = prototype.Substring(0, prototype.Length - 1);
            cardinality = suffix switch
            {
                '?' => ValueCardinality.Optional,
                '*' => ValueCardinality.ZeroOrMore,
                '+' => ValueCardinality.OneOrMore,
                _ => ValueCardinality.Single
            };
        }

        if (prototype.Length < 3 || prototype[0] != '<' || prototype[^1] != '>')
            return false;

        basePrototype = prototype;
        normalizedPrototype = cardinality switch
        {
            ValueCardinality.Optional => basePrototype + "?",
            ValueCardinality.ZeroOrMore => basePrototype + "*",
            ValueCardinality.OneOrMore => basePrototype + "+",
            _ => basePrototype
        };
        return true;
    }

    internal static bool IsArgumentPrototype(string prototypeText)
    {
        var prototype = prototypeText.AsSpan();
        if (prototype.SequenceEqual("<>".AsSpan()))
            return true;

        if (prototype.Length < 3 || prototype[0] != '<')
            return false;

        var suffix = prototype[^1];
        if (suffix is '?' or '*' or '+')
        {
            prototype = prototype[..^1];
        }

        if (prototype.Length < 3 || prototype[0] != '<' || prototype[^1] != '>')
            return false;

        return true;
    }
}
