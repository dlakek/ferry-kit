namespace FerryKit.Core
{
    /// <summary>
    /// Defines separators and parsing behavior for struct-constrained parser paths.
    /// Using a struct policy avoids interface boxing in the generic APIs.
    /// </summary>
    public interface IParsePolicy
    {
        char ColSep { get; }
        char ArrSep { get; }
        char SetSep { get; }
        char DictSep { get; }
        char PairSep { get; }
        bool IgnoreCase { get; }
        QuoteEscapeMode EscapeMode { get; }
    }

    /// <summary>
    /// Stateless policy that exposes the built-in parsing defaults.
    /// </summary>
    public readonly struct DefaultPolicy : IParsePolicy
    {
        public char ColSep => ParsePolicyDefaults.ColumnSeparator;
        public char ArrSep => ParsePolicyDefaults.ArraySeparator;
        public char SetSep => ParsePolicyDefaults.SetSeparator;
        public char DictSep => ParsePolicyDefaults.DictionarySeparator;
        public char PairSep => ParsePolicyDefaults.PairSeparator;
        public bool IgnoreCase => ParsePolicyDefaults.IgnoreCase;
        public QuoteEscapeMode EscapeMode => ParsePolicyDefaults.EscapeMode;
    }

    /// <summary>
    /// Stores parsing settings selected at runtime.
    /// For settings fixed at compile time, implement a stateless <see cref="IParsePolicy"/> struct instead.
    /// </summary>
    public readonly struct CustomPolicy : IParsePolicy
    {
        /// <summary>
        /// A fully initialized custom policy using every built-in default.
        /// Under C# 9, <c>new CustomPolicy()</c> is zero-initialized instead of invoking the optional-parameter constructor.
        /// </summary>
        public static readonly CustomPolicy Default = new(colSep: ParsePolicyDefaults.ColumnSeparator);

        public char ColSep { get; }
        public char ArrSep { get; }
        public char SetSep { get; }
        public char DictSep { get; }
        public char PairSep { get; }
        public bool IgnoreCase { get; }
        public QuoteEscapeMode EscapeMode { get; }

        public CustomPolicy(
            char colSep = ParsePolicyDefaults.ColumnSeparator,
            char arrSep = ParsePolicyDefaults.ArraySeparator,
            char setSep = ParsePolicyDefaults.SetSeparator,
            char dictSep = ParsePolicyDefaults.DictionarySeparator,
            char pairSep = ParsePolicyDefaults.PairSeparator,
            bool ignoreCase = ParsePolicyDefaults.IgnoreCase,
            QuoteEscapeMode escapeMode = ParsePolicyDefaults.EscapeMode)
        {
            ColSep = colSep;
            ArrSep = arrSep;
            SetSep = setSep;
            DictSep = dictSep;
            PairSep = pairSep;
            IgnoreCase = ignoreCase;
            EscapeMode = escapeMode;
        }
    }

    /// <summary>
    /// Default values shared by the built-in parse policies.
    /// </summary>
    public static class ParsePolicyDefaults
    {
        public const char ColumnSeparator = ',';
        public const char ArraySeparator = ';';
        public const char SetSeparator = '|';
        public const char DictionarySeparator = '|';
        public const char PairSeparator = '=';
        public const bool IgnoreCase = false;
        public const QuoteEscapeMode EscapeMode = QuoteEscapeMode.Csv;
    }
}
