using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    /// <summary>
    /// A general-purpose parser primarily used for CSV parsing, but implemented to be able to parse other forms of text as well.
    /// </summary>
    public static class TextParser
    {
        private const int _defaultInitialCapacity = 4;
        private const int _capacitySampleLength = 4096;
        private const int _maximumEstimatedCapacity = 1024;

        public static List<T> Parse<T>(this string text, int reserveLine = 0, bool skipFirstLine = false)
            where T : IParsable, new()
        {
            if (string.IsNullOrWhiteSpace(text))
                return new();

            var splitter = new LineSplitter<DefaultPolicy>(text, default);
            var reader = new LineReader(default);
            var result = new List<T>(GetInitialCapacity(text, reserveLine, ParsePolicyDefaults.EscapeMode));
            if (skipFirstLine)
            {
                splitter.MoveNext();
            }
            var line = splitter.Current;
            try
            {
                while (splitter.MoveNext())
                {
                    line = splitter.Current;
                    if (line.IsWhiteSpace())
                        continue;

                    var data = new T();
                    reader = new(line);
                    data.Parse(ref reader);
                    if (reader.HasRemaining)
                        throw new FormatException("unexpected trailing column.");

                    result.Add(data);
                }
            }
            catch (Exception ex)
            {
                throw CreateFormatException(line, reader.Current, ex);
            }
            return result;
        }

        public static List<T> Parse<T, P>(this string text, P policy, int reserveLine = 0, bool skipFirstLine = false)
            where T : IParsable<P>, new()
            where P : struct, IParsePolicy
        {
            if (string.IsNullOrWhiteSpace(text))
                return new();

            var splitter = new LineSplitter<P>(text, policy);
            var reader = new LineReader<P>(default, policy);
            var result = new List<T>(GetInitialCapacity(text, reserveLine, policy.EscapeMode));
            if (skipFirstLine)
            {
                splitter.MoveNext();
            }
            var line = splitter.Current;
            try
            {
                while (splitter.MoveNext())
                {
                    line = splitter.Current;
                    if (line.IsWhiteSpace())
                        continue;

                    var data = new T();
                    reader = new(line, policy);
                    data.Parse(ref reader);
                    if (reader.HasRemaining)
                        throw new FormatException("unexpected trailing column.");

                    result.Add(data);
                }
            }
            catch (Exception ex)
            {
                throw CreateFormatException(line, reader.Current, ex);
            }
            return result;
        }

        public static bool TryParse<T>(this string text, out List<T> result, out string reason, int reserveLine = 0, bool skipFirstLine = false)
            where T : ITryParsable, new()
        {
            result = new();
            return text.TryParse(result, out reason, reserveLine, skipFirstLine);
        }

        public static bool TryParse<T>(this string text, List<T> result, out string reason, int reserveLine = 0, bool skipFirstLine = false, bool revertOnFailure = false)
            where T : ITryParsable, new()
        {
            if (!text.TryPrepare(result, reserveLine, revertOnFailure, ParsePolicyDefaults.EscapeMode, out int originalCount, out reason))
                return false;

            var splitter = new LineSplitter<DefaultPolicy>(text, default);
            if (skipFirstLine)
            {
                splitter.MoveNext();
            }
            if (revertOnFailure)
            {
                bool success = false;
                try
                {
                    success = splitter.TryParseLines(result, out reason);
                }
                finally
                {
                    result.CompleteTryParse(originalCount, success);
                }
                return success;
            }
            return splitter.TryParseLines(result, out reason);
        }

        public static bool TryParse<T, P>(this string text, P policy, out List<T> result, out string reason, int reserveLine = 0, bool skipFirstLine = false)
            where T : ITryParsable<P>, new()
            where P : struct, IParsePolicy
        {
            result = new();
            return text.TryParse(policy, result, out reason, reserveLine, skipFirstLine);
        }

        public static bool TryParse<T, P>(this string text, P policy, List<T> result, out string reason, int reserveLine = 0, bool skipFirstLine = false, bool revertOnFailure = false)
            where T : ITryParsable<P>, new()
            where P : struct, IParsePolicy
        {
            if (!text.TryPrepare(result, reserveLine, revertOnFailure, policy.EscapeMode, out int originalCount, out reason))
                return false;

            var splitter = new LineSplitter<P>(text, policy);
            if (skipFirstLine)
            {
                splitter.MoveNext();
            }
            if (revertOnFailure)
            {
                bool success = false;
                try
                {
                    success = splitter.TryParseLines(policy, result, out reason);
                }
                finally
                {
                    result.CompleteTryParse(originalCount, success);
                }
                return success;
            }
            return splitter.TryParseLines(policy, result, out reason);
        }

        private static bool TryParseLines<T>(this ref LineSplitter<DefaultPolicy> lineSplitter, List<T> result, out string reason)
            where T : ITryParsable, new()
        {
            var reader = new LineReader(default);
            foreach (var line in lineSplitter)
            {
                if (line.IsWhiteSpace())
                    continue;

                reader = new(line);
                var data = new T();
                if (data.TryParse(ref reader) && !reader.HasRemaining)
                {
                    result.Add(data);
                    continue;
                }
                reason = CreateFailureReason(line, reader.Current);
                return false;
            }
            reason = null;
            return true;
        }

        private static bool TryParseLines<T, P>(this ref LineSplitter<P> lineSplitter, P policy, List<T> result, out string reason)
            where T : ITryParsable<P>, new()
            where P : struct, IParsePolicy
        {
            var reader = new LineReader<P>(default, policy);
            foreach (var line in lineSplitter)
            {
                if (line.IsWhiteSpace())
                    continue;

                reader = new(line, policy);
                var data = new T();
                if (data.TryParse(ref reader) && !reader.HasRemaining)
                {
                    result.Add(data);
                    continue;
                }
                reason = CreateFailureReason(line, reader.Current);
                return false;
            }
            reason = null;
            return true;
        }

        private static bool TryPrepare<T>(this string text, List<T> result, int reserveLine, bool revertOnFailure, QuoteEscapeMode escapeMode, out int originalCount, out string reason)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (string.IsNullOrWhiteSpace(text))
            {
                originalCount = 0;
                reason = "input text is null or empty";
                return false;
            }
            if (revertOnFailure)
            {
                originalCount = result.Count;
            }
            else
            {
                originalCount = 0;
                result.Clear();
            }
            int requiredCapacity = checked(originalCount + GetInitialCapacity(text, reserveLine, escapeMode));
            if (result.Capacity < requiredCapacity)
            {
                result.Capacity = requiredCapacity;
            }
            reason = null;
            return true;
        }

        private static void CompleteTryParse<T>(this List<T> result, int originalCount, bool success)
        {
            if (success)
            {
                if (originalCount > 0)
                {
                    result.RemoveRange(0, originalCount);
                }
                return;
            }
            int appendedCount = result.Count - originalCount;
            if (appendedCount > 0)
            {
                result.RemoveRange(originalCount, appendedCount);
            }
        }

        [MethodImpl(Opt.Inline)]
        private static int GetInitialCapacity(string text, int reserveLine, QuoteEscapeMode escapeMode)
        {
            if (reserveLine < 0)
                throw new ArgumentOutOfRangeException(nameof(reserveLine));

            return reserveLine > 0 ? reserveLine : EstimateInitialCapacity(text, escapeMode);
        }

        private static int EstimateInitialCapacity(string text, QuoteEscapeMode escapeMode)
        {
            int sampleLength = text.Length < _capacitySampleLength ? text.Length : _capacitySampleLength;
            var sample = text.AsSpan(0, sampleLength);
            int lineBreakCount = 0;
            int offset = 0;
            while (offset < sampleLength)
            {
                int index = sample[offset..].IndexOfUnquoted('\n', escapeMode);
                if (index < 0)
                    break;

                ++lineBreakCount;
                offset += index + 1;
            }
            if (lineBreakCount == 0)
                return _defaultInitialCapacity;

            long estimatedCapacity = ((long)text.Length * lineBreakCount + sampleLength - 1) / sampleLength + 1;
            if (estimatedCapacity <= _defaultInitialCapacity)
                return _defaultInitialCapacity;

            return estimatedCapacity < _maximumEstimatedCapacity
                ? (int)estimatedCapacity
                : _maximumEstimatedCapacity;
        }

        private static string CreateFailureReason(ReadOnlySpan<char> line, ReadOnlySpan<char> cell)
            => $"parse failed. line: {line.ToString()}, cell: {cell.ToString()}";

        private static FormatException CreateFormatException(ReadOnlySpan<char> line, ReadOnlySpan<char> cell, Exception exception)
            => new($"{CreateFailureReason(line, cell)}, reason: {exception.Message}", exception);
    }
}
