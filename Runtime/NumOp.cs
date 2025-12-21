using System;
using System.Runtime.CompilerServices;

namespace FerryKit
{
    /// <summary>
    /// 숫자 연산 추상화 (구조체 제약을 통해 가상 함수 호출 및 박싱 발생 제거 가능)
    /// 추후 유니티에서 C# 11 이상 지원시 언어 자체에서 제공하는 INumber로 대체
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
        public readonly int Zero => 0;
        public readonly int One => 1;
        public readonly int Min => int.MinValue;
        public readonly int Max => int.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int Inc(ref int a) => ++a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int Dec(ref int a) => --a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int Add(int a, int b) => a + b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int Sub(int a, int b) => a - b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int Mul(int a, int b) => a * b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int Div(int a, int b) => a / b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int Mod(int a, int b) => a % b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool EQ(int a, int b) => a == b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool GT(int a, int b) => a > b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool LT(int a, int b) => a < b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool GTE(int a, int b) => a >= b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool LTE(int a, int b) => a <= b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int ToInt(int a) => a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int FromInt(int a) => a;
    }

    public readonly struct UIntOp : INumOp<uint>
    {
        public readonly uint Zero => 0;
        public readonly uint One => 1;
        public readonly uint Min => uint.MinValue;
        public readonly uint Max => uint.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly uint Inc(ref uint a) => ++a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly uint Dec(ref uint a) => --a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly uint Add(uint a, uint b) => a + b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly uint Sub(uint a, uint b) => a - b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly uint Mul(uint a, uint b) => a * b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly uint Div(uint a, uint b) => a / b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly uint Mod(uint a, uint b) => a % b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool EQ(uint a, uint b) => a == b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool GT(uint a, uint b) => a > b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool LT(uint a, uint b) => a < b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool GTE(uint a, uint b) => a >= b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool LTE(uint a, uint b) => a <= b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int ToInt(uint a) => a > int.MaxValue ? Throw(a) : (int)a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly uint FromInt(int a) => a < 0 ? Throw(a) : (uint)a;

        // ToInt 함수의 인라인 최적화를 방해하지 않도록 throw 구문을 별도 함수로 분리
        private static int Throw(uint a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
        private static uint Throw(int a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
    }

    public readonly struct LongOp : INumOp<long>
    {
        public readonly long Zero => 0;
        public readonly long One => 1;
        public readonly long Min => long.MinValue;
        public readonly long Max => long.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly long Inc(ref long a) => ++a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly long Dec(ref long a) => --a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly long Add(long a, long b) => a + b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly long Sub(long a, long b) => a - b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly long Mul(long a, long b) => a * b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly long Div(long a, long b) => a / b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly long Mod(long a, long b) => a % b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool EQ(long a, long b) => a == b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool GT(long a, long b) => a > b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool LT(long a, long b) => a < b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool GTE(long a, long b) => a >= b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool LTE(long a, long b) => a <= b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int ToInt(long a) => a > int.MaxValue || a < int.MinValue ? Throw(a) : (int)a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly long FromInt(int a) => a;

        private static int Throw(long a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
    }

    public readonly struct ULongOp : INumOp<ulong>
    {
        public readonly ulong Zero => 0;
        public readonly ulong One => 1;
        public readonly ulong Min => ulong.MinValue;
        public readonly ulong Max => ulong.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly ulong Inc(ref ulong a) => ++a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly ulong Dec(ref ulong a) => --a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly ulong Add(ulong a, ulong b) => a + b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly ulong Sub(ulong a, ulong b) => a - b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly ulong Mul(ulong a, ulong b) => a * b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly ulong Div(ulong a, ulong b) => a / b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly ulong Mod(ulong a, ulong b) => a % b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool EQ(ulong a, ulong b) => a == b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool GT(ulong a, ulong b) => a > b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool LT(ulong a, ulong b) => a < b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool GTE(ulong a, ulong b) => a >= b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool LTE(ulong a, ulong b) => a <= b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly int ToInt(ulong a) => a > int.MaxValue ? Throw(a) : (int)a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly ulong FromInt(int a) => a < 0 ? Throw(a) : (ulong)a;

        private static int Throw(ulong a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
        private static ulong Throw(int a) => throw new ArgumentOutOfRangeException(nameof(a), $"arg {a} is out of bounds.");
    }
}
