using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace FerryKit.Core.Tests.Functional
{
    public class EnumAndNumOpTests
    {
        [Flags] private enum Flags : short { None = 0, First = 1, Second = 2 }
        private enum SByteValue : sbyte { Min = sbyte.MinValue }
        private enum ByteValue : byte { Max = byte.MaxValue }
        private enum ShortValue : short { Min = short.MinValue }
        private enum UShortValue : ushort { Max = ushort.MaxValue }
        private enum IntValue : int { Min = int.MinValue }
        private enum UIntValue : uint { Max = uint.MaxValue }
        private enum LongValue : long { Min = long.MinValue }
        private enum ULongValue : ulong { Max = ulong.MaxValue }
        private enum LargeSignedValue : int
        {
            Zero = 0,
            One = 1,
            Two = 2,
            Three = 3,
            Four = 4,
            Five = 5,
            Six = 6,
            Seven = 7,
            Eight = 8,
            Nine = 9,
            Ten = 10,
            Eleven = 11,
            Twelve = 12,
            Thirteen = 13,
            Fourteen = 14,
            Fifteen = 15,
            Sixteen = 16,
            Negative = -1,
        }
        private enum LargeSByteValue : sbyte
        {
            Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7,
            Eight = 8, Nine = 9, Ten = 10, Eleven = 11, Twelve = 12, Thirteen = 13,
            Fourteen = 14, Fifteen = 15, Min = sbyte.MinValue, Negative = -1,
        }
        private enum LargeShortValue : short
        {
            Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7,
            Eight = 8, Nine = 9, Ten = 10, Eleven = 11, Twelve = 12, Thirteen = 13,
            Fourteen = 14, Fifteen = 15, Min = short.MinValue, Negative = -1,
        }
        private enum LargeLongValue : long
        {
            Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7,
            Eight = 8, Nine = 9, Ten = 10, Eleven = 11, Twelve = 12, Thirteen = 13,
            Fourteen = 14, Fifteen = 15, Min = long.MinValue, Negative = -1,
        }
        private enum LargeULongValue : ulong
        {
            Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7,
            Eight = 8, Nine = 9, Ten = 10, Eleven = 11, Twelve = 12, Thirteen = 13,
            Fourteen = 14, Fifteen = 15, Max = ulong.MaxValue,
        }
        private enum AliasedValue { Primary = 1, Alias = 1, Other = 2 }
        private enum LargeCaseCollision
        {
            lower = 0, Value01 = 1, Value02 = 2, Value03 = 3, Value04 = 4, Value05 = 5,
            Value06 = 6, Value07 = 7, Value08 = 8, Value09 = 9, Value10 = 10, Value11 = 11,
            Value12 = 12, Value13 = 13, Value14 = 14, Lower = 15,
        }
        private enum ValueBoundary17
        {
            Value00, Value01, Value02, Value03, Value04, Value05, Value06, Value07,
            Value08, Value09, Value10, Value11, Value12, Value13, Value14, Value15,
            Value16,
        }
        private enum ValueBoundary64
        {
            Value00, Value01, Value02, Value03, Value04, Value05, Value06, Value07,
            Value08, Value09, Value10, Value11, Value12, Value13, Value14, Value15,
            Value16, Value17, Value18, Value19, Value20, Value21, Value22, Value23,
            Value24, Value25, Value26, Value27, Value28, Value29, Value30, Value31,
            Value32, Value33, Value34, Value35, Value36, Value37, Value38, Value39,
            Value40, Value41, Value42, Value43, Value44, Value45, Value46, Value47,
            Value48, Value49, Value50, Value51, Value52, Value53, Value54, Value55,
            Value56, Value57, Value58, Value59, Value60, Value61, Value62, Value63,
        }
        private enum ValueBoundary65
        {
            Value00, Value01, Value02, Value03, Value04, Value05, Value06, Value07,
            Value08, Value09, Value10, Value11, Value12, Value13, Value14, Value15,
            Value16, Value17, Value18, Value19, Value20, Value21, Value22, Value23,
            Value24, Value25, Value26, Value27, Value28, Value29, Value30, Value31,
            Value32, Value33, Value34, Value35, Value36, Value37, Value38, Value39,
            Value40, Value41, Value42, Value43, Value44, Value45, Value46, Value47,
            Value48, Value49, Value50, Value51, Value52, Value53, Value54, Value55,
            Value56, Value57, Value58, Value59, Value60, Value61, Value62, Value63,
            Value64,
        }

        [Test]
        public void EnumMetadataIterationParsingAndFlags_AreConsistent()
        {
            Assert.That(EnumHelper.Count<Flags>(), Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] { Flags.None, Flags.First, Flags.Second }, EnumHelper.GetValues<Flags>().ToArray());
            Assert.That(Flags.First.IsDefined(), Is.True);
            Assert.That(((Flags)8).IsDefined(), Is.False);
            Assert.That(EnumHelper.TrueForAll<Flags>(x => (short)x >= 0), Is.True);
            var values = new List<Flags>();
            EnumHelper.ForEach<Flags>(values.Add);
            CollectionAssert.AreEqual(EnumHelper.GetValues<Flags>().ToArray(), values);

            Flags flags = Flags.First;
            flags.AccumulateFlag(Flags.Second);
            Assert.That(flags, Is.EqualTo(Flags.First | Flags.Second));
            Assert.That(EnumHelper.ToUInt64Bits(ShortValue.Min), Is.EqualTo(unchecked((ushort)short.MinValue)));
            Assert.That(" first ".Parse<Flags>(ignoreCase: true), Is.EqualTo(Flags.First));
            Assert.That(" First ".TryParse(out Flags _, ignoreSpace: false), Is.False);
            Assert.That("1".AsSpan().TryParse(out Flags numeric), Is.True);
            Assert.That(numeric, Is.EqualTo(Flags.First));
            Assert.That("8".AsSpan().TryParse(out Flags _), Is.False);
            Assert.Throws<ArgumentException>(() => "missing".Parse<Flags>());
        }

        [Test]
        public void EnumParsing_SupportsEveryUnderlyingStorageType()
        {
            AssertEnum(SByteValue.Min, sbyte.MinValue);
            AssertEnum(ByteValue.Max, byte.MaxValue);
            AssertEnum(ShortValue.Min, short.MinValue);
            AssertEnum(UShortValue.Max, ushort.MaxValue);
            AssertEnum(IntValue.Min, int.MinValue);
            AssertEnum(UIntValue.Max, uint.MaxValue);
            AssertEnum(LongValue.Min, long.MinValue);
            AssertEnum(ULongValue.Max, ulong.MaxValue);
        }

        [Test]
        public void EnumParsing_RespectsSpanWhitespacePolicyAndEmptyInput()
        {
            Assert.That(ReadOnlySpan<char>.Empty.TryParse(out Flags _, ignoreSpace: false), Is.False);
            Assert.That(" 1 ".TryParse(out Flags _, ignoreSpace: false), Is.False);
            Assert.That(" 1 ".AsSpan().TryParse(out Flags _, ignoreSpace: false), Is.False);
            Assert.That(" 1 ".AsSpan().TryParse(out Flags value, ignoreSpace: true), Is.True);
            Assert.That(value, Is.EqualTo(Flags.First));
        }

        [Test]
        public void EnumParsing_IndexedLookupFindsBoundariesAndRejectsUndefinedValues()
        {
            Assert.That(EnumHelper.Count<LargeSignedValue>(), Is.GreaterThanOrEqualTo(16));
            AssertNumericEnum("0", LargeSignedValue.Zero);
            AssertNumericEnum("8", LargeSignedValue.Eight);
            AssertNumericEnum("16", LargeSignedValue.Sixteen);
            AssertNumericEnum("-1", LargeSignedValue.Negative);
            Assert.That("17".AsSpan().TryParse(out LargeSignedValue _), Is.False);
        }

        [Test]
        public void EnumParsing_IndexedValueLookupSupportsSignedWidthsAndUnsignedMaximum()
        {
            AssertNumericEnum("-128", LargeSByteValue.Min);
            AssertNumericEnum("-32768", LargeShortValue.Min);
            AssertNumericEnum("-9223372036854775808", LargeLongValue.Min);
            AssertNumericEnum("18446744073709551615", LargeULongValue.Max);

            Assert.That(LargeSByteValue.Negative.IsDefined(), Is.True);
            Assert.That(LargeShortValue.Min.IsDefined(), Is.True);
            Assert.That(LargeLongValue.Negative.IsDefined(), Is.True);
            Assert.That(LargeULongValue.Max.IsDefined(), Is.True);
            Assert.That(((LargeSByteValue)127).IsDefined(), Is.False);
            Assert.That(((LargeShortValue)127).IsDefined(), Is.False);
            Assert.That(((LargeLongValue)127).IsDefined(), Is.False);
            Assert.That(((LargeULongValue)127).IsDefined(), Is.False);
        }

        [Test]
        public void EnumParsing_NameLookupPreservesAliasesCaseRulesAndMisses()
        {
            AssertSpanEnum("Zero", LargeSignedValue.Zero);
            AssertSpanEnum("Eight", LargeSignedValue.Eight);
            AssertSpanEnum("Negative", LargeSignedValue.Negative);
            Assert.That("Missing".AsSpan().TryParse(out LargeSignedValue _), Is.False);

            AssertSpanEnum("Alias", AliasedValue.Alias);
            Assert.That(AliasedValue.Alias.IsDefined(), Is.True);

            AssertSpanEnum("Lower", LargeCaseCollision.Lower);
            Assert.That("LOWER".TryParse(out LargeCaseCollision stringValue, ignoreCase: true), Is.True);
            Assert.That("LOWER".AsSpan().TryParse(out LargeCaseCollision spanValue, ignoreCase: true), Is.True);
            Assert.That(spanValue, Is.EqualTo(stringValue));
        }

        [Test]
        public void EnumValueLookup_HandlesLinearBinaryAndHashSetThresholdBoundaries()
        {
            Assert.That(EnumHelper.Count<LargeCaseCollision>(), Is.EqualTo(16));
            Assert.That(EnumHelper.Count<ValueBoundary17>(), Is.EqualTo(17));
            Assert.That(EnumHelper.Count<ValueBoundary64>(), Is.EqualTo(64));
            Assert.That(EnumHelper.Count<ValueBoundary65>(), Is.EqualTo(65));

            AssertNumericEnum("15", LargeCaseCollision.Lower);
            AssertNumericEnum("16", ValueBoundary17.Value16);
            AssertNumericEnum("63", ValueBoundary64.Value63);
            AssertNumericEnum("64", ValueBoundary65.Value64);

            Assert.That("16".AsSpan().TryParse(out LargeCaseCollision _), Is.False);
            Assert.That("17".AsSpan().TryParse(out ValueBoundary17 _), Is.False);
            Assert.That("64".AsSpan().TryParse(out ValueBoundary64 _), Is.False);
            Assert.That("+16".TryParse(out ValueBoundary17 signed), Is.True);
            Assert.That(signed, Is.EqualTo(ValueBoundary17.Value16));
            Assert.That("65".AsSpan().TryParse(out ValueBoundary65 _), Is.False);

            Assert.That(LargeCaseCollision.Lower.IsDefined(), Is.True);
            Assert.That(ValueBoundary17.Value16.IsDefined(), Is.True);
            Assert.That(ValueBoundary64.Value63.IsDefined(), Is.True);
            Assert.That(ValueBoundary65.Value64.IsDefined(), Is.True);
            Assert.That(((LargeCaseCollision)16).IsDefined(), Is.False);
            Assert.That(((ValueBoundary17)17).IsDefined(), Is.False);
            Assert.That(((ValueBoundary64)64).IsDefined(), Is.False);
            Assert.That(((ValueBoundary65)65).IsDefined(), Is.False);
        }

        [Test]
        public void NumericOperators_CoverArithmeticComparisonMutationAndConversions()
        {
            VerifyOperator<int, IntOp>(2, 3, 5, 6, 1, 1);
            VerifyOperator<uint, UIntOp>(2, 3, 5, 6, 1, 1);
            VerifyOperator<long, LongOp>(2, 3, 5, 6, 1, 1);
            VerifyOperator<ulong, ULongOp>(2, 3, 5, 6, 1, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => default(UIntOp).FromInt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => default(ULongOp).FromInt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => default(UIntOp).ToInt((uint)int.MaxValue + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => default(LongOp).ToInt((long)int.MaxValue + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => default(ULongOp).ToInt((ulong)int.MaxValue + 1));
        }

        [Test]
        public void LongConversion_ValidatesBothBoundsBeforeNarrowing()
        {
            var op = default(LongOp);
            Assert.That(op.ToInt(int.MinValue), Is.EqualTo(int.MinValue));
            Assert.That(op.ToInt(int.MaxValue), Is.EqualTo(int.MaxValue));
            foreach (long value in new[] { long.MinValue, (long)int.MinValue - 1, (long)int.MaxValue + 1, long.MaxValue })
                Assert.Throws<ArgumentOutOfRangeException>(() => op.ToInt(value));
        }

        private static void VerifyOperator<T, TOp>(int ai, int bi, int sum, int product, int quotient, int remainder)
            where T : struct where TOp : struct, INumOp<T>
        {
            TOp op = default;
            T a = op.FromInt(ai);
            T b = op.FromInt(bi);
            Assert.That(op.ToInt(op.Add(a, b)), Is.EqualTo(sum));
            Assert.That(op.ToInt(op.Mul(a, b)), Is.EqualTo(product));
            Assert.That(op.ToInt(op.Div(b, a)), Is.EqualTo(quotient));
            Assert.That(op.ToInt(op.Mod(b, a)), Is.EqualTo(remainder));
            Assert.That(op.GT(b, a) && op.GTE(b, b) && op.LT(a, b) && op.LTE(a, a) && op.EQ(a, a), Is.True);
            Assert.That(op.ToInt(op.Inc(ref a)), Is.EqualTo(ai + 1));
            Assert.That(op.ToInt(op.Dec(ref a)), Is.EqualTo(ai));
            Assert.That(op.ToInt(op.Sub(b, a)), Is.EqualTo(bi - ai));
        }

        private static void AssertEnum<T, TValue>(T expected, TValue number) where T : struct, Enum where TValue : IFormattable
        {
            string text = number.ToString(null, CultureInfo.InvariantCulture);
            Assert.That(text.AsSpan().TryParse(out T actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static void AssertNumericEnum<T>(string text, T expected) where T : struct, Enum
        {
            Assert.That(text.AsSpan().TryParse(out T actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static void AssertSpanEnum<T>(string text, T expected) where T : struct, Enum
        {
            Assert.That(text.AsSpan().TryParse(out T actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
