using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public interface IIdPool<T>
    {
        T NextId();
        void ReleaseId(T id); // Ignored in CircularPool
    }

    /// <summary>
    /// Allocates IDs in a circular fashion within an inclusive range.
    /// After returning the maximum ID, the next allocation returns the starting ID.
    /// Use this only when wrapping cannot collide with IDs that are still in use.
    /// </summary>
    public class CircularIdPool<T, TOp> : IIdPool<T>
        where T : struct
        where TOp : struct, INumOp<T>
    {
        private readonly TOp _op = default;
        private readonly T _startId;
        private readonly T _maxId;

        private T _curId;

        public CircularIdPool() : this(default(TOp).Zero, default(TOp).Max) { }
        public CircularIdPool(T startId) : this(startId, default(TOp).Max) { }
        public CircularIdPool(T startId, T maxId)
        {
            if (_op.LT(startId, _op.Zero))
                throw new ArgumentException("startId must be non-negative.");

            if (_op.LT(maxId, startId))
                throw new ArgumentException("maxId must be greater than or equal to startId.");

            _startId = startId;
            _maxId = maxId;
            _curId = maxId;
        }

        [MethodImpl(Opt.Inline)]
        public T NextId() => _op.GTE(_curId, _maxId)
            ? _curId = _startId
            : _op.Inc(ref _curId);

        public void ReleaseId(T id) { } // No need to implement
    }

    /// <summary>
    /// Allocates IDs within an inclusive range and recycles IDs after <see cref="ReleaseId(T)"/>.
    /// Allocation fails while every ID in the range is in use.
    /// This is suitable when allocated IDs have long or unpredictable lifetimes.
    /// </summary>
    public class RecyclableIdPool<T, TOp> : IIdPool<T>
        where T : struct
        where TOp : struct, INumOp<T>
    {
        private readonly HashSet<T> _usingIds = new();
        private readonly Stack<T> _usableIds = new();
        private readonly TOp _op = default;
        private readonly T _maxId;

        private T _curId;
        private bool _hasAllocated;

        public RecyclableIdPool() : this(default(TOp).Zero, default(TOp).Max) { }
        public RecyclableIdPool(T startId) : this(startId, default(TOp).Max) { }
        public RecyclableIdPool(T startId, T maxId)
        {
            if (_op.LT(startId, _op.Zero))
                throw new ArgumentException("startId must be non-negative.");

            if (_op.LT(maxId, startId))
                throw new ArgumentException("maxId must be greater than or equal to startId.");

            _maxId = maxId;
            _curId = startId;
        }

        [MethodImpl(Opt.Inline)]
        public T NextId()
        {
            if (!_usableIds.TryPop(out T id))
            {
                if (_hasAllocated)
                {
                    if (_op.GTE(_curId, _maxId))
                        ThrowExhausted(_maxId);

                    id = _op.Inc(ref _curId);
                }
                else
                {
                    id = _curId;
                    _hasAllocated = true;
                }
            }
            _usingIds.Add(id);
            return id;
        }

        [MethodImpl(Opt.Inline)]
        public void ReleaseId(T id)
        {
            if (!_usingIds.Remove(id))
                ThrowInvalidRelease(id);

            _usableIds.Push(id);
        }

        [MethodImpl(Opt.NoInline)]
        private static void ThrowExhausted(T maxId) => throw new InvalidOperationException($"no more IDs available. max: {maxId}");

        [MethodImpl(Opt.NoInline)]
        private static void ThrowInvalidRelease(T id) => throw new InvalidOperationException($"ID {id} is not currently in use or already released.");
    }

    /// <summary>
    /// Tracks a fixed, contiguous ID range with a <see cref="BitArray"/>.
    /// This avoids per-entry hash and stack storage and is most suitable for bounded ranges where
    /// allocating one bit for every possible ID is acceptable.
    /// </summary>
    public class BitArrayIdPool<T, TOp> : IIdPool<T>
        where T : struct
        where TOp : struct, INumOp<T>
    {
        private const int _defaultCapacity = 1024;

        private readonly TOp _op = default;
        private readonly T _startId;
        private readonly int _capacity;
        private readonly BitArray _idUsage;

        private int _curIdx;
        private int _availableCount;

        public BitArrayIdPool() : this(default(TOp).Zero, _defaultCapacity) { }
        public BitArrayIdPool(T startId) : this(startId, _defaultCapacity) { }
        public BitArrayIdPool(T startId, int capacity)
        {
            if (_op.LT(startId, _op.Zero))
                throw new ArgumentException("startId must be non-negative.");

            if (capacity <= 0)
                throw new ArgumentException("capacity must be greater than 0.");

            if (_op.LT(_op.Sub(_op.Max, startId), _op.FromInt(capacity - 1)))
                throw new ArgumentException("capacity exceeds the available ID range from startId.");

            _startId = startId;
            _capacity = capacity;
            _idUsage = new(capacity);
            _availableCount = capacity;
        }

        public T NextId()
        {
            if (_availableCount == 0)
                return ThrowExhausted();

            // A free bit exists. Search from the cursor, preserving circular allocation order.
            int idx = _curIdx;
            while (_idUsage.Get(idx))
            {
                if (++idx == _capacity)
                    idx = 0;
            }
            _idUsage.Set(idx, true);
            --_availableCount;
            _curIdx = idx + 1 < _capacity ? idx + 1 : 0;
            return _op.Add(_startId, _op.FromInt(idx));
        }

        [MethodImpl(Opt.Inline)]
        public void ReleaseId(T id)
        {
            if (_op.LT(id, _startId))
                ThrowOutOfBounds(id, _startId, _capacity);

            T offset = _op.Sub(id, _startId);
            if (_op.GTE(offset, _op.FromInt(_capacity)))
                ThrowOutOfBounds(id, _startId, _capacity);

            int idx = _op.ToInt(offset);
            if (!_idUsage.Get(idx))
                ThrowAlreadyReleased(id);

            _idUsage.Set(idx, false);
            ++_availableCount;
        }

        [MethodImpl(Opt.NoInline)]
        private static T ThrowExhausted() => throw new InvalidOperationException("pool exhausted: no bits available.");

        [MethodImpl(Opt.NoInline)]
        private static void ThrowOutOfBounds(T id, T startId, int capacity)
            => throw new ArgumentOutOfRangeException(nameof(id), $"ID {id} is out of bounds. startId: {startId}, capacity: {capacity}");

        [MethodImpl(Opt.NoInline)]
        private static void ThrowAlreadyReleased(T id) => throw new InvalidOperationException($"ID {id} is already released.");
    }
}
