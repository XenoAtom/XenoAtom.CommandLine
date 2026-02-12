// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Text;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal static class PrototypeFormatter
{
    public static string FormatOptionPrototype(Option option)
    {
        ArgumentNullException.ThrowIfNull(option);

        var names = option.GetNames();
        if (names.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        if (names[0].Length == 1)
        {
            builder.Append("  -");
            builder.Append(names[0]);
        }
        else
        {
            builder.Append("      --");
            builder.Append(names[0]);
        }

        for (var index = 1; index < names.Length; index++)
        {
            builder.Append(", ");
            builder.Append(names[index].Length == 1 ? "-" : "--");
            builder.Append(names[index]);
        }

        if (option.OptionValueType is OptionValueType.Optional or OptionValueType.Required)
        {
            if (option.OptionValueType == OptionValueType.Optional)
            {
                builder.Append('[');
            }

            builder.Append('=');
            builder.Append(CommandOutputHelper.GetOptionValueName(option, 0));

            var separators = option.GetValueSeparators();
            var separator = separators.Length > 0 ? separators[0] : " ";
            for (var valueIndex = 1; valueIndex < option.MaxValueCount; valueIndex++)
            {
                builder.Append(separator);
                builder.Append(CommandOutputHelper.GetOptionValueName(option, valueIndex));
            }

            if (option.OptionValueType == OptionValueType.Optional)
            {
                builder.Append(']');
            }
        }

        return builder.ToString();
    }

    public static string FormatArgumentSourcePrototype(ArgumentSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var names = source.GetNames();
        if (names.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("  ");
        builder.Append(names[0]);
        for (var index = 1; index < names.Length; index++)
        {
            builder.Append(", ");
            builder.Append(names[index]);
        }

        return builder.ToString();
    }
}
