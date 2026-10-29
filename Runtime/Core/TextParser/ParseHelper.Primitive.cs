using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public static partial class ParseHelper
    {
        private const NumberStyles _intStyles = NumberStyles.Integer;
        private const NumberStyles _floatStyles = NumberStyles.Float;

        private static readonly IFormatProvider _format = CultureInfo.InvariantCulture;

        [MethodImpl(Opt.Inline)] public static sbyte ToSByte(this ReadOnlySpan<char> span) => sbyte.Parse(span, _intStyles, _format);
        [MethodImpl(Opt.Inline)] public static byte ToByte(this ReadOnlySpan<char> span) => byte.Parse(span, _intStyles, _format);
        [MethodImpl(Opt.Inline)] public static short ToShort(this ReadOnlySpan<char> span) => short.Parse(span, _intStyles, _format);
        [MethodImpl(Opt.Inline)] public static ushort ToUShort(this ReadOnlySpan<char> span) => ushort.Parse(span, _intStyles, _format);
        [MethodImpl(Opt.Inline)] public static int ToInt(this ReadOnlySpan<char> span) => int.Parse(span, _intStyles, _format);
        [MethodImpl(Opt.Inline)] public static uint ToUInt(this ReadOnlySpan<char> span) => uint.Parse(span, _intStyles, _format);
        [MethodImpl(Opt.Inline)] public static long ToLong(this ReadOnlySpan<char> span) => long.Parse(span, _intStyles, _format);
        [MethodImpl(Opt.Inline)] public static ulong ToULong(this ReadOnlySpan<char> span) => ulong.Parse(span, _intStyles, _format);
        [MethodImpl(Opt.Inline)] public static float ToFloat(this ReadOnlySpan<char> span) => float.Parse(span, _floatStyles, _format);
        [MethodImpl(Opt.Inline)] public static double ToDouble(this ReadOnlySpan<char> span) => double.Parse(span, _floatStyles, _format);
        [MethodImpl(Opt.Inline)] public static bool ToBool(this ReadOnlySpan<char> span) => span.Trim().TryParseForBit(out var result) ? result : bool.Parse(span);
        [MethodImpl(Opt.Inline)] public static string ToStr(this ReadOnlySpan<char> span) => span.Trim().TrimQuotes().UnescapeCsvQuotes();
        [MethodImpl(Opt.Inline)] public static DateTime ToDateTime(this ReadOnlySpan<char> span) => DateTime.Parse(span, _format);

        [MethodImpl(Opt.Inline)] public static bool TryToSByte(this ReadOnlySpan<char> span, out sbyte result) => sbyte.TryParse(span, _intStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToByte(this ReadOnlySpan<char> span, out byte result) => byte.TryParse(span, _intStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToShort(this ReadOnlySpan<char> span, out short result) => short.TryParse(span, _intStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToUShort(this ReadOnlySpan<char> span, out ushort result) => ushort.TryParse(span, _intStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToInt(this ReadOnlySpan<char> span, out int result) => int.TryParse(span, _intStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToUInt(this ReadOnlySpan<char> span, out uint result) => uint.TryParse(span, _intStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToLong(this ReadOnlySpan<char> span, out long result) => long.TryParse(span, _intStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToULong(this ReadOnlySpan<char> span, out ulong result) => ulong.TryParse(span, _intStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToFloat(this ReadOnlySpan<char> span, out float result) => float.TryParse(span, _floatStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToDouble(this ReadOnlySpan<char> span, out double result) => double.TryParse(span, _floatStyles, _format, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToBool(this ReadOnlySpan<char> span, out bool result) => span.Trim().TryParseForBit(out result) || bool.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToStr(this ReadOnlySpan<char> span, out string result) { result = span.ToStr(); return true; }
        [MethodImpl(Opt.Inline)] public static bool TryToDateTime(this ReadOnlySpan<char> span, out DateTime result) => DateTime.TryParse(span, _format, DateTimeStyles.None, out result);

        [MethodImpl(Opt.Inline)]
        private static bool TryParseForBit(this ReadOnlySpan<char> span, out bool result)
        {
            if (span.Length == 1 && span[0] is '0' or '1')
            {
                result = span[0] == '1';
                return true;
            }
            result = false;
            return false;
        }
    }
}
