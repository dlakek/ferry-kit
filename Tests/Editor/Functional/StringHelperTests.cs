using NUnit.Framework;
using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using CoreBuilder = FerryKit.Core.StringBuilderHelper;
using CoreString = FerryKit.Core.StringHelper;

namespace FerryKit.Core.Tests.Functional
{
    public class StringHelperTests
    {
        [TestCase(int.MinValue)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(99)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        public void SizeFormatting_HandlesIntegerBoundaries(int size)
        {
            Assert.That(CoreString.Size("x", size), Is.EqualTo($"<size={size.ToString(CultureInfo.InvariantCulture)}>x</size>"));
        }

        [Test]
        public void SizeFormatting_UsesRichTextNumericSyntaxWithCustomNegativeSigns()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = "minus";
            try
            {
                CultureInfo.CurrentCulture = culture;
                const string expected = "<size=-2147483648>x</size>";
                Assert.That(CoreString.Size("x", int.MinValue), Is.EqualTo(expected));
                Assert.That(CoreBuilder.AppendSize(new StringBuilder(), "x", int.MinValue).ToString(), Is.EqualTo(expected));
            }
            finally { CultureInfo.CurrentCulture = original; }
        }

        [Test]
        public void NumericStart_HandlesEveryCharacterWithoutOverflow()
        {
            for (int code = char.MinValue; code <= char.MaxValue; ++code)
            {
                char value = (char)code;
                bool expected = (value >= '0' && value <= '9') || value == '+' || value == '-';
                if (CoreString.IsNumericStart(value) != expected)
                    Assert.Fail($"Unexpected numeric-start result for character U+{code:X4}.");
            }
        }

        [Test]
        public void QuoteSearch_InvalidStartIndexesReturnNoMatch()
        {
            foreach (QuoteEscapeMode mode in Enum.GetValues(typeof(QuoteEscapeMode)))
            {
                foreach (int start in new[] { int.MinValue, -1, 3, int.MaxValue })
                    Assert.That("a,b".AsSpan().IndexOfUnquoted(',', start, mode), Is.EqualTo(-1));
            }
        }

        [Test]
        public void QuoteSearch_PreservesQuoteAndEscapeCharacterSearchBehavior()
        {
            Assert.That("\"".AsSpan().IndexOfUnquoted('"', QuoteEscapeMode.None), Is.EqualTo(-1));
            Assert.That("\"".AsSpan().IndexOfUnquoted('"', QuoteEscapeMode.Backslash), Is.EqualTo(-1));
            Assert.That("\\".AsSpan().IndexOfUnquoted('\\', QuoteEscapeMode.Backslash), Is.EqualTo(-1));
            Assert.That("\"".AsSpan().IndexOfUnquoted('"', QuoteEscapeMode.Csv), Is.Zero);
            Assert.That("\\".AsSpan().IndexOfUnquoted('\\', QuoteEscapeMode.None), Is.Zero);
        }

        [Test]
        public void QuoteSearch_MatchesScalarReferenceAcrossEscapesAndStartIndexes()
        {
            char[] alphabet = { 'a', ';', '"', '\\' };
            for (int length = 0, combinations = 1; length <= 6; ++length, combinations *= alphabet.Length)
            {
                var buffer = new char[length];
                for (int code = 0; code < combinations; ++code)
                {
                    int value = code;
                    for (int i = 0; i < length; ++i, value /= alphabet.Length)
                        buffer[i] = alphabet[value % alphabet.Length];
                    string text = new(buffer);
                    foreach (QuoteEscapeMode mode in Enum.GetValues(typeof(QuoteEscapeMode)))
                    {
                        for (int start = -1; start <= length; ++start)
                        {
                            int expected = ReferenceUnquotedSeparator(text, start, mode);
                            int actual = text.AsSpan().IndexOfUnquoted(';', start, mode);
                            if (actual != expected)
                                Assert.Fail($"Quote search mismatch: {text}, mode={mode}, start={start}, expected={expected}, actual={actual}.");
                        }
                    }
                }
            }
        }

        private static int ReferenceUnquotedSeparator(string text, int start, QuoteEscapeMode mode)
        {
            if (start < 0 || start >= text.Length)
                return -1;
            bool inQuotes = false;
            for (int i = 0; i < text.Length; ++i)
            {
                char value = text[i];
                if (inQuotes && mode == QuoteEscapeMode.Backslash && value == '\\')
                {
                    ++i;
                    continue;
                }
                if (value == '"')
                {
                    if (inQuotes && mode == QuoteEscapeMode.Csv && i + 1 < text.Length && text[i + 1] == '"')
                        ++i;
                    else
                        inQuotes = !inQuotes;
                }
                else if (!inQuotes && value == ';' && i >= start)
                {
                    return i;
                }
            }
            return -1;
        }

