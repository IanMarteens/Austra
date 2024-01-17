using System.Security.Cryptography;

namespace Austra.Library.Helpers;

/// <summary>Generates AVX512-compatible random numbers using the xoshiro256** algorithm.</summary>
public sealed class Random512
{
    /// <summary>A shared instance of the generator using a randomized seed.</summary>
    [ThreadStatic]
    private static Random512? shared;

    /// <summary>Converts a ulong to a double in the range [0, 1).</summary>
    private const double NORM = 1.0 / (1UL << 53);
    private Vector512<ulong> _s0, _s1, _s2, _s3;

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
    public V8d NextDouble()
    {
        Vector512<ulong> x = NextUInt64() >> 11;
        return V8.Create(
            x.ToScalar() * NORM, x[1] * NORM, x[2] * NORM, x[3] * NORM,
            x[4] * NORM, x[5] * NORM, x[6] * NORM, x[7] * NORM);
    }
}