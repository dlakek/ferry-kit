namespace FerryKit.Core
{
    /// <summary>
    /// Defines an object parsed by the throwing <see cref="TextParser"/> APIs.
    /// Implementations should throw when a required value is invalid or missing.
    /// </summary>
    public interface IParsable
    {
        void Parse(ref LineReader reader);
    }

    /// <summary>
    /// Defines an object parsed with an explicitly supplied policy.
    /// Implementations should throw when a required value is invalid or missing.
    /// </summary>
    public interface IParsable<P> where P : struct, IParsePolicy
    {
        void Parse(ref LineReader<P> reader);
    }

    /// <summary>
    /// Defines an object that reports invalid or missing required values by returning <see langword="false"/>.
    /// </summary>
    public interface ITryParsable
    {
        bool TryParse(ref LineReader reader);
    }

    /// <summary>
    /// Defines an object that reports invalid or missing required values by returning <see langword="false"/>
    /// while using an explicitly supplied policy.
    /// </summary>
    public interface ITryParsable<P> where P : struct, IParsePolicy
    {
        bool TryParse(ref LineReader<P> reader);
    }

    /// <summary>
    /// Combines the default-policy parsing and try-parsing contracts.
    /// </summary>
    public interface IParsableEx : IParsable, ITryParsable
    {
    }

    /// <summary>
    /// Combines the explicit-policy parsing and try-parsing contracts.
    /// </summary>
    public interface IParsableEx<P> : IParsable<P>, ITryParsable<P> where P : struct, IParsePolicy
    {
    }
}
