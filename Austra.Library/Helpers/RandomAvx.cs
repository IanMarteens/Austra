﻿using System.Security.Cryptography;

namespace Austra.Library.Helpers;

/// <summary>Generates AVX256-compatible random numbers using the xoshiro256** algorithm.</summary>
public sealed class Random256
{
    /// <summary>A shared instance of the generator using a randomized seed.</summary>
    [ThreadStatic]
    private static Random256? shared;

    /// <summary>Converts a ulong to a double in the range [0, 1).</summary>
    private const double NORM = 1.0 / (1UL << 53);
    /// <summary>Converts a ulong to a double in the range [0, τ).</summary>
    private const double TAU_NORM = Tau / (1UL << 53);
    /// <summary>Four vector seeds for the xoshiro256** algorithm.</summary>
    private Vector256<ulong> _s0, _s1, _s2, _s3;

    /// <summary>Pending value to return.</summary>
    private V4d item;
    /// <summary>Do we have a pending value to return.</summary>
    private bool hasItem;

    /// <summary>Initializes a new instance of the <see cref="Random256"/> class.</summary>
    public Random256()
    {
        Span<byte> bytes = stackalloc byte[16 * sizeof(ulong)];
        RandomNumberGenerator.Create().GetNonZeroBytes(bytes);
        ref byte b = ref MemoryMarshal.GetReference(bytes);
        _s0 = V4.LoadUnsafe(ref As<byte, ulong>(ref b));
        _s1 = V4.LoadUnsafe(ref As<byte, ulong>(ref Add(ref b, 4)));
        _s2 = V4.LoadUnsafe(ref As<byte, ulong>(ref Add(ref b, 8)));
        _s3 = V4.LoadUnsafe(ref As<byte, ulong>(ref Add(ref b, 12)));
    }

    /// <summary>A shared instance of the generator using a randomized seed.</summary>
    public static Random256 Shared => shared ??= new();

    /// <summary>Produces four values in the range [0, ulong.MaxValue].</summary>
    /// <returns>An AVX256 vector of unsigned long integers.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector256<ulong> NextUInt64()
    {
        // NextUInt64 is based on the algorithm from http://prng.di.unimi.it/xoshiro256starstar.c:
        //
        //     Written in 2018 by David Blackman and Sebastiano Vigna (vigna@acm.org)
        //
        //     To the extent possible under law, the author has dedicated all copyright
        //     and related and neighboring rights to this software to the public domain
        //     worldwide. This software is distributed without any warranty.
        //
        //     See <http://creativecommons.org/publicdomain/zero/1.0/>.

        Vector256<ulong> s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;
        Vector256<ulong> result = RotateLeft7(_s1 * V4.Create(5UL)) * V4.Create(9UL);
        Vector256<ulong> t = s1 << 17;

        s2 ^= s0; s3 ^= s1; s1 ^= s2; s0 ^= s3;
        s2 ^= t;
        s3 = RotateLeft45(s3);
        _s0 = s0; _s1 = s1; _s2 = s2; _s3 = s3;

        return result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<ulong> RotateLeft7(Vector256<ulong> x) =>
            Avx2.Or(Avx2.ShiftLeftLogical(x, 7), Avx2.ShiftRightLogical(x, 64 - 7));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<ulong> RotateLeft45(Vector256<ulong> x) =>
            Avx2.Or(Avx2.ShiftLeftLogical(x, 45), Avx2.ShiftRightLogical(x, 64 - 45));
    }

    /// <summary>Produces four values in the range [0, 1).</summary>
    /// <returns>An AVX256 vector of double precision reals.</returns>
    public V4d NextDouble() =>
        V4.ConvertToDouble(NextUInt64() >> 11) * V4.Create(NORM);

    /// <summary>Produces eight random unsigned integers.</summary>
    /// <returns>An AVX256 vector of unsigned integers.</returns>
    public Vector256<uint> NextUInt32() =>
        Vector256.Narrow(NextUInt64() >> 32, NextUInt64() >> 32);

    /// <summary>Produces four values from the standard normal distribution.</summary>
    /// <returns>An AVX256 vector of double precision reals.</returns>
    public V4d NextNormal()
    {
        if (hasItem)
        {
            hasItem = false;
            return item;
        }

        hasItem = true;
        V4d u = (V4d.One - NextDouble()).Log();
        V4d r = V4.Sqrt(-u - u);
        (V4d s, V4d c) = (V4.ConvertToDouble(NextUInt64() >> 11)
            * V4.Create(TAU_NORM)).SinCosNormal();
        item = s * r;
        return c * r;
    }
}

