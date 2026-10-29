using System;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    /// <summary>
    /// Defines numeric operations for generic algorithms that constrain the operator implementation to a struct,
    /// avoiding boxing and enabling the runtime or AOT compiler to specialize calls.
    /// </summary>
    public interface INumOp<T>
    {
        T Zero { get; }
        T One { get; }
        T Min { get; }
        T Max { get; }

        T Inc(ref T a);
        T Dec(ref T a);

        T Add(T a, T b);
        T Sub(T a, T b);
        T Mul(T a, T b);
        T Div(T a, T b);
        T Mod(T a, T b);

        bool EQ(T a, T b);
        bool GT(T a, T b);
        bool LT(T a, T b);
        bool GTE(T a, T b);
        bool LTE(T a, T b);

        int ToInt(T a);
        T FromInt(int a);
    }

    public readonly struct IntOp : INumOp<int>
    {
        public int Zero => 0;
        public int One => 1;
        public int Min => int.MinValue;
        public int Max => int.MaxValue;

        [MethodImpl(Opt.Inline)] public int Inc(ref int a) => ++a;
        [MethodImpl(Opt.Inline)] public int Dec(ref int a) => --a;

        [MethodImpl(Opt.Inline)] public int Add(int a, int b) => a + b;
        [MethodImpl(Opt.Inline)] public int Sub(int a, int b) => a - b;
        [MethodImpl(Opt.Inline)] public int Mul(int a, int b) => a * b;
        [MethodImpl(Opt.Inline)] public int Div(int a, int b) => a / b;
        [MethodImpl(Opt.Inline)] public int Mod(int a, int b) => a % b;

        [MethodImpl(Opt.Inline)] public bool EQ(int a, int b) => a == b;
        [MethodImpl(Opt.Inline)] public bool GT(int a, int b) => a > b;
        [MethodImpl(Opt.Inline)] public bool LT(int a, int b) => a < b;
        [MethodImpl(Opt.Inline)] public bool GTE(int a, int b) => a >= b;
        [MethodImpl(Opt.Inline)] public bool LTE(int a, int b) => a <= b;

        [MethodImpl(Opt.Inline)] public int ToInt(int a) => a;
        [MethodImpl(Opt.Inline)] public int FromInt(int a) => a;
    }

    public readonly struct UIntOp : INumOp<uint>
    {
        public uint Zero => 0;
        public uint One => 1;
        public uint Min => uint.MinValue;
        public uint Max => uint.MaxValue;

        [MethodImpl(Opt.Inline)] public uint Inc(ref uint a) => ++a;
        [MethodImpl(Opt.Inline)] public uint Dec(ref uint a) => --a;

        [MethodImpl(Opt.Inline)] public uint Add(uint a, uint b) => a + b;
        [MethodImpl(Opt.Inline)] public uint Sub(uint a, uint b) => a - b;
        [MethodImpl(Opt.Inline)] public uint Mul(uint a, uint b) => a * b;
        [MethodImpl(Opt.Inline)] public uint Div(uint a, uint b) => a / b;
        [MethodImpl(Opt.Inline)] public uint Mod(uint a, uint b) => a % b;

        [MethodImpl(Opt.Inline)] public bool EQ(uint a, uint b) => a == b;
        [MethodImpl(Opt.Inline)] public bool GT(uint a, uint b) => a > b;
        [MethodImpl(Opt.Inline)] public bool LT(uint a, uint b) => a < b;
        [MethodImpl(Opt.Inline)] public bool GTE(uint a, uint b) => a >= b;
        [MethodImpl(Opt.Inline)] public bool LTE(uint a, uint b) => a <= b;

        [MethodImpl(Opt.Inline)] public int ToInt(uint a) => a > int.MaxValue ? Throw(a) : (int)a;
        [MethodImpl(Opt.Inline)] public uint FromInt(int a) => a < 0 ? Throw(a) : (uint)a;

        // Keep exceptional paths separate from the inlineable conversion methods.
        [MethodImpl(Opt.NoInline)] private static int Throw(uint a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
        [MethodImpl(Opt.NoInline)] private static uint Throw(int a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
    }

    public readonly struct LongOp : INumOp<long>
    {
        public long Zero => 0;
        public long One => 1;
        public long Min => long.MinValue;
        public long Max => long.MaxValue;

        [MethodImpl(Opt.Inline)] public long Inc(ref long a) => ++a;
        [MethodImpl(Opt.Inline)] public long Dec(ref long a) => --a;

        [MethodImpl(Opt.Inline)] public long Add(long a, long b) => a + b;
        [MethodImpl(Opt.Inline)] public long Sub(long a, long b) => a - b;
        [MethodImpl(Opt.Inline)] public long Mul(long a, long b) => a * b;
        [MethodImpl(Opt.Inline)] public long Div(long a, long b) => a / b;
        [MethodImpl(Opt.Inline)] public long Mod(long a, long b) => a % b;

        [MethodImpl(Opt.Inline)] public bool EQ(long a, long b) => a == b;
        [MethodImpl(Opt.Inline)] public bool GT(long a, long b) => a > b;
        [MethodImpl(Opt.Inline)] public bool LT(long a, long b) => a < b;
        [MethodImpl(Opt.Inline)] public bool GTE(long a, long b) => a >= b;
        [MethodImpl(Opt.Inline)] public bool LTE(long a, long b) => a <= b;

        [MethodImpl(Opt.Inline)] public int ToInt(long a) => a < int.MinValue || a > int.MaxValue ? Throw(a) : (int)a;
        [MethodImpl(Opt.Inline)] public long FromInt(int a) => a;

        [MethodImpl(Opt.NoInline)] private static int Throw(long a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
    }

    public readonly struct ULongOp : INumOp<ulong>
    {
        public ulong Zero => 0;
        public ulong One => 1;
        public ulong Min => ulong.MinValue;
        public ulong Max => ulong.MaxValue;

        [MethodImpl(Opt.Inline)] public ulong Inc(ref ulong a) => ++a;
        [MethodImpl(Opt.Inline)] public ulong Dec(ref ulong a) => --a;

        [MethodImpl(Opt.Inline)] public ulong Add(ulong a, ulong b) => a + b;
        [MethodImpl(Opt.Inline)] public ulong Sub(ulong a, ulong b) => a - b;
        [MethodImpl(Opt.Inline)] public ulong Mul(ulong a, ulong b) => a * b;
        [MethodImpl(Opt.Inline)] public ulong Div(ulong a, ulong b) => a / b;
        [MethodImpl(Opt.Inline)] public ulong Mod(ulong a, ulong b) => a % b;

        [MethodImpl(Opt.Inline)] public bool EQ(ulong a, ulong b) => a == b;
        [MethodImpl(Opt.Inline)] public bool GT(ulong a, ulong b) => a > b;
        [MethodImpl(Opt.Inline)] public bool LT(ulong a, ulong b) => a < b;
        [MethodImpl(Opt.Inline)] public bool GTE(ulong a, ulong b) => a >= b;
        [MethodImpl(Opt.Inline)] public bool LTE(ulong a, ulong b) => a <= b;

        [MethodImpl(Opt.Inline)] public int ToInt(ulong a) => a > int.MaxValue ? Throw(a) : (int)a;
        [MethodImpl(Opt.Inline)] public ulong FromInt(int a) => a < 0 ? Throw(a) : (ulong)a;

        [MethodImpl(Opt.NoInline)] private static int Throw(ulong a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
        [MethodImpl(Opt.NoInline)] private static ulong Throw(int a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
    }
}