        [Test]
        public void RichTextHelpers_CreateExpectedTags()
        {
            Assert.That(CoreString.Bold("x"), Is.EqualTo("<b>x</b>"));
            Assert.That(CoreString.Italic("x"), Is.EqualTo("<i>x</i>"));
            Assert.That(CoreString.Strike("x"), Is.EqualTo("<s>x</s>"));
            Assert.That(CoreString.Underline("x"), Is.EqualTo("<u>x</u>"));
            Assert.That(CoreString.Sub("x"), Is.EqualTo("<sub>x</sub>"));
            Assert.That(CoreString.Sup("x"), Is.EqualTo("<sup>x</sup>"));
            Assert.That(CoreString.Size("x", -12), Is.EqualTo("<size=-12>x</size>"));
            Assert.That(CoreString.Link("x", "url"), Is.EqualTo("<link=url>x</link>"));
            Assert.That(CoreString.Color("x", "red"), Is.EqualTo("<color=red>x</color>"));
            Assert.That(CoreString.Color("x", 0, 127, 255), Is.EqualTo("<color=#007FFF>x</color>"));
            Assert.That(FerryKit.StringHelper.Color("x", new Color32(1, 2, 255, 9)), Is.EqualTo("<color=#0102FF>x</color>"));
        }

        [Test]
        public void SpanAndCaseHelpers_HandleBoundariesAndQuotedSeparators()
        {
            Assert.That(CoreString.TrimQuotes("\"a\"".AsSpan()).ToString(), Is.EqualTo("a"));
            Assert.That(CoreString.TrimEndOne("a;;".AsSpan(), ';').ToString(), Is.EqualTo("a;"));
            Assert.That(CoreString.TrimStartOne("..a".AsSpan(), '.').ToString(), Is.EqualTo(".a"));
            Assert.That("a,\"b,c\",d".AsSpan().IndexOfUnquoted(',', QuoteEscapeMode.Csv), Is.EqualTo(1));
            Assert.That("a,\"b,c\",d".AsSpan().IndexOfUnquoted(',', 2, QuoteEscapeMode.Csv), Is.EqualTo(7));
            Assert.That("\"a,b\",c".AsSpan().IndexOfUnquoted(',', 2, QuoteEscapeMode.Csv), Is.EqualTo(5));
            Assert.That("\"a,b\",c".AsSpan().IndexOfUnquoted(',', 2), Is.EqualTo(5));
            Assert.That("\"a\\\"b,c\",d".AsSpan().IndexOfUnquoted(',', QuoteEscapeMode.Backslash), Is.EqualTo(8));
            Assert.That("\"a\\\"b,c\",d".AsSpan().IndexOfUnquoted(',', 5, QuoteEscapeMode.Backslash), Is.EqualTo(8));
            Assert.That(CoreString.HasWhiteSpace((string)null), Is.False);
            Assert.That(CoreString.HasWhiteSpace("a b"), Is.True);
            Assert.That(CoreString.Truncate("abcdef", 5), Is.EqualTo("ab..."));
            Assert.That(CoreString.Truncate("abcdef".AsSpan(), 2), Is.EqualTo(".."));
            Assert.That(CoreString.ToCamelCase("URLValue"), Is.EqualTo("urlValue"));
            Assert.That(CoreString.ToPascalCase("value".AsSpan()), Is.EqualTo("Value"));
        }

        [Test]
        public void StringBuilderHelpers_AppendEveryTagShape()
        {
            var sb = new StringBuilder();
            CoreBuilder.AppendBold(sb, "b");
            CoreBuilder.AppendItalic(sb, "i");
            CoreBuilder.AppendStrike(sb, "s");
            CoreBuilder.AppendUnderline(sb, "u");
            CoreBuilder.AppendSub(sb, "sub");
            CoreBuilder.AppendSup(sb, "sup");
            CoreBuilder.AppendSize(sb, "z", 4);
            CoreBuilder.AppendLink(sb, "l", "u");
            CoreBuilder.AppendColor(sb, "c", "red");
            FerryKit.StringBuilderHelper.AppendColor(sb, "h", new Color32(10, 11, 12, 0));
            Assert.That(sb.ToString(), Is.EqualTo("<b>b</b><i>i</i><s>s</s><u>u</u><sub>sub</sub><sup>sup</sup><size=4>z</size><link=u>l</link><color=red>c</color><color=#0A0B0C>h</color>"));
        }
    }
}
