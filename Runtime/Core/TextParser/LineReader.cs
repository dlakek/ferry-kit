using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    /// <summary>
    /// A line reader that uses <see cref="DefaultPolicy"/>.
    /// </summary>
    public ref struct LineReader
    {
        private LineReader<DefaultPolicy> _reader;

        public readonly bool HasRemaining => _reader.HasRemaining;
        public readonly ReadOnlySpan<char> Line => _reader.Line;
        public readonly ReadOnlySpan<char> Current => _reader.Current;

        [MethodImpl(Opt.Inline)] public void SkipRemaining() => _reader.SkipRemaining();

        [MethodImpl(Opt.Inline)] public T Read<T>() => _reader.Read<T>();
        [MethodImpl(Opt.Inline)] public T[] ReadArray<T>() => _reader.ReadArray<T>();
        [MethodImpl(Opt.Inline)] public List<T> ReadList<T>() => _reader.ReadList<T>();
        [MethodImpl(Opt.Inline)] public HashSet<T> ReadHashSet<T>() => _reader.ReadHashSet<T>();
        [MethodImpl(Opt.Inline)] public Dictionary<K, V> ReadDictionary<K, V>() => _reader.ReadDictionary<K, V>();
        [MethodImpl(Opt.Inline)] public (K, V) ReadPair<K, V>() => _reader.ReadPair<K, V>();
        [MethodImpl(Opt.Inline)] public ReadOnlySpan<char> ReadNext() => _reader.ReadNext();

        [MethodImpl(Opt.Inline)] public bool TryRead<T>(out T result) => _reader.TryRead(out result);
        [MethodImpl(Opt.Inline)] public bool TryRead<T>(out T[] result) => _reader.TryRead(out result);
        [MethodImpl(Opt.Inline)] public bool TryRead<T>(out List<T> result) => _reader.TryRead(out result);
        [MethodImpl(Opt.Inline)] public bool TryRead<T>(out HashSet<T> result) => _reader.TryRead(out result);
        [MethodImpl(Opt.Inline)] public bool TryRead<K, V>(out Dictionary<K, V> result) => _reader.TryRead(out result);
        [MethodImpl(Opt.Inline)] public bool TryRead<K, V>(out (K, V) result) => _reader.TryRead(out result);
        [MethodImpl(Opt.Inline)] public bool TryReadNext(out ReadOnlySpan<char> result) => _reader.TryReadNext(out result);

        [MethodImpl(Opt.Inline)]
        public LineReader(ReadOnlySpan<char> line)
        {
            _reader = new(line, default);
        }
    }

    /// <summary>
    /// Splits one logical row into column cells according to the supplied policy.
    /// Outer quotes are removed from the current cell; typed string reads also unescape CSV doubled quotes.
    /// </summary>
    public ref struct LineReader<P> where P : struct, IParsePolicy
    {
        private readonly ReadOnlySpan<char> _line;
        private readonly P _policy;

        private ReadOnlySpan<char> _current;
        private int _nextIndex;
        private bool _isExhausted;
        private bool _currentWasQuoted;

        public readonly bool HasRemaining => !_isExhausted;
        public readonly ReadOnlySpan<char> Line => _line;
        public readonly ReadOnlySpan<char> Current => _current;
        public readonly P Policy => _policy;

        [MethodImpl(Opt.Inline)] public void SkipRemaining() => _isExhausted = true;

        [MethodImpl(Opt.Inline)] public T[] ReadArray<T>() => ReadNext().ToArray<T, P>(_policy);
        [MethodImpl(Opt.Inline)] public List<T> ReadList<T>() => ReadNext().ToList<T, P>(_policy);
        [MethodImpl(Opt.Inline)] public HashSet<T> ReadHashSet<T>() => ReadNext().ToHashSet<T, P>(_policy);
        [MethodImpl(Opt.Inline)] public Dictionary<K, V> ReadDictionary<K, V>() => ReadNext().ToDictionary<K, V, P>(_policy);
        [MethodImpl(Opt.Inline)] public (K, V) ReadPair<K, V>() => ReadNext().ToPair<K, V, P>(_policy);
        [MethodImpl(Opt.Inline)] public ReadOnlySpan<char> ReadNext() => TryReadNext(out var result) ? result : ThrowNoColumns();

        [MethodImpl(Opt.Inline)]
        public LineReader(ReadOnlySpan<char> line, P policy)
        {
            _line = line;
            _policy = policy;
            _current = default;
            _nextIndex = 0;
            _isExhausted = false;
            _currentWasQuoted = false;
        }

        [MethodImpl(Opt.Inline)]
        public T Read<T>()
        {
            var cell = ReadNext();
            if (typeof(T) == typeof(string) && _currentWasQuoted && cell.TryGetUnescapeCsvQuotes(out T value))
                return value;

            return cell.To<T, P>(_policy);
        }

        [MethodImpl(Opt.Inline)]
        public bool TryRead<T>(out T result)
        {
            if (TryReadNext(out var cell))
            {
                if (typeof(T) == typeof(string) && _currentWasQuoted && cell.TryGetUnescapeCsvQuotes(out result))
                    return true;

                return cell.TryTo(out result, _policy);
            }
            result = default;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        public bool TryRead<T>(out T[] result)
        {
            if (TryReadNext(out var cell))
                return cell.TryToArray(out result, _policy);

            result = null;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        public bool TryRead<T>(out List<T> result)
        {
            if (TryReadNext(out var cell))
                return cell.TryToList(out result, _policy);

            result = null;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        public bool TryRead<T>(out HashSet<T> result)
        {
            if (TryReadNext(out var cell))
                return cell.TryToHashSet(out result, _policy);

            result = null;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        public bool TryRead<K, V>(out Dictionary<K, V> result)
        {
            if (TryReadNext(out var cell))
                return cell.TryToDictionary(out result, _policy);

            result = null;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        public bool TryRead<K, V>(out (K, V) result)
        {
            if (TryReadNext(out var cell))
                return cell.TryToPair(out result, _policy);

            result = default;
            return false;
        }

        public bool TryReadNext(out ReadOnlySpan<char> result)
        {
            if (_isExhausted)
            {
                result = default;
                return false;
            }
            var remaining = _line[_nextIndex..];
            int separatorIndex = remaining.IndexOfUnquoted(_policy.ColSep, _policy.EscapeMode);
            ReadOnlySpan<char> cell;
            if (separatorIndex == -1)
            {
                _isExhausted = true;
                cell = remaining;
            }
            else
            {
                _nextIndex += separatorIndex + 1;
                cell = remaining[..separatorIndex];
            }
            _currentWasQuoted = cell.IsInQuotes();
            _current = _currentWasQuoted ? cell[1..^1] : cell;
            result = _current;
            return true;
        }

        [MethodImpl(Opt.NoInline)]
        private readonly ReadOnlySpan<char> ThrowNoColumns() => throw new InvalidOperationException("no columns remain in the current line.");
    }
}
