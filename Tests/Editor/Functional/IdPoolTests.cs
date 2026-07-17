using System;
using NUnit.Framework;

namespace FerryKit.Core.Tests.Functional
{
    public class IdPoolTests
    {
        [Test]
        public void CircularPool_WrapsAndValidatesRange()
        {
            var pool = new CircularIdPool<int, IntOp>(2, 4);
            Assert.That(new[] { pool.NextId(), pool.NextId(), pool.NextId(), pool.NextId() }, Is.EqualTo(new[] { 2, 3, 4, 2 }));
            pool.ReleaseId(3);
            Assert.Throws<ArgumentException>(() => new CircularIdPool<int, IntOp>(-1, 2));
            Assert.Throws<ArgumentException>(() => new CircularIdPool<int, IntOp>(2, 2));
        }

        [Test]
        public void RecyclablePool_ReusesReleasedIdsAndRejectsInvalidOperations()
        {
            var pool = new RecyclableIdPool<int, IntOp>(5, 6);
            Assert.That(pool.NextId(), Is.EqualTo(5));
            Assert.That(pool.NextId(), Is.EqualTo(6));
            Assert.Throws<InvalidOperationException>(() => pool.NextId());
            pool.ReleaseId(5);
            Assert.That(pool.NextId(), Is.EqualTo(5));
            pool.ReleaseId(5);
            Assert.Throws<InvalidOperationException>(() => pool.ReleaseId(5));
            Assert.Throws<ArgumentException>(() => new RecyclableIdPool<int, IntOp>(-1, 2));
        }

        [Test]
        public void BitArrayPool_ReusesBoundsExhaustsAndProtectsRepresentableRange()
        {
            var pool = new BitArrayIdPool<int, IntOp>(10, 2);
            Assert.That(pool.NextId(), Is.EqualTo(10));
            Assert.That(pool.NextId(), Is.EqualTo(11));
            Assert.Throws<InvalidOperationException>(() => pool.NextId());
            pool.ReleaseId(10);
            Assert.That(pool.NextId(), Is.EqualTo(10));
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.ReleaseId(9));
            pool.ReleaseId(10);
            Assert.Throws<InvalidOperationException>(() => pool.ReleaseId(10));

            var lastInt = new BitArrayIdPool<int, IntOp>(int.MaxValue, 1);
            Assert.That(lastInt.NextId(), Is.EqualTo(int.MaxValue));
            var lastUInts = new BitArrayIdPool<uint, UIntOp>(uint.MaxValue - 1, 2);
            Assert.That(lastUInts.NextId(), Is.EqualTo(uint.MaxValue - 1));
            Assert.That(lastUInts.NextId(), Is.EqualTo(uint.MaxValue));
            Assert.Throws<ArgumentException>(() => new BitArrayIdPool<int, IntOp>(int.MaxValue, 2));
            Assert.Throws<ArgumentException>(() => new BitArrayIdPool<uint, UIntOp>(uint.MaxValue - 1, 3));
            Assert.Throws<ArgumentException>(() => new BitArrayIdPool<int, IntOp>(0, 0));
        }
    }
}
