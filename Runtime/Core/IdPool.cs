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
    /// An ID pool implementation that allocates IDs in a circular fashion within a range.
    /// When the range ends, allocation begins again from the starting ID.
    /// This is suitable when previously allocated IDs are guaranteed to have short lifetimes and returning to the starting ID is expected to be free of conflicts.
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
    /// An ID pool implementation that allocates IDs within a scope and returns used IDs, allowing them to be recycled.
    /// No more IDs can be allocated when all IDs within the scope are in use.
    /// Suitable for cases where the lifetime of allocated IDs is long or uncertain.
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
    /// An ID pool implementation that serves the same purpose as RecyclableIdPool, but uses a BitArray to track ID allocation status.
    /// Suitable for those seeking to optimize memory usage as much as possible compared to RecyclableIdPool.
    /// Recommended only when the number of IDs expected to remain allocated is expected to be relatively small.
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

            _startId = startId;
            _capacity = capacity;
            _limit = capacity - 1;
            _idUsage = new(capacity);
        }

        public T NextId()
        {
            // Basically, it converges to O(1) and has a time complexity of O(n) in the worst case.
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
