using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FerryKit.Core.Tests.Functional
{
    public class ArrayAndCollectionHelperTests
    {
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

        private sealed class Box { public int Value; }
    }
}
