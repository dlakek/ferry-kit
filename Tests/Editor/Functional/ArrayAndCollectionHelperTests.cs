using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FerryKit.Core.Tests.Functional
{
    public class ArrayAndCollectionHelperTests
    {
        [Test]
        public void ArraySearch_ValidatesRangesLikeSystemArray()
        {
            Predicate<int> match = static value => value == 1;
            int[] boundaries = { int.MinValue, -1, 0, 1, 2, 3, 4, int.MaxValue };
            foreach (var values in new[] { Array.Empty<int>(), new[] { 0, 1, 1 } })
            {
                foreach (int start in boundaries)
                {
                    Assert.That(SearchOutcome(() => ArrayHelper.FindIndex(values, start, match)),
                        Is.EqualTo(SearchOutcome(() => Array.FindIndex(values, start, match))));
                    Assert.That(SearchOutcome(() => ArrayHelper.FindLastIndex(values, start, match)),
                        Is.EqualTo(SearchOutcome(() => Array.FindLastIndex(values, start, match))));
                    foreach (int count in boundaries)
                    {
                        Assert.That(SearchOutcome(() => ArrayHelper.FindIndex(values, start, count, match)),
                            Is.EqualTo(SearchOutcome(() => Array.FindIndex(values, start, count, match))));
                        Assert.That(SearchOutcome(() => ArrayHelper.FindLastIndex(values, start, count, match)),
                            Is.EqualTo(SearchOutcome(() => Array.FindLastIndex(values, start, count, match))));
                    }
                }
                Assert.Throws<ArgumentNullException>(() => ArrayHelper.FindIndex(values, null));
                Assert.Throws<ArgumentNullException>(() => ArrayHelper.FindLastIndex(values, null));
            }
            Assert.Throws<ArgumentNullException>(() => ArrayHelper.FindIndex<int>(null, match));
            Assert.Throws<ArgumentNullException>(() => ArrayHelper.FindLastIndex<int>(null, match));
        }

        [Test]
        public void ArrayFindAll_PreservesPredicateMutationsAndHandlesNoMatches()
        {
            int[] values = { 1, 2 };
            int index = 0;
            var result = ArrayHelper.FindAll(values, _ =>
            {
                values[index++] *= 10;
                return true;
            });
            Assert.That(result, Is.EqualTo(new[] { 10, 20 }));
            Assert.That(ArrayHelper.FindAll(values, static _ => false), Is.Empty);
            Assert.Throws<ArgumentNullException>(() => ArrayHelper.FindAll<int>(null, static _ => true));
            Assert.Throws<ArgumentNullException>(() => ArrayHelper.FindAll(Array.Empty<int>(), null));
        }

        private static (int Index, Type Exception) SearchOutcome(Func<int> search)
        {
            try { return (search(), null); }
            catch (Exception ex) { return (0, ex.GetType()); }
        }

        [Test]
        public void ArrayHelpers_CoverSearchMutationProjectionAndBounds()
        {
            int[] values = { 3, 1, 2, 1 };
            Assert.That(ArrayHelper.IndexOf(values, 1), Is.EqualTo(1));
            Assert.That(ArrayHelper.IndexOf(values, 1, 2), Is.EqualTo(3));
            Assert.That(ArrayHelper.IndexOf(values, 1, 1, 2), Is.EqualTo(1));
            Assert.That(ArrayHelper.LastIndexOf(values, 1), Is.EqualTo(3));
            Assert.That(ArrayHelper.LastIndexOf(values, 1, 2), Is.EqualTo(1));
            Assert.That(ArrayHelper.LastIndexOf(values, 1, 3, 3), Is.EqualTo(3));
            Assert.That(ArrayHelper.Contains(values, 2), Is.True);
            Assert.That(ArrayHelper.FindIndex(values, x => x == 2), Is.EqualTo(2));
            Assert.That(ArrayHelper.FindIndex(values, 2, x => x == 1), Is.EqualTo(3));
            Assert.That(ArrayHelper.FindLastIndex(values, x => x == 1), Is.EqualTo(3));
            Assert.That(ArrayHelper.Find(values, x => x > 2), Is.EqualTo(3));
            Assert.That(ArrayHelper.FindLast(values, x => x == 1), Is.EqualTo(1));
            Assert.That(ArrayHelper.Exists(values, x => x == 9), Is.False);
            Assert.That(ArrayHelper.TrueForAll(values, x => x > 0), Is.True);
            Assert.That(ArrayHelper.GetOrDefault(values, -1), Is.Zero);
            Assert.That(ArrayHelper.GetOrDefault(values, 99), Is.Zero);

            CollectionAssert.AreEqual(new[] { 3, 2 }, ArrayHelper.FindAll(values, x => x > 1));
            CollectionAssert.AreEqual(new[] { "3", "1", "2", "1" }, ArrayHelper.ConvertAll(values, x => x.ToString()));
            CollectionAssert.AreEqual(new[] { 1, 2 }, ArrayHelper.GetRange(values, 1, 2));

            int sum = 0;
            int indexed = 0;
            ArrayHelper.ForEach(values, x => sum += x);
            ArrayHelper.ForEach(values, (x, i) => indexed += x * i);
            Assert.That(sum, Is.EqualTo(7));
            Assert.That(indexed, Is.EqualTo(8));

            ArrayHelper.Reverse(values, 1, 2);
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 1 }, values);
            ArrayHelper.Reverse(values);
            CollectionAssert.AreEqual(new[] { 1, 1, 2, 3 }, values);
            ArrayHelper.Sort(values, (a, b) => b.CompareTo(a));
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 1 }, values);
            ArrayHelper.Sort(values, Comparer<int>.Default);
            Assert.That(ArrayHelper.BinarySearch(values, 2), Is.EqualTo(2));
            Assert.That(ArrayHelper.BinarySearch(values, 3, Comparer<int>.Default), Is.EqualTo(3));
        }

        [Test]
        public void ListLinkedListHashSetAndDictionaryHelpers_CoverAllOverloads()
        {
            var list = new List<int> { 9 };
            list.Assign(3, 4);
            CollectionAssert.AreEqual(new[] { 4, 4, 4 }, list);
            int created = 0;
            list.Assign(2, () => ++created);
            list.Append(2, 7);
            list.Append(2, () => ++created);
            CollectionAssert.AreEqual(new[] { 1, 2, 7, 7, 3, 4 }, list);
            int indexed = 0;
            ListHelper.ForEach(list, (x, i) => indexed += x * i);
            Assert.That(indexed, Is.EqualTo(69));

            var linked = new LinkedList<int>(new[] { 2, 3 });
            int linkedSum = 0;
            int linkedIndexed = 0;
            linked.ForEach(x => linkedSum += x);
            linked.ForEach((x, i) => linkedIndexed += x * i);
            Assert.That((linkedSum, linkedIndexed), Is.EqualTo((5, 3)));

            var set = new HashSet<int> { 1, 2, 3 };
            int setSum = 0;
            set.ForEach(x => setSum += x);
            Assert.That(setSum, Is.EqualTo(6));

            var dictionary = new Dictionary<string, Box>();
            Box first = dictionary.GetOrAdd("new");
            Assert.That(dictionary.GetOrAdd("new"), Is.SameAs(first));
            Assert.That(dictionary.GetOrAdd("value", new Box { Value = 2 }).Value, Is.EqualTo(2));
            int calls = 0;
            Assert.That(dictionary.GetOrAdd("factory", () => { calls++; return new Box { Value = 3 }; }).Value, Is.EqualTo(3));
            dictionary.GetOrAdd("factory", () => { calls++; return new Box(); });
            Assert.That(calls, Is.EqualTo(1));

            int pairCount = 0, keyCount = 0, valueCount = 0, twoArgCount = 0;
            dictionary.ForEach(_ => pairCount++);
            dictionary.ForEach((_, __) => twoArgCount++);
            dictionary.Keys.ForEach(_ => keyCount++);
            dictionary.Values.ForEach(_ => valueCount++);
            Assert.That(new[] { pairCount, keyCount, valueCount, twoArgCount }, Is.All.EqualTo(3));
        }

        [Test]
        public void ListAppend_UsesGeometricCapacityGrowth()
        {
            var list = new List<int>();
            int capacityChanges = 0;
            int previousCapacity = list.Capacity;
            for (int i = 0; i < 4096; ++i)
            {
                list.Append(1, i);
                if (list.Capacity != previousCapacity)
                {
                    ++capacityChanges;
                    previousCapacity = list.Capacity;
                }
            }
            Assert.That(capacityChanges, Is.LessThanOrEqualTo(12));
            Assert.That(list.Count, Is.EqualTo(4096));
        }

        [Test]
        public void ListAppend_NegativeCount_PreservesPreviousNoOpBehavior()
        {
            var list = new List<int> { 1, 2 };
            int creatorCalls = 0;

            list.Append(-1, 3);
            list.Append(-1, () =>
            {
                ++creatorCalls;
                return 4;
            });

            Assert.That(list, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(creatorCalls, Is.Zero);
        }

        [Test]
        public void TextParser_DefaultReserve_DoesNotOverAllocateLongSingleRows()
        {
            string longValue = new string('x', 32_000);
            Assert.That(TextParser.TryParse<LongRow>($"1,{longValue}", out var rows, out string reason), Is.True, reason);
            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows.Capacity, Is.LessThanOrEqualTo(16));
        }

        [Test]
        public void TextParser_DefaultReserve_DoesNotCountQuotedNewlinesAsRows()
        {
            string multilineValue = string.Join("\n", Enumerable.Repeat("x", 3_000));
            Assert.That(TextParser.TryParse<LongRow>($"1,\"{multilineValue}\"", out var rows, out string reason), Is.True, reason);
            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Value, Is.EqualTo(multilineValue));
            Assert.That(rows.Capacity, Is.LessThanOrEqualTo(16));
        }

        [Test]
        public void TextParser_DefaultReserve_PreallocatesLargeTables()
        {
            const int rowCount = 300;
            string row = $"1,{new string('x', 48)}";
            string table = string.Join("\n", Enumerable.Repeat(row, rowCount));

            Assert.That(TextParser.TryParse<LongRow>(table, out var rows, out string reason), Is.True, reason);
            Assert.That(rows, Has.Count.EqualTo(rowCount));
            Assert.That(rows.Capacity, Is.GreaterThanOrEqualTo(rowCount));
            Assert.That(rows.Capacity, Is.LessThan(512));
        }

        public sealed class LongRow : ITryParsable
        {
            public int Id;
            public string Value;
            public bool TryParse(ref LineReader reader) => reader.TryRead(out Id) && reader.TryRead(out Value);
        }

        private sealed class Box { public int Value; }
    }
}
