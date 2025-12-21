using System;
using System.Collections;
using System.Collections.Generic;

namespace OptimizedUtils
{
    public interface IIdPool<T>
    {
        T NextId();
        void ReleaseId(T id); // CircularPool에서는 무시됨
    }

    /// <summary>
    /// 범위 내에서 ID를 순환하며 할당하는 ID 풀 구현체.
    /// 범위 끝에 도달하면 다시 시작ID부터 할당을 시작함.
    /// 앞서 할당한 ID의 생명주기가 짧음이 보장되어 시작ID로 돌아가도 충돌이 없을 것으로 예상되는 경우에 적합.
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

        public T NextId()
        {
            if (_op.GTE(_curId, _maxId))
            {
                _curId = _startId;
            }
            else
            {
                _op.Inc(ref _curId);
            }
            return _curId;
        }

        public void ReleaseId(T id) { } // 구현 불필요
    }

    /// <summary>
    /// 범위 내에서 ID를 할당하고, 사용 완료한 ID를 돌려받아 재활용 가능한 ID 풀 구현체.
    /// 범위 내의 모든 ID가 사용 중일 때는 더 이상 할당할 수 없음.
    /// 할당된 ID의 생명주기가 길거나 불확실한 경우에 적합.
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
    /// RecyclableIdPool과 동일한 역할이지만, BitArray를 사용하여 ID 할당 상태를 추적하는 ID 풀 구현체.
    /// RecyclableIdPool보다 메모리 사용량을 최대한 최적화하려는 경우에 적합.
    /// 할당된 상태로 유지될 ID 양이 적당히 적을 것으로 예상되는 경우에만 사용하는 것이 좋음.
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
            // 기본적으로 O(1)에 수렴하며 최악의 경우 O(n)의 시간 복잡도를 가짐
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
