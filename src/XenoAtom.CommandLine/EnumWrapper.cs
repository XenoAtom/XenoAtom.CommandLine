// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// A wrapper around an enum that provides parsing.
/// </summary>
/// <typeparam name="TEnum">The type of the enum to be wrapper.</typeparam>
/// <param name="Value">The enum value.</param>
public readonly record struct EnumWrapper<TEnum>(TEnum Value) : ISpanParsable<EnumWrapper<TEnum>> where TEnum : struct, Enum
{
    /// <summary>
    /// Returns the names of the enum items.
    /// </summary>
    public static string Names => string.Join(", ", Enum.GetNames<TEnum>());

    /// <inheritdoc />
    public static EnumWrapper<TEnum> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, out EnumWrapper<TEnum> result) => TryParse(s.AsSpan(), provider, out result);

    /// <inheritdoc />
    public static EnumWrapper<TEnum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => Enum.Parse<TEnum>(s, true);

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out EnumWrapper<TEnum> result)
    {
        var tryParse = Enum.TryParse<TEnum>(s, true, out var value);
        result = value;
        return tryParse;
    }

    /// <summary>
    /// Implicit conversion from <see cref="EnumWrapper{TEnum}"/> to <typeparamref name="TEnum"/>.
    /// </summary>
    /// <param name="wrapper">The wrapped enum.</param>
    public static implicit operator TEnum(EnumWrapper<TEnum> wrapper) => wrapper.Value;

    /// <summary>
    /// Implicit conversion from <typeparamref name="TEnum"/> to <see cref="EnumWrapper{TEnum}"/>.
    /// </summary>
    /// <param name="value">The enum value to be wrapped.</param>
    public static implicit operator EnumWrapper<TEnum>(TEnum value) => new(value);
}