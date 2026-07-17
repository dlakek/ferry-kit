using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using FerryKit.Core;
using NUnit.Framework;

namespace FerryKit.Tests.Functional
{
    public class FixedArrayTests
    {
        [Test]
        public void FixedArray_TryParse_ConsumesOneCellFromLineReader()
        {
            const string source = "7,1;2;3,0.5";

            Assert.That(TextParser.TryParse<Row>(source, out List<Row> rows, out string reason), Is.True, reason);
            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Id, Is.EqualTo(7));
            Assert.That(rows[0].Values.Count, Is.EqualTo(3));
            Assert.That(rows[0].Values[0], Is.EqualTo(1));
            Assert.That(rows[0].Values[1], Is.EqualTo(2));
            Assert.That(rows[0].Values[2], Is.EqualTo(3));
            Assert.That(rows[0].Ratio, Is.EqualTo(0.5f));
        }

        [Test]
        public void FixedArrayParsing_RespectsPolicyAndFailureSemantics()
        {
            var policy = new ParseHelper.Custom(arrSep: '/');

            var values = new FixedArray4<int>();
            Assert.That(values.TryParse("1/2/3".AsSpan(), policy), Is.True);
            Assert.That(values.Count, Is.EqualTo(3));
            Assert.That(values[2], Is.EqualTo(3));

            Assert.That(values.TryParse("bad".AsSpan(), policy), Is.False);
            Assert.That(values.Count, Is.EqualTo(3));
            Assert.That(values[0], Is.EqualTo(1));
            Assert.That(values[2], Is.EqualTo(3));

            var overflow = new FixedArray4<int>();
            Assert.That(overflow.TryParse("1/2/3/4/5".AsSpan(), policy), Is.False);
            Assert.That(overflow.Count, Is.Zero);

            var invalid = new FixedArray4<int>();
            Assert.That(invalid.TryParse("1/bad".AsSpan(), policy), Is.False);
            Assert.That(invalid.Count, Is.Zero);

            var empty = new FixedArray4<int>();
            Assert.That(empty.TryParse(ReadOnlySpan<char>.Empty, policy), Is.True);
            Assert.That(empty.Count, Is.Zero);

            Assert.That(values.TryParse("4/5/".AsSpan(), policy), Is.True);
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values[0], Is.EqualTo(4));
            Assert.That(values[1], Is.EqualTo(5));
            Assert.That(values[2], Is.Zero);
        }

        [Test]
        public void Indexer_RejectsAccessBeyondCapacityWhenSerializedCountIsCorrupted()
        {
            var values = new FixedArray4<int>();
            Assert.That(values.TryParse("1;2;3;4".AsSpan(), default(ParseHelper.Default)), Is.True);

            values = SetCount(values, uint.MaxValue);

            Assert.That(values[3], Is.EqualTo(4));
            Assert.That(values[4], Is.Zero);
            Assert.That(values[-1], Is.Zero);
        }

        [Test]
        public void ItemFields_RemainContiguousForUnsafeAccess()
        {
            AssertContiguousItems<FixedArray2<byte>, byte>(FixedArray2<byte>.Capacity);
            AssertContiguousItems<FixedArray3<short>, short>(FixedArray3<short>.Capacity);
            AssertContiguousItems<FixedArray4<int>, int>(FixedArray4<int>.Capacity);
            AssertContiguousItems<FixedArray5<double>, double>(FixedArray5<double>.Capacity);
        }

        [Serializable]
        public struct Row : ITryParsable
        {
            public int Id;
            public FixedArray4<int> Values;
            public float Ratio;

            public bool TryParse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
                => reader.TryRead(out Id)
                && Values.TryParse(ref reader)
                && reader.TryRead(out Ratio);
        }

        private static TArray SetCount<TArray>(TArray array, uint count) where TArray : struct
        {
            object boxed = array;
            typeof(TArray).GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boxed, count);
            return (TArray)boxed;
        }

        private static void AssertContiguousItems<TArray, TItem>(int capacity)
            where TArray : struct
            where TItem : unmanaged
        {
            long firstOffset = Marshal.OffsetOf<TArray>("_item0").ToInt64();
            int stride = Marshal.SizeOf<TItem>();
            for (int i = 1; i < capacity; ++i)
            {
                Assert.That(Marshal.OffsetOf<TArray>($"_item{i}").ToInt64(), Is.EqualTo(firstOffset + stride * i));
            }
        }
    }
}
