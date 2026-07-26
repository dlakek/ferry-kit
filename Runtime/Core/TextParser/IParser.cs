using System;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    /// <summary>
    /// Provides value parsing through a struct-constrained default-policy path.
    /// </summary>
    public interface IParser<T>
    {
        T Parse(ReadOnlySpan<char> span);
        bool TryParse(ReadOnlySpan<char> span, out T result);
    }

    /// <summary>
    /// Provides value parsing through a struct-constrained explicit-policy path.
    /// </summary>
    public interface IParser<T, P> where P : struct, IParsePolicy
    {
        T Parse(ReadOnlySpan<char> span, P policy);
        bool TryParse(ReadOnlySpan<char> span, P policy, out T result);
    }

    /// <summary>
    /// Uses <see cref="ParseHelper"/> as the default-policy parser for a supported value.
    /// </summary>
    public readonly struct Parser<T> : IParser<T>
    {
        [MethodImpl(Opt.Inline)] public T Parse(ReadOnlySpan<char> span) => span.To<T>();
        [MethodImpl(Opt.Inline)] public bool TryParse(ReadOnlySpan<char> span, out T result) => span.TryTo(out result);
    }

    /// <summary>
    /// Uses <see cref="ParseHelper"/> as the explicit-policy parser for a supported value.
    /// </summary>
    public readonly struct Parser<T, P> : IParser<T, P> where P : struct, IParsePolicy
    {
        [MethodImpl(Opt.Inline)] public T Parse(ReadOnlySpan<char> span, P policy) => span.To<T, P>(policy);
        [MethodImpl(Opt.Inline)] public bool TryParse(ReadOnlySpan<char> span, P policy, out T result) => span.TryTo(out result, policy);
    }

    /// <summary>
    /// Adapts a default-policy parser to an explicit-policy parser, ignoring the policy argument.
    /// </summary>
    public readonly struct ParserAdapter<T, P, TParser> : IParser<T, P>
        where P : struct, IParsePolicy
        where TParser : struct, IParser<T>
    {
        private readonly TParser _parser;

        public ParserAdapter(TParser parser) => _parser = parser;
        [MethodImpl(Opt.Inline)] public T Parse(ReadOnlySpan<char> span, P policy) => _parser.Parse(span);
        [MethodImpl(Opt.Inline)] public bool TryParse(ReadOnlySpan<char> span, P policy, out T result) => _parser.TryParse(span, out result);
    }
}
