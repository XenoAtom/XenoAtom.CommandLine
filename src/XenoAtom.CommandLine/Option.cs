// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace XenoAtom.CommandLine;

/// <summary>
/// Defines an option.
/// </summary>
public abstract class Option : CommandNode, ICommandNodeDescriptor
{
    private static readonly char[] NameTerminator = ['=', ':'];
    private readonly string[]? _separators;

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    /// <param name="prototype">The prototype of this option. E.g "t|test".</param>
    /// <param name="description">The description of this option</param>
    protected Option(string prototype, string? description)
        : this(prototype, description, 1, false)
    {
    }

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    /// <param name="prototype">The prototype of this option. E.g "t|test".</param>
    /// <param name="description">The description of this option</param>
    /// <param name="maxValueCount">The maximum number of accepted values</param>
    protected Option(string prototype, string? description, int maxValueCount)
        : this(prototype, description, maxValueCount, false)
    {
    }

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    /// <param name="prototype">The prototype of this option. E.g "t|test".</param>
    /// <param name="description">The description of this option</param>
    /// <param name="maxValueCount">The maximum number of accepted values</param>
    /// <param name="hidden">A boolean indicating if this option is hidden</param>
    protected Option(string prototype, string? description, int maxValueCount, bool hidden)
    {
        ArgumentException.ThrowIfNullOrEmpty(prototype);
        if (maxValueCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maxValueCount));

        Prototype = prototype;
        Description = description;
        MaxValueCount = maxValueCount;
        Names = prototype.Split('|');

        OptionValueType = ParsePrototype(out _separators);
        Hidden = hidden;

        if (MaxValueCount == 0 && OptionValueType != OptionValueType.None)
            throw new ArgumentException(
                "Cannot provide maxValueCount of 0 for OptionValueType.Required or " +
                "OptionValueType.Optional.",
                nameof(maxValueCount));
        if (OptionValueType == OptionValueType.None && maxValueCount > 1)
            throw new ArgumentException($"Cannot provide maxValueCount of {maxValueCount} for OptionValueType.None.", nameof(maxValueCount));
        if (Array.IndexOf(Names, "<>") >= 0 &&
            ((Names.Length == 1 && OptionValueType != OptionValueType.None) ||
             (Names.Length > 1 && MaxValueCount > 1)))
            throw new ArgumentException("The default option handler '<>' cannot require values.", nameof(prototype));
    }

    /// <summary>
    /// Gets the prototype of this option. E.g "v|version".
    /// </summary>
    public string Prototype { get; }

    /// <summary>
    /// Gets the description of this option.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the type of this option (Required, Optional, None).
    /// </summary>
    public OptionValueType OptionValueType { get; }

    /// <summary>
    /// Gets the maximum number of accepted values.
    /// </summary>
    public int MaxValueCount { get; }

    /// <summary>
    /// Gets a boolean indicating if this option is hidden.
    /// </summary>
    public bool Hidden { get; }

    internal string[] Names { get; }

    internal string[]? ValueSeparators => _separators;

    /// <summary>
    /// Gets the names of this option deduced from the prototype. E.g "v", "version".
    /// </summary>
    public string[] GetNames()
    {
        return (string[])Names.Clone();
    }

    /// <summary>
    /// Gets the separators for this option. E.g ":", "=".
    /// </summary>
    public string[] GetValueSeparators()
    {
        return _separators == null ? Array.Empty<string>() : (string[])_separators.Clone();
    }

    /// <summary>
    /// Invoke this option after the parsing is complete.
    /// </summary>
    /// <param name="c">The parsing context.</param>
    public void Invoke(OptionContext c)
    {
        OnParseComplete(c);
        c.OptionName = null;
        c.Option = null;
        c.OptionValues.Clear();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Prototype;
    }

    /// <summary>
    /// Called when the parsing is complete.
    /// </summary>
    /// <param name="c">The parsing context.</param>
    protected abstract void OnParseComplete(OptionContext c);

    /// <summary>
    /// Parses a value for this option.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <param name="value">A string representation of the value.</param>
    /// <param name="c">The parsing context.</param>
    /// <returns>The parsed value</returns>
    /// <exception cref="OptionException">If an exception occured while parsing.</exception>
    protected static T Parse<T>(string? value, OptionContext c) where T : ISpanParsable<T>
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
            var args = new object[] { c.OptionName! };
            throw new OptionException(string.Format(c.Command.Config.Localizer($"{e.Message} for option `{{0}}`"), args), c.OptionName!, e);
        }

        return result;
    }
    
    private OptionValueType ParsePrototype(out string[]? separators)
    {
        separators = null;
        char type = '\0';
        var seps = new List<string>();
        for (int i = 0; i < Names.Length; ++i)
        {
            string name = Names[i];
            if (name.Length == 0)
                throw new ArgumentException("Empty option names are not supported.", "prototype");

            int end = name.IndexOfAny(NameTerminator);
            if (end == -1)
                continue;
            Names[i] = name.Substring(0, end);
            if (type == '\0' || type == name[end])
                type = name[end];
            else
                throw new ArgumentException($"Conflicting option types: '{type}' vs. '{name[end]}'.", "prototype");
            AddSeparators(name, end, seps);
        }

        if (type == '\0')
            return OptionValueType.None;

        if (MaxValueCount <= 1 && seps.Count != 0)
            throw new ArgumentException($"Cannot provide key/value separators for Options taking {MaxValueCount} value(s).", "prototype");

        if (MaxValueCount > 1)
        {
            if (seps.Count == 0)
                separators = new string[] { ":", "=" };
            else if (seps.Count == 1 && seps[0].Length == 0)
                separators = null;
            else
                separators = seps.ToArray();
        }

        return type == '=' ? OptionValueType.Required : OptionValueType.Optional;
    }

    private static void AddSeparators(string name, int end, ICollection<string> seps)
    {
        int start = -1;
        for (int i = end + 1; i < name.Length; ++i)
        {
            switch (name[i])
            {
                case '{':
                    if (start != -1)
                        throw new ArgumentException($"Ill-formed name/value separator found in \"{name}\".", "prototype");
                    start = i + 1;
                    break;

                case '}':
                    if (start == -1)
                        throw new ArgumentException($"Ill-formed name/value separator found in \"{name}\".", "prototype");
                    seps.Add(name.Substring(start, i - start));
                    start = -1;
                    break;
                default:
                    if (start == -1)
                        seps.Add(name[i].ToString());
                    break;
            }
        }
        if (start != -1)
            throw new ArgumentException($"Ill-formed name/value separator found in \"{name}\".", "prototype");
    }
}