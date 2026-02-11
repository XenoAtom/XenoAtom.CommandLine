// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;

namespace XenoAtom.CommandLine;

/// <summary>
/// Provides built-in validators for options and arguments.
/// </summary>
public static class Validate
{
    /// <summary>
    /// Validates that a comparable value is within the specified inclusive range.
    /// </summary>
    public static OptionValidator<T> Range<T>(T min, T max) where T : IComparable<T>
    {
        return value =>
            value.CompareTo(min) < 0 || value.CompareTo(max) > 0
                ? $"The value must be between {min} and {max}."
                : null;
    }

    /// <summary>
    /// Validates that a numeric value is greater than zero.
    /// </summary>
    public static OptionValidator<T> Positive<T>() where T : INumber<T>
    {
        return value => value > T.Zero ? null : "The value must be positive.";
    }

    /// <summary>
    /// Validates that a numeric value is greater than or equal to zero.
    /// </summary>
    public static OptionValidator<T> NonNegative<T>() where T : INumber<T>
    {
        return value => value >= T.Zero ? null : "The value must be zero or positive.";
    }

    /// <summary>
    /// Validates that a string is non-empty.
    /// </summary>
    public static OptionValidator<string> NonEmpty()
    {
        return value => string.IsNullOrEmpty(value) ? "The value must not be empty." : null;
    }

    /// <summary>
    /// Validates that a string matches the specified regular expression pattern.
    /// </summary>
    public static OptionValidator<string> Matches([StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string? errorMessage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        var regex = new Regex(pattern, RegexOptions.CultureInvariant);
        return Matches(regex, errorMessage);
    }

    /// <summary>
    /// Validates that a string matches the specified regular expression.
    /// </summary>
    public static OptionValidator<string> Matches(Regex regex, string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(regex);
        var message = errorMessage ?? string.Format(CultureInfo.InvariantCulture, "The value must match the pattern '{0}'.", regex.ToString());
        return value => regex.IsMatch(value) ? null : message;
    }

    /// <summary>
    /// Validates that a value is one of the provided allowed values.
    /// </summary>
    public static OptionValidator<T> OneOf<T>(params T[] allowedValues) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(allowedValues);
        if (allowedValues.Length == 0)
            throw new ArgumentException("At least one allowed value must be provided.", nameof(allowedValues));

        return value =>
        {
            for (var i = 0; i < allowedValues.Length; i++)
            {
                if (allowedValues[i].Equals(value))
                    return null;
            }

            return $"The value must be one of: {string.Join(", ", allowedValues)}.";
        };
    }

    /// <summary>
    /// Validates that a path refers to an existing file.
    /// </summary>
    public static OptionValidator<string> FileExists()
    {
        return value => File.Exists(value) ? null : $"The file '{value}' does not exist.";
    }

    /// <summary>
    /// Validates that a path refers to an existing directory.
    /// </summary>
    public static OptionValidator<string> DirectoryExists()
    {
        return value => Directory.Exists(value) ? null : $"The directory '{value}' does not exist.";
    }

    /// <summary>
    /// Validates that a path refers to an existing file or directory.
    /// </summary>
    public static OptionValidator<string> PathExists()
    {
        return value => File.Exists(value) || Directory.Exists(value) ? null : $"The path '{value}' does not exist.";
    }

    /// <summary>
    /// Combines multiple validators and returns the first validation error.
    /// </summary>
    public static OptionValidator<T> Chain<T>(params OptionValidator<T>[] validators)
    {
        ArgumentNullException.ThrowIfNull(validators);
        return value =>
        {
            for (var i = 0; i < validators.Length; i++)
            {
                var validator = validators[i];
                if (validator is null)
                    continue;
                var error = validator(value);
                if (error is not null)
                    return error;
            }
            return null;
        };
    }

    /// <summary>
    /// Creates a validator from an inline predicate.
    /// </summary>
    public static OptionValidator<T> That<T>(Func<T, bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return value => predicate(value) ? null : errorMessage;
    }

    /// <summary>
    /// Returns the provided custom validator.
    /// </summary>
    public static OptionValidator<T> Custom<T>(OptionValidator<T> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        return validator;
    }
}

