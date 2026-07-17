using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace FerryKit.Core.Tests.Functional
{
    public class TextParserTests
    {
        [Flags] private enum Options : byte { None = 0, A = 1, B = 2 }

        [Test]
        public void PrimitiveAndCompositeParsing_CoversSuccessEmptyAndFailurePaths()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                    Assert.That("".AsSpan().ToInt(), Is.Zero);
                    Assert.That("-2".AsSpan().ToLong(), Is.EqualTo(-2L));
                    Assert.That("1.5".AsSpan().ToFloat(), Is.EqualTo(1.5f));
                    Assert.That("2.5".AsSpan().TryToDouble(out double d) && d == 2.5, Is.True);
                    Assert.That("07/17/2026".AsSpan().ToDateTime(), Is.EqualTo(new DateTime(2026, 7, 17)));
                    Assert.That("\"a\"\"b\"".AsSpan().ToStr(), Is.EqualTo("a\"b"));
                    Assert.That("1".AsSpan().ToBool(), Is.True);
                    Assert.That("0".AsSpan().TryToBool(out bool bit) && !bit, Is.True);
                    Assert.That("bad".AsSpan().TryToInt(out _), Is.False);
                Assert.Throws<FormatException>(() => "bad".AsSpan().ToInt());
            }
            finally { CultureInfo.CurrentCulture = original; }

            CollectionAssert.AreEqual(new[] { 1, 2 }, "1;2;".AsSpan().ToArray<int>());
            CollectionAssert.AreEqual(new[] { 1, 2 }, "1;2;".AsSpan().ToList<int>());
            Assert.That("1;bad".AsSpan().TryToArray(out int[] failedArray), Is.False);
            Assert.That(failedArray, Is.Null);
            Assert.That("1|2|".AsSpan().ToHashSet<int>().SetEquals(new[] { 1, 2 }), Is.True);
            Assert.Throws<ArgumentException>(() => "1|1".AsSpan().ToHashSet<int>());
            Assert.That("1|1".AsSpan().TryToHashSet(out HashSet<int> failedSet), Is.False);
            Assert.That(failedSet, Is.Null);
            CollectionAssert.AreEquivalent(new Dictionary<int, string> { [1] = "a", [2] = "b" }, "1=a|2=b|".AsSpan().ToDictionary<int, string>());
            Assert.That("1=a|1=b".AsSpan().TryToDictionary(out Dictionary<int, string> failedDictionary), Is.False);
            Assert.That(failedDictionary, Is.Null);
            Assert.That("3=value".AsSpan().ToPair<int, string>(), Is.EqualTo((3, "value")));
            Assert.That("invalid".AsSpan().TryToPair<int, int>(out _), Is.False);
            Assert.Throws<FormatException>(() => "invalid".AsSpan().ToPair<int, int>());
            Assert.That("A|B|".AsSpan().ToEnum<Options>(), Is.EqualTo(Options.A | Options.B));
        }

        [Test]
        public void CustomPolicyLineSplitterAndReader_RespectQuotedSeparatorsAndLineEndings()
        {
            var policy = new ParseHelper.Custom(colSep: '\t', arrSep: '/', ignoreCase: true, escapeMode: FerryKit.Core.StringHelper.QuoteEscapeMode.Csv);
            var splitter = new LineSplitter<ParseHelper.Custom>("a\t\"b\nc\"\r\nd\te", policy);
            Assert.That(splitter.MoveNext(), Is.True);
            Assert.That(splitter.Current.ToString(), Is.EqualTo("a\t\"b\nc\""));
            var reader = new LineReader<ParseHelper.Custom>(splitter.Current, policy);
            Assert.That(reader.Read<string>(), Is.EqualTo("a"));
            Assert.That(reader.Read<string>(), Is.EqualTo("b\nc"));
            Assert.That(reader.ReadNext().IsEmpty, Is.True);
            Assert.That(splitter.MoveNext(), Is.True);
            Assert.That(splitter.Current.ToString(), Is.EqualTo("d\te"));
            Assert.That(splitter.MoveNext(), Is.False);

            Assert.That("a/b".AsSpan().To<List<string>, ParseHelper.Custom>(policy), Is.EqualTo(new[] { "a", "b" }));
            Assert.That("a".AsSpan().To<Options, ParseHelper.Custom>(policy), Is.EqualTo(Options.A));
        }

        [Test]
        public void TableParser_HandlesHeadersWhitespaceExceptionsAndSelectableRollback()
        {
            string source = "id,name\r\n1,one\r\n\r\n2,\"two,quoted\"";
            List<Row> parsed = TextParser.Parse<Row>(source, isSkipFirstLine: true);
            Assert.That(parsed.Count, Is.EqualTo(2));
            Assert.That((parsed[1].Id, parsed[1].Name), Is.EqualTo((2, "two,quoted")));

            Assert.That(TextParser.TryParse<TryRow>(source, out List<TryRow> tried, out string reason, isSkipFirstLine: true), Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(tried.Count, Is.EqualTo(2));
            Assert.That(TextParser.TryParse<TryRow>("", out _, out reason), Is.False);
            Assert.That(reason, Does.Contain("null or empty"));
            Assert.Throws<FormatException>(() => TextParser.Parse<Row>("id,name\nbad,value", isSkipFirstLine: true));

            var result = new List<TryRow> { new TryRow { Id = 99 } };
            Assert.That(TextParser.TryParse("1,ok\nbad,no", result, out reason, isRevertWhenFail: false), Is.False);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(1));

            result = new List<TryRow> { new TryRow { Id = 99 } };
            Assert.That(TextParser.TryParse("1,ok\nbad,no", result, out reason, isRevertWhenFail: true), Is.False);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(99));

            Assert.That(TextParser.TryParse("1,ok\n2,two", result, out reason, isRevertWhenFail: true), Is.True);
            Assert.That(result.ConvertAll(x => x.Id), Is.EqualTo(new[] { 1, 2 }));
            Assert.Throws<ArgumentNullException>(() => TextParser.TryParse<TryRow>("1,ok", null, out _));
        }

        public sealed class Row : IParsable
        {
            public int Id;
            public string Name;
            public void Parse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
            {
                Id = reader.Read<int>();
                Name = reader.Read<string>();
            }
        }

        public sealed class TryRow : ITryParsable
        {
            public int Id;
            public string Name;
            public bool TryParse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
                => reader.TryRead(out Id) && reader.TryRead(out Name);
        }
    }
}
