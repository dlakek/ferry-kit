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

            if (_op.LTE(maxId, startId))
                throw new ArgumentException("maxId must be greater than startId.");

            _startId = startId;
            _maxId = maxId;
            _curId = _op.Sub(startId, _op.One);
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

        public RecyclableIdPool() : this(default(TOp).Zero, default(TOp).Max) { }
        public RecyclableIdPool(T startId) : this(startId, default(TOp).Max) { }
        public RecyclableIdPool(T startId, T maxId)
        {
            if (_op.LT(startId, _op.Zero))
                throw new ArgumentException("startId must be non-negative.");

            if (_op.LTE(maxId, startId))
                throw new ArgumentException("maxId must be greater than startId.");

            _maxId = maxId;
            _curId = _op.Sub(startId, _op.One);
        }

        public T NextId()
        {
            if (!_usableIds.TryPop(out T id))
            {
                if (_op.GTE(_curId, _maxId))
                    throw new InvalidOperationException($"no more IDs available. max: {_maxId}");

                id = _op.Inc(ref _curId);
            }
            _usingIds.Add(id);
            return id;
        }

        public void ReleaseId(T id)
        {
            if (!_usingIds.Remove(id))
                throw new InvalidOperationException($"ID {id} is not currently in use or already released.");

            _usableIds.Push(id);
        }
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
        private const int DEFAULT_CAPACITY = 1024;

        private readonly TOp _op = default;
        private readonly T _startId;
        private readonly int _capacity;
        private readonly int _limit;
        private readonly BitArray _idUsage;

        private int _curIdx = 0;

        public BitArrayIdPool() : this(default(TOp).Zero, DEFAULT_CAPACITY) { }
        public BitArrayIdPool(T startId) : this(startId, DEFAULT_CAPACITY) { }
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
            _limit = capacity - 1;
            _idUsage = new(capacity);
        }

        public T NextId()
        {
            // The next free bit is O(1) when found immediately and O(capacity) in the worst case.
            int idx = _curIdx;
            for (int i = 0; i < _capacity; ++i)
            {
                if (!_idUsage.Get(idx))
                {
                    _idUsage.Set(idx, true);
                    _curIdx = idx < _limit ? idx + 1 : 0;
                    return _op.Add(_startId, _op.FromInt(idx));
                }
                if (++idx > _limit)
                {
                    idx = 0;
                }
            }
            throw new InvalidOperationException("pool exhausted: no bits available.");
        }

        public void ReleaseId(T id)
        {
            int idx = _op.ToInt(_op.Sub(id, _startId));
            if (idx < 0 || idx >= _capacity)
                throw new ArgumentOutOfRangeException(nameof(id), $"ID {id} is out of bounds. startId: {_startId}, capacity: {_capacity}");

            if (!_idUsage.Get(idx))
                throw new InvalidOperationException($"ID {id} is already released.");

            _idUsage.Set(idx, false);
        }
    }
}
