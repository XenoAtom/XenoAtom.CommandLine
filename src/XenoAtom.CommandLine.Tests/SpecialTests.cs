using System.Collections;
using System.Runtime.CompilerServices;

namespace XenoAtom.CommandLine.Tests;

[TestClass]
public class SpecialTests
{
    [TestMethod]
    public void TestVersionOptionGetDefaultVersion()
    {
        Assert.AreNotEqual("0.0.0", VersionOption.GetDefaultVersion());
    }

    [TestMethod]
    public void TestCommandAddedMultipleTimes()
    {
        var commandApp = new CommandApp();
        var command = new Command("command");
        commandApp.Add(command);
        Assert.ThrowsException<InvalidOperationException>(() => commandApp.Add(command));

        var enumerator = ((IEnumerable)commandApp).GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(command, enumerator.Current);
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public async Task TestKeyValueTyped()
    {
        var list = new List<(string, int)>();
        var commandApp = new CommandApp()
        {
            {"D=", "{0:NAME} and mandatory {1:VALUE}", (string k, int v) => { list.Add((k, v)); }},
        };
        await commandApp.RunAsync(new[] { "-DHELLO=1", "-DTEST=2" });
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual("HELLO", list[0].Item1);
        Assert.AreEqual(1, list[0].Item2);
        Assert.AreEqual("TEST", list[1].Item1);
        Assert.AreEqual(2, list[1].Item2);
    }

    [TestMethod]
    public async Task TestAction()
    {
        var list = new List<string>();
        var commandApp = new CommandApp();
        commandApp.Add(args =>
        {
            list.AddRange(args);
            return new ValueTask<int>(0);
        });
        await commandApp.RunAsync([ "a", "b", "c" ]);
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual("a", list[0]);
        Assert.AreEqual("b", list[1]);
        Assert.AreEqual("c", list[2]);
    }

    [TestMethod]
    public void TestArgumentSource()
    {
        var values = ArgumentSource.GetArguments(new StringReader("""
                                                     "a" "b" "c"
                                                     """)).ToArray();
        Assert.AreEqual(3, values.Length);
        Assert.AreEqual("a", values[0]);
        Assert.AreEqual("b", values[1]);
        Assert.AreEqual("c", values[2]);
    }

    [TestMethod]
    public void TestEnumWrapper()
    {
        Assert.IsFalse(EnumWrapper<SpecialEnum>.TryParse("Hello", null, out _));
        Assert.IsFalse(EnumWrapper<SpecialEnum>.TryParse("Hello".AsSpan(), null, out _));
        Assert.AreEqual(SpecialEnum.Value1, (SpecialEnum)EnumWrapper<SpecialEnum>.Parse("Value1", null));
    }

    [TestMethod]
    public async Task TestCustomOption()
    {
        var customOption = new CustomOption("test");
        var commandApp = new CommandApp()
        {
            customOption
        };
        Assert.AreEqual("test", customOption.Prototype);
        var names = customOption.GetNames();
        Assert.AreEqual(1, names.Length);
        Assert.AreEqual("test", names[0]);
        var seps = customOption.GetValueSeparators();
        Assert.AreEqual(0, seps.Length);
        Assert.AreEqual("test", customOption.ToString());
        
        await commandApp.RunAsync(new[] { "--test" });
    }

    private enum SpecialEnum
    {
        Value1,
        Value2,
        Value3,
    }

    private class CustomOption(string prototype) : Option(prototype, null)
    {
        protected override void OnParseComplete(OptionContext c)
        {
            Assert.IsFalse(c.OptionValues.IsReadOnly);
            Assert.IsFalse(c.OptionValues.Contains("HELLO"));
            c.OptionValues.Remove("HELLO");
            Assert.AreEqual(-1, c.OptionValues.IndexOf("HELLO"));
            Assert.IsTrue(c.OptionValues.Contains("test"));
            Assert.AreEqual(1, c.OptionValues.Count);
            c.OptionValues.Remove("Nothing");

            c.OptionValues.Insert(0, "Hello");
            var array = c.OptionValues.ToArray();
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual("Hello", array[0]);

            var list = c.OptionValues.ToList();
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("Hello", list[0]);


            var array2 = new string[2];
            c.OptionValues.CopyTo(array2, 0);
            Assert.AreEqual("Hello", array2[0]);
            Assert.AreEqual("test", array2[1]);

            Assert.AreEqual("Hello, test", c.OptionValues.ToString());

            var enumerator = ((IEnumerable)c.OptionValues).GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("Hello", enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("test", enumerator.Current);

            c.OptionValues[0] = "Test";

            c.OptionValues.RemoveAt(0);
            Assert.AreEqual(1, c.OptionValues.Count);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => c.OptionValues[1]);
        }
    }
}
