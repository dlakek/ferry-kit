using System;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    /// <summary>
    /// Iterates logical lines and ignores newline characters inside quoted fields according to the policy.
    /// </summary>
    public ref struct LineSplitter<P> where P : struct, IParsePolicy
    {
        private readonly P _policy;

        private ReadOnlySpan<char> _remaining;
        private ReadOnlySpan<char> _current;

        public readonly ReadOnlySpan<char> Current
        {
            [MethodImpl(Opt.Inline)]
            get => _current;
        }

        [MethodImpl(Opt.Inline)] public readonly LineSplitter<P> GetEnumerator() => this;

        [MethodImpl(Opt.Inline)]
        public LineSplitter(ReadOnlySpan<char> text, P policy)
        {
            _policy = policy;
            _remaining = text;
            _current = default;
        }

        [MethodImpl(Opt.Inline)]
        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
            {
                _current = ReadOnlySpan<char>.Empty;
                return false;
            }
            int separatorIndex = _remaining.IndexOfUnquoted('\n', _policy.EscapeMode);
            if (separatorIndex == -1)
            {
                _current = _remaining.TrimEndOne('\r');
                _remaining = ReadOnlySpan<char>.Empty;
            }
            else
            {
                _current = _remaining[..separatorIndex].TrimEndOne('\r');
                _remaining = _remaining[(separatorIndex + 1)..];
            }
            return true;
        }
    }
}
