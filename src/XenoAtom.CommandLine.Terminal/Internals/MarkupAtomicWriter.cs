using System;
using XenoAtom.Ansi;
using XenoAtom.Terminal;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal static class MarkupAtomicWriter
{
    public static void Write(Action<Writer> writeAction)
    {
        ArgumentNullException.ThrowIfNull(writeAction);

        XenoAtom.Terminal.Terminal.WriteAtomic(ansiWriter =>
        {
            var customStyles = XenoAtom.Terminal.Terminal.MarkupStyles;
            var markup = customStyles is { Count: > 0 }
                ? new AnsiMarkup(ansiWriter, customStyles)
                : new AnsiMarkup(ansiWriter);
            var writer = new Writer(ansiWriter, markup);
            writeAction(writer);
        });
    }

    public static void WriteLine(string text)
    {
        Write(writer => writer.WriteLineEscaped(text));
    }

    internal readonly struct Writer
    {
        private readonly AnsiWriter _ansiWriter;
        private readonly AnsiMarkup _markup;

        public Writer(AnsiWriter ansiWriter, AnsiMarkup markup)
        {
            _ansiWriter = ansiWriter;
            _markup = markup;
        }

        public void WriteMarkupLine(string markupText)
        {
            _markup.Write(markupText);
            _ansiWriter.Write(Environment.NewLine);
        }

        public void WriteLineEscaped(string text)
        {
            _markup.Write(AnsiMarkup.Escape(text ?? string.Empty));
            _ansiWriter.Write(Environment.NewLine);
        }
    }
}
