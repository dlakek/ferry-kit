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
    }
}