/// <summary>Generates AVX512-compatible random numbers using the xoshiro256** algorithm.</summary>
public sealed class Random512
{
    /// <summary>A shared instance of the generator using a randomized seed.</summary>
    [ThreadStatic]
    private static Random512? shared;

    /// <summary>Converts a ulong to a double in the range [0, 1).</summary>
    private const double NORM = 1.0 / (1UL << 53);
    /// <summary>Converts a ulong to a double in the range [0, τ).</summary>
    private const double TAU_NORM = Tau / (1UL << 53);
    /// <summary>Four vector seeds for the xoshiro256** algorithm.</summary>
    private Vector512<ulong> _s0, _s1, _s2, _s3;

    /// <summary>Pending value to return, for the normal distribution generator.</summary>
    private V8d item;
    /// <summary>Do we have a pending value to return.</summary>
    private bool hasItem;

    /// <summary>Initializes a new instance of the <see cref="Random512"/> class.</summary>
    public Random512()
    {
        Span<byte> bytes = stackalloc byte[32 * sizeof(ulong)];
        RandomNumberGenerator.Create().GetNonZeroBytes(bytes);
        ref byte b = ref MemoryMarshal.GetReference(bytes);
        _s0 = V8.LoadUnsafe(ref As<byte, ulong>(ref b));
        _s1 = V8.LoadUnsafe(ref As<byte, ulong>(ref Add(ref b, 8)));
        _s2 = V8.LoadUnsafe(ref As<byte, ulong>(ref Add(ref b, 16)));
        _s3 = V8.LoadUnsafe(ref As<byte, ulong>(ref Add(ref b, 24)));
    }

    /// <summary>A shared instance of the generator using a randomized seed.</summary>
    public static Random512 Shared => shared ??= new();

    /// <summary>Produces eight values in the range [0, ulong.MaxValue].</summary>
    /// <returns>An AVX512 vector of unsigned long integers.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector512<ulong> NextUInt64()
    {
        // NextUInt64 is based on the algorithm from http://prng.di.unimi.it/xoshiro256starstar.c:
        //
        //     Written in 2018 by David Blackman and Sebastiano Vigna (vigna@acm.org)
        //
        //     To the extent possible under law, the author has dedicated all copyright
        //     and related and neighboring rights to this software to the public domain
        //     worldwide. This software is distributed without any warranty.
        //
        //     See <http://creativecommons.org/publicdomain/zero/1.0/>.

        Vector512<ulong> s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;
        Vector512<ulong> result = Avx512F.RotateLeft(_s1 * V8.Create(5UL), 7) * V8.Create(9UL);
        Vector512<ulong> t = s1 << 17;

        s2 ^= s0; s3 ^= s1; s1 ^= s2; s0 ^= s3;
        s2 ^= t;
        s3 = Avx512F.RotateLeft(s3, 45);
        _s0 = s0; _s1 = s1; _s2 = s2; _s3 = s3;

        return result;
    }

    /// <summary>Produces eight values in the range [0, 1).</summary>
    /// <returns>An AVX512 vector of double precision reals.</returns>
    public V8d NextDouble() => V8.ConvertToDouble(NextUInt64() >> 11) * V8.Create(NORM);

    /// <summary>Produces sixteen random unsigned integers.</summary>
    /// <returns>An AVX512 vector of unsigned integers.</returns>
    public Vector512<uint> NextUInt32() =>
        Vector512.Narrow(NextUInt64() >> 32, NextUInt64() >> 32);

    /// <summary>Produces eight values from the standard normal distribution.</summary>
    /// <returns>An AVX512 vector of double precision reals.</returns>
    public V8d NextNormal()
    {
        if (hasItem)
        {
            hasItem = false;
            return item;
        }

        hasItem = true;
        V8d u = (V8d.One - NextDouble()).Log();
        V8d r = V8.Sqrt(-u - u);
        (V8d s, V8d c) = (V8.ConvertToDouble(NextUInt64() >> 11)
            * V8.Create(TAU_NORM)).SinCosNormal();
        item = s * r;
        return c * r;
    }
}