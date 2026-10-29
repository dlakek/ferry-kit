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
            Assert.Throws<ArgumentException>(() => new CircularIdPool<int, IntOp>(3, 2));
        }

        [Test]
        public void InclusivePools_SupportZeroAndSingleIdRangesForEveryOperator()
        {
            VerifyInclusivePools<int, IntOp>();
            VerifyInclusivePools<uint, UIntOp>();
            VerifyInclusivePools<long, LongOp>();
            VerifyInclusivePools<ulong, ULongOp>();
        }

        private static void VerifyInclusivePools<T, TOp>() where T : struct where TOp : struct, INumOp<T>
        {
            TOp op = default;
            var circular = new CircularIdPool<T, TOp>(op.Zero, op.One);
            Assert.That(circular.NextId(), Is.EqualTo(op.Zero));
            Assert.That(circular.NextId(), Is.EqualTo(op.One));
            Assert.That(circular.NextId(), Is.EqualTo(op.Zero));
            var recyclable = new RecyclableIdPool<T, TOp>(op.Zero, op.One);
            Assert.That(recyclable.NextId(), Is.EqualTo(op.Zero));
            Assert.That(recyclable.NextId(), Is.EqualTo(op.One));
            Assert.Throws<InvalidOperationException>(() => recyclable.NextId());
            recyclable.ReleaseId(op.Zero);
            Assert.That(recyclable.NextId(), Is.EqualTo(op.Zero));

            foreach (T id in new[] { op.Zero, op.Max })
            {
                var singleCircular = new CircularIdPool<T, TOp>(id, id);
                Assert.That(singleCircular.NextId(), Is.EqualTo(id));
                Assert.That(singleCircular.NextId(), Is.EqualTo(id));
                var singleRecyclable = new RecyclableIdPool<T, TOp>(id, id);
                Assert.That(singleRecyclable.NextId(), Is.EqualTo(id));
                Assert.Throws<InvalidOperationException>(() => singleRecyclable.NextId());
                singleRecyclable.ReleaseId(id);
                Assert.That(singleRecyclable.NextId(), Is.EqualTo(id));
            }
        }

        [Test]
        public void BitArrayPool_RejectsExtremeIdsBeforeSubtractingOrNarrowing()
        {
            VerifyInvalidBitArrayIds<int, IntOp>();
            VerifyInvalidBitArrayIds<uint, UIntOp>();
            VerifyInvalidBitArrayIds<long, LongOp>();
            VerifyInvalidBitArrayIds<ulong, ULongOp>();
        }

        private static void VerifyInvalidBitArrayIds<T, TOp>() where T : struct where TOp : struct, INumOp<T>
        {
            TOp op = default;
            var pool = new BitArrayIdPool<T, TOp>(op.One, 2);
            T id = pool.NextId();
            foreach (T invalid in new[] { op.Min, op.Max })
            {
                var error = Assert.Throws<ArgumentOutOfRangeException>(() => pool.ReleaseId(invalid));
                Assert.That(error.ParamName, Is.EqualTo("id"));
            }
            pool.ReleaseId(id);
            Assert.Throws<InvalidOperationException>(() => pool.ReleaseId(id));
        }

        [Test]
        public void BitArrayPool_TracksAvailabilityAcrossExhaustionAndRelease()
        {
            const int capacity = 257;
            var pool = new BitArrayIdPool<int, IntOp>(0, capacity);
            for (int i = 0; i < capacity; ++i)
                Assert.That(pool.NextId(), Is.EqualTo(i));
            Assert.Throws<InvalidOperationException>(() => pool.NextId());
            pool.ReleaseId(256);
            pool.ReleaseId(0);
            Assert.That(pool.NextId(), Is.Zero);
            Assert.That(pool.NextId(), Is.EqualTo(256));
            Assert.Throws<InvalidOperationException>(() => pool.NextId());
        }

        [Test]
        public void BitArrayPool_MatchesCircularReferenceAcrossMixedOperations()
        {
            const int capacity = 17;
            var pool = new BitArrayIdPool<int, IntOp>(0, capacity);
            var used = new bool[capacity];
            var random = new Random(42);
            int cursor = 0;
            for (int operation = 0; operation < 5_000; ++operation)
            {
                if (random.Next(2) == 0)
                {
                    int expected = -1;
                    for (int step = 0; step < capacity; ++step)
                    {
                        int candidate = (cursor + step) % capacity;
                        if (!used[candidate])
                        {
                            expected = candidate;
                            break;
                        }
                    }
                    if (expected == -1)
                        Assert.Throws<InvalidOperationException>(() => pool.NextId());
                    else
                    {
                        Assert.That(pool.NextId(), Is.EqualTo(expected));
                        used[expected] = true;
                        cursor = (expected + 1) % capacity;
                    }
                }
                else
                {
                    int id = random.Next(-1, capacity + 1);
                    if (id < 0 || id >= capacity)
                        Assert.Throws<ArgumentOutOfRangeException>(() => pool.ReleaseId(id));
                    else if (!used[id])
                        Assert.Throws<InvalidOperationException>(() => pool.ReleaseId(id));
                    else
                    {
                        pool.ReleaseId(id);
                        used[id] = false;
                    }
                }
            }
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
