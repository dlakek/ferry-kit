using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

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
                Assert.Throws<FormatException>(() => "".AsSpan().ToInt());
                Assert.That("255".AsSpan().ToByte(), Is.EqualTo(byte.MaxValue));
                Assert.That("-128".AsSpan().ToSByte(), Is.EqualTo(sbyte.MinValue));
                Assert.That("-32768".AsSpan().ToShort(), Is.EqualTo(short.MinValue));
                Assert.That("65535".AsSpan().ToUShort(), Is.EqualTo(ushort.MaxValue));
                Assert.That("4294967295".AsSpan().ToUInt(), Is.EqualTo(uint.MaxValue));
                Assert.That("18446744073709551615".AsSpan().ToULong(), Is.EqualTo(ulong.MaxValue));
                Assert.Throws<OverflowException>(() => "256".AsSpan().ToByte());
                Assert.That("-2".AsSpan().ToLong(), Is.EqualTo(-2L));
                Assert.That("1.5".AsSpan().ToFloat(), Is.EqualTo(1.5f));
                Assert.That("2.5".AsSpan().TryToDouble(out double d) && d == 2.5, Is.True);
                Assert.That("255".AsSpan().TryToByte(out byte byteValue) && byteValue == byte.MaxValue, Is.True);
                Assert.That("-128".AsSpan().TryToSByte(out sbyte sbyteValue) && sbyteValue == sbyte.MinValue, Is.True);
                Assert.That("-32768".AsSpan().TryToShort(out short shortValue) && shortValue == short.MinValue, Is.True);
                Assert.That("65535".AsSpan().TryToUShort(out ushort ushortValue) && ushortValue == ushort.MaxValue, Is.True);
                Assert.That("4294967295".AsSpan().TryToUInt(out uint uintValue) && uintValue == uint.MaxValue, Is.True);
                Assert.That("18446744073709551615".AsSpan().TryToULong(out ulong ulongValue) && ulongValue == ulong.MaxValue, Is.True);
                Assert.That("256".AsSpan().TryToByte(out _), Is.False);
                Assert.That("07/17/2026".AsSpan().ToDateTime(), Is.EqualTo(new DateTime(2026, 7, 17)));
                Assert.That("\"a\"\"b\"".AsSpan().ToStr(), Is.EqualTo("a\"b"));
                Assert.That("1".AsSpan().ToBool(), Is.True);
                Assert.That("0".AsSpan().TryToBool(out bool bit) && !bit, Is.True);
                Assert.That("bad".AsSpan().TryToInt(out _), Is.False);
                Assert.Throws<FormatException>(() => "bad".AsSpan().ToInt());
                Assert.That("value".AsSpan().TryTo<Guid>(out _), Is.False);
                Assert.Throws<NotSupportedException>(() => "value".AsSpan().To<Guid>());
            }
            finally { CultureInfo.CurrentCulture = original; }

            CollectionAssert.AreEqual(new[] { 1, 2 }, "1;2;".AsSpan().ToArray<int>());
            CollectionAssert.AreEqual(new[] { 1, 2 }, "1;2;".AsSpan().ToList<int>());
            var exactList = "1;2;3;4;5".AsSpan().ToList<int>();
            Assert.That(exactList.Capacity, Is.EqualTo(exactList.Count));
            Assert.That("1;2;3;4;5".AsSpan().TryToList(out List<int> exactTryList), Is.True);
            Assert.That(exactTryList.Capacity, Is.EqualTo(exactTryList.Count));
            var quotedList = "\"a;b\";c".AsSpan().ToList<string>();
            Assert.That(quotedList, Is.EqualTo(new[] { "a;b", "c" }));
            Assert.That(quotedList.Capacity, Is.EqualTo(quotedList.Count));
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
            var policy = new CustomPolicy(colSep: '\t', arrSep: '/', ignoreCase: true, escapeMode: QuoteEscapeMode.Csv);
            var splitter = new LineSplitter<CustomPolicy>("a\t\"b\nc\"\r\nd\te", policy);
            Assert.That(splitter.MoveNext(), Is.True);
            Assert.That(splitter.Current.ToString(), Is.EqualTo("a\t\"b\nc\""));
            var reader = new LineReader<CustomPolicy>(splitter.Current, policy);
            Assert.That(reader.Read<string>(), Is.EqualTo("a"));
            Assert.That(reader.Read<string>(), Is.EqualTo("b\nc"));
            Assert.That(reader.TryReadNext(out _), Is.False);
            Assert.That(splitter.MoveNext(), Is.True);
            Assert.That(splitter.Current.ToString(), Is.EqualTo("d\te"));
            Assert.That(splitter.MoveNext(), Is.False);

            Assert.That("a/b".AsSpan().ToList<string, CustomPolicy>(policy), Is.EqualTo(new[] { "a", "b" }));
            Assert.That("a".AsSpan().To<Options, CustomPolicy>(policy), Is.EqualTo(Options.A));
        }

        [Test]
        public void CustomPolicy_DefaultAndConstructor_ExposeDocumentedSettings()
        {
            var defaults = CustomPolicy.Default;
            var zeroInitialized = new CustomPolicy();
            var policy = new CustomPolicy(colSep: '\t', ignoreCase: true);

            Assert.That(zeroInitialized.ColSep, Is.EqualTo('\0'));
            Assert.That(zeroInitialized.ArrSep, Is.EqualTo('\0'));
            Assert.That(zeroInitialized.EscapeMode, Is.EqualTo(QuoteEscapeMode.None));

            Assert.That(defaults.ColSep, Is.EqualTo(ParsePolicyDefaults.ColumnSeparator));
            Assert.That(defaults.ArrSep, Is.EqualTo(ParsePolicyDefaults.ArraySeparator));
            Assert.That(defaults.SetSep, Is.EqualTo(ParsePolicyDefaults.SetSeparator));
            Assert.That(defaults.DictSep, Is.EqualTo(ParsePolicyDefaults.DictionarySeparator));
            Assert.That(defaults.PairSep, Is.EqualTo(ParsePolicyDefaults.PairSeparator));
            Assert.That(defaults.IgnoreCase, Is.EqualTo(ParsePolicyDefaults.IgnoreCase));
            Assert.That(defaults.EscapeMode, Is.EqualTo(ParsePolicyDefaults.EscapeMode));

            Assert.That(policy.ColSep, Is.EqualTo('\t'));
            Assert.That(policy.ArrSep, Is.EqualTo(ParsePolicyDefaults.ArraySeparator));
            Assert.That(policy.SetSep, Is.EqualTo(ParsePolicyDefaults.SetSeparator));
            Assert.That(policy.DictSep, Is.EqualTo(ParsePolicyDefaults.DictionarySeparator));
            Assert.That(policy.PairSep, Is.EqualTo(ParsePolicyDefaults.PairSeparator));
            Assert.That(policy.IgnoreCase, Is.True);
            Assert.That(policy.EscapeMode, Is.EqualTo(ParsePolicyDefaults.EscapeMode));
        }

        [Test]
        public void LineReader_DistinguishesMissingEmptyAndTrailingColumns()
        {
            var reader = new LineReader("1,,tail");
            Assert.That(reader.TryRead(out int id), Is.True);
            Assert.That(id, Is.EqualTo(1));
            Assert.That(reader.TryRead(out string empty), Is.True);
            Assert.That(empty, Is.Empty);
            Assert.That(reader.HasRemaining, Is.True);
            reader.SkipRemaining();
            Assert.That(reader.HasRemaining, Is.False);
            Assert.That(reader.TryRead(out string _), Is.False);

            Assert.That(TextParser.TryParse<TryRow>("1", out _, out _), Is.False);
            Assert.That(TextParser.TryParse<TryRow>("1,ok,extra", out _, out _), Is.False);
            Assert.That(TextParser.TryParse<TryRow>("1,", out var rows, out string reason), Is.True, reason);
            Assert.That(rows[0].Name, Is.Empty);

            var collections = new LineReader(",");
            Assert.That(collections.TryRead(out List<int> emptyList), Is.True);
            Assert.That(emptyList, Is.Empty);
            Assert.That(collections.TryRead(out int[] emptyArray), Is.True);
            Assert.That(emptyArray, Is.Empty);
            Assert.That(collections.TryRead(out List<int> missingList), Is.False);
            Assert.That(missingList, Is.Null);

            var scalar = new LineReader(ReadOnlySpan<char>.Empty);
            Assert.That(scalar.TryRead(out int _), Is.False);

            var quotedStrings = new LineReader("\" a \",\" b \"");
            Assert.That(quotedStrings.TryRead<string>(out string firstQuoted), Is.True);
            Assert.That(firstQuoted, Is.EqualTo(" a "));
            Assert.That(quotedStrings.Read<string>(), Is.EqualTo(" b "));

            var unquotedString = new LineReader(" a ");
            Assert.That(unquotedString.TryRead(out string trimmed), Is.True);
            Assert.That(trimmed, Is.EqualTo("a"));
        }

        [Test]
        public void StringParsing_UnescapesLongQuotedValues()
        {
            string prefix = new('a', 600);
            string input = $"\"{prefix}\"\"b\"";

            Assert.That(input.AsSpan().ToStr(), Is.EqualTo($"{prefix}\"b"));
        }

        [Test]
        public void Parsers_SupportExplicitSpanParsing()
        {
            var parser = new HexValueParser();
            Assert.That("15".AsSpan().TryToWith(out HexValue parsed, parser), Is.True);
            Assert.That(parsed.Value, Is.EqualTo(0x15));
            Assert.That("F".AsSpan().ToWith<HexValue, HexValueParser>(parser).Value, Is.EqualTo(0xF));
            Assert.That("xyz".AsSpan().TryToWith(out parsed, parser), Is.False);

            var reader = new LineReader("15,A;F");
            Assert.That(reader.TryReadNext(out var cell), Is.True);
            Assert.That(parser.TryParse(cell, out HexValue value), Is.True);
            Assert.That(value.Value, Is.EqualTo(0x15));
            Assert.That(reader.TryReadNext(out cell), Is.True);
            Assert.That(cell.TryToListWith(out List<HexValue> values, parser), Is.True);
            Assert.That(values.ConvertAll(static item => item.Value), Is.EqualTo(new[] { 10, 15 }));

            var policy = new CustomPolicy(colSep: '\t');
            var policyParser = new PolicyHexValueParser();
            Assert.That("20".AsSpan().TryToWith(out HexValue policyParsed, policy, policyParser), Is.True);
            Assert.That(policyParsed.Value, Is.EqualTo(0x20));
            Assert.That("F".AsSpan().ToWith<HexValue, CustomPolicy, PolicyHexValueParser>(policy, policyParser).Value, Is.EqualTo(0xF));

            var policyReader = new LineReader<CustomPolicy>("20\tA;F", policy);
            Assert.That(policyReader.TryReadNext(out cell), Is.True);
            Assert.That(policyParser.TryParse(cell, policy, out HexValue custom), Is.True);
            Assert.That(custom.Value, Is.EqualTo(0x20));
            Assert.That(policyReader.TryReadNext(out cell), Is.True);
            Assert.That(cell.TryToListWith(out List<HexValue> policyValues, policy, policyParser), Is.True);
            Assert.That(policyValues.ConvertAll(static item => item.Value), Is.EqualTo(new[] { 10, 15 }));
        }

        [Test]
        public void ParserWithWrappers_SupportThrowingCollections()
        {
            var parser = new HexValueParser();
            var intParser = new Parser<int>();

            var array = "A;F".AsSpan().ToArrayWith<HexValue, HexValueParser>(parser);
            CollectionAssert.AreEqual(new[] { 10, 15 }, Array.ConvertAll(array, static item => item.Value));

            var list = "A;F".AsSpan().ToListWith<HexValue, HexValueParser>(parser);
            CollectionAssert.AreEqual(new[] { 10, 15 }, list.ConvertAll(static item => item.Value));

            var set = "A|F".AsSpan().ToHashSetWith<HexValue, HexValueParser>(parser);
            CollectionAssert.AreEquivalent(new[] { 10, 15 }, new List<HexValue>(set).ConvertAll(static item => item.Value));

            var dictionary = "A=1|F=2".AsSpan().ToDictionaryWith<HexValue, int, HexValueParser, Parser<int>>(parser, intParser);
            Assert.That(dictionary[new HexValue { Value = 0xA }], Is.EqualTo(1));
            Assert.That(dictionary[new HexValue { Value = 0xF }], Is.EqualTo(2));

            var pair = "F=2".AsSpan().ToPairWith<HexValue, int, HexValueParser, Parser<int>>(parser, intParser);
            Assert.That((pair.Item1.Value, pair.Item2), Is.EqualTo((0xF, 2)));
            Assert.Throws<FormatException>(() => "xyz".AsSpan().ToArrayWith<HexValue, HexValueParser>(parser));

            var policy = CustomPolicy.Default;
            var policyParser = new PolicyHexValueParser();
            var policyIntParser = new Parser<int, CustomPolicy>();

            Assert.That("A;F".AsSpan().ToArrayWith<HexValue, CustomPolicy, PolicyHexValueParser>(policy, policyParser).Length, Is.EqualTo(2));
            Assert.That("A;F".AsSpan().ToListWith<HexValue, CustomPolicy, PolicyHexValueParser>(policy, policyParser).Count, Is.EqualTo(2));
            Assert.That("A|F".AsSpan().ToHashSetWith<HexValue, CustomPolicy, PolicyHexValueParser>(policy, policyParser).Count, Is.EqualTo(2));
            Assert.That("A=1|F=2".AsSpan().ToDictionaryWith<HexValue, int, CustomPolicy, PolicyHexValueParser, Parser<int, CustomPolicy>>(policy, policyParser, policyIntParser).Count, Is.EqualTo(2));
            Assert.That("F=2".AsSpan().ToPairWith<HexValue, int, CustomPolicy, PolicyHexValueParser, Parser<int, CustomPolicy>>(policy, policyParser, policyIntParser).Item1.Value, Is.EqualTo(0xF));
        }

        [Test]
        public void TableParser_HandlesHeadersWhitespaceExceptionsAndSelectableRollback()
        {
            string source = "id,name\r\n1,one\r\n\r\n2,\"two,quoted\"";
            List<Row> parsed = TextParser.Parse<Row>(source, skipFirstLine: true);
            Assert.That(parsed.Count, Is.EqualTo(2));
            Assert.That((parsed[1].Id, parsed[1].Name), Is.EqualTo((2, "two,quoted")));

            Assert.That(TextParser.TryParse<TryRow>(source, out List<TryRow> tried, out string reason, skipFirstLine: true), Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(tried.Count, Is.EqualTo(2));
            Assert.That(TextParser.TryParse<TryRow>("", out _, out reason), Is.False);
            Assert.That(reason, Does.Contain("null or empty"));
            Assert.Throws<FormatException>(() => TextParser.Parse<Row>("id,name\nbad,value", skipFirstLine: true));

            var result = new List<TryRow> { new TryRow { Id = 99 } };
            Assert.That(TextParser.TryParse("1,ok\nbad,no", result, out reason, revertOnFailure: false), Is.False);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(1));

            result = new List<TryRow> { new TryRow { Id = 99 } };
            Assert.That(TextParser.TryParse("1,ok\nbad,no", result, out reason, revertOnFailure: true), Is.False);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(99));

            Assert.That(TextParser.TryParse("1,ok\n2,two", result, out reason, revertOnFailure: true), Is.True);
            Assert.That(result.ConvertAll(x => x.Id), Is.EqualTo(new[] { 1, 2 }));
            Assert.Throws<ArgumentNullException>(() => TextParser.TryParse<TryRow>("1,ok", null, out _));
        }

        [Test]
        public void TryParse_SupportsNestedTryParsableWithDefaultReader()
        {
            Assert.That(TextParser.TryParse<NestedTryRow>("7,0.25,alpha", out var rows, out string reason), Is.True, reason);
            Assert.That(rows[0].Data.Id, Is.EqualTo(7));
            Assert.That(rows[0].Data.Ratio, Is.EqualTo(0.25f));
            Assert.That(rows[0].Name, Is.EqualTo("alpha"));
        }

        [Test]
        public void ParseAndTryParse_SupportExplicitPolicyInterfaces()
        {
            var policy = new CustomPolicy(colSep: '\t');
            var parsed = TextParser.Parse<PolicyRow<CustomPolicy>, CustomPolicy>("7\t0.25\talpha", policy);
            Assert.That(parsed[0].Data.Id, Is.EqualTo(7));
            Assert.That(parsed[0].Data.Ratio, Is.EqualTo(0.25f));
            Assert.That(parsed[0].Name, Is.EqualTo("alpha"));

            Assert.That(TextParser.TryParse<PolicyTryRow<CustomPolicy>, CustomPolicy>("8\t0.5\tbeta", policy, out var tried, out string reason), Is.True, reason);
            Assert.That(tried[0].Data.Id, Is.EqualTo(8));
            Assert.That(tried[0].Data.Ratio, Is.EqualTo(0.5f));
            Assert.That(tried[0].Name, Is.EqualTo("beta"));
        }

        public sealed class Row : IParsable
        {
            public int Id;
            public string Name;
            public void Parse(ref LineReader reader)
            {
                Id = reader.Read<int>();
                Name = reader.Read<string>();
            }
        }

        public sealed class TryRow : ITryParsable
        {
            public int Id;
            public string Name;
            public bool TryParse(ref LineReader reader)
                => reader.TryRead(out Id) && reader.TryRead(out Name);
        }

        public struct NestedData : ITryParsable
        {
            public int Id;
            public float Ratio;

            public bool TryParse(ref LineReader reader)
                => reader.TryRead(out Id) && reader.TryRead(out Ratio);
        }

        public sealed class NestedTryRow : ITryParsable
        {
            public NestedData Data;
            public string Name;

            public bool TryParse(ref LineReader reader)
                => Data.TryParse(ref reader) && reader.TryRead(out Name);
        }

        public struct PolicyData
        {
            public int Id;
            public float Ratio;

            public void Parse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
            {
                Id = reader.Read<int>();
                Ratio = reader.Read<float>();
            }

            public bool TryParse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
                => reader.TryRead(out Id) && reader.TryRead(out Ratio);
        }

        public sealed class PolicyRow<P> : IParsable<P> where P : struct, IParsePolicy
        {
            public PolicyData Data;
            public string Name;

            public void Parse(ref LineReader<P> reader)
            {
                Data.Parse(ref reader);
                Name = reader.Read<string>();
            }
        }

        public sealed class PolicyTryRow<P> : ITryParsable<P> where P : struct, IParsePolicy
        {
            public PolicyData Data;
            public string Name;

            public bool TryParse(ref LineReader<P> reader)
                => Data.TryParse(ref reader) && reader.TryRead(out Name);
        }

        public struct HexValue { public int Value; }

        public readonly struct HexValueParser : IParser<HexValue>
        {
            public HexValue Parse(ReadOnlySpan<char> span)
                => TryParse(span, out var result) ? result : throw new FormatException();

            public bool TryParse(ReadOnlySpan<char> span, out HexValue result)
            {
                bool success = int.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value);
                result = new HexValue { Value = value };
                return success;
            }
        }

        public readonly struct PolicyHexValueParser : IParser<HexValue, CustomPolicy>
        {
            public HexValue Parse(ReadOnlySpan<char> span, CustomPolicy policy)
                => TryParse(span, policy, out var result) ? result : throw new FormatException();

            public bool TryParse(ReadOnlySpan<char> span, CustomPolicy policy, out HexValue result)
                => new HexValueParser().TryParse(span, out result);
        }
    }
}
