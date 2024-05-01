// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace XenoAtom.CommandLine;

/// <summary>
/// A collection of option values.
/// </summary>
public class OptionValueCollection : IList<string?>
{
    private readonly List<string?> _values = new();
    private readonly OptionContext _optionContext;

    internal OptionValueCollection(OptionContext optionContext)
    {
        _optionContext = optionContext;
    }

    /// <inheritdoc />
    public void Add(string? item) { _values.Add(item); }
    /// <inheritdoc />
    public void Clear() { _values.Clear(); }
    /// <inheritdoc />
    public bool Contains(string? item) { return _values.Contains(item); }
    /// <inheritdoc />
    public void CopyTo(string?[] array, int arrayIndex) { _values.CopyTo(array, arrayIndex); }
    /// <inheritdoc />
    public bool Remove(string? item) { return _values.Remove(item); }
    /// <inheritdoc />
    public int Count { get { return _values.Count; } }
    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator() { return _values.GetEnumerator(); }

    /// <inheritdoc />
    public int IndexOf(string? item) { return _values.IndexOf(item); }
    /// <inheritdoc />
    public void Insert(int index, string? item) { _values.Insert(index, item); }
    /// <inheritdoc />
    public void RemoveAt(int index) { _values.RemoveAt(index); }

    private void AssertValid(int index)
    {
        if (_optionContext.Option == null)
            throw new InvalidOperationException("OptionContext.Option is null.");
        if (index >= _optionContext.Option.MaxValueCount)
            throw new ArgumentOutOfRangeException("index");
        if (_optionContext.Option.OptionValueType == OptionValueType.Required &&
            index >= _values.Count)
            throw new OptionException(string.Format(_optionContext.Command.Config.Localizer("Missing required value for option '{0}'."), _optionContext.OptionName), _optionContext.OptionName!);
    }

    /// <inheritdoc />
    public string? this[int index]
    {
        get
        {
            AssertValid(index);
            return index >= _values.Count ? null: _values[index];
        }
        set
        {
            AssertValid(index);
            _values[index] = value!;
        }
    }

    /// <summary>
    /// Converts the values to a list.
    /// </summary>
    public List<string?> ToList() => [.._values];

    /// <summary>
    /// Converts the values to an array.
    /// </summary>
    public string?[] ToArray() => _values.ToArray();

    /// <inheritdoc />
    public override string ToString() => string.Join(", ", _values.ToArray());
}