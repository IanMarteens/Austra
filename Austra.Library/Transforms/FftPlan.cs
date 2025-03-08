namespace Austra.Library.Transforms;

/// <summary>Represents a Fast Fourier Transform plan.</summary>
public sealed partial class FftPlan
{
    /// <summary>The list of implemented sub-algorithms.</summary>
    private enum Op : byte
    {
        /// <summary>End of FFT transform.</summary>
        End,
        /// <summary>Param0 stores position of the jump target.</summary>
        Jump,
        /// <summary>Bluestein algorithm, with zero-padding.</summary>
        Bluestein,
        /// <summary>Rader algorithm, with precomputed data.</summary>
        Rader,
        /// <summary>Optimized 1D FFT for small sizes.</summary>
        ComplexCodelet,
        /// <summary>Codelets with micro-vectors size greater than 2.</summary>
        IntegratedCodelet,
        /// <summary>Complex transform, using temporary buffer.</summary>
        Transpose, SmallTranspose,
        /// <summary>Apply twiddle factors for Cooley-Tukey.</summary>
        TwiddleFactors,
        /// <summary>May call subplans in parallel.</summary>
        ParallelCall,
        Start,
    }

    /// <summary>Individual sub-algorithms in a FFT plan.</summary>
    private struct Plan
    {
        /// <summary>Operation type.</summary>
        public Op Type { get; }
        /// <summary>Repetition count.</summary>
        public int Count { get; }
        /// <summary>Number of micro-vectors in operand.</summary>
        public int Size { get; }
        public int Param0 { readonly get; set; }
        public int Param1 { readonly get; init; }
        /// <summary>Offset of precomputed data, for Rader's algorithm.</summary>
        public int Param2 { readonly get; init; }

        /// <summary>Total size of affected data, as real numbers.</summary>
        public readonly int TotalSize => Count * Size * 2;

        public Plan(Op type) => Type = type;
        public Plan(Op type, int count, int size, int p0) =>
            (Type, Count, Size, Param0) = (type, count, size, p0);

        public readonly string Describe() => Type switch
        {
            Op.Bluestein => $"Bluestein, {Count}x{Size}",
            Op.Rader => $"Rader, count: {Count}, size: {Size}",
            Op.ComplexCodelet => $"Codelet-{Size} x{Count}",
            Op.IntegratedCodelet => $"Codelet-{Size} x{Count}x{Param0}",
            Op.TwiddleFactors => $"Twiddle factors, {Param0}x{Size / Param0}",
            Op.Transpose or Op.SmallTranspose => $"Transpose {Param0}x{Size / Param0}",
            Op.ParallelCall => $"Parallel call",
            Op.Start => $"Sub, chunk size: {TotalSize}",
            _ => "",
        };
    }

    private const int UPDATE_TW = 16;
    private const int MAX_RADIX = 6;
    private const int RecursiveThreshold = 1024;
    private const int RaderThreshold = 19;

    /// <summary>Mask vector to conjugate one complex value.</summary>
    private static readonly Vector128<double> cnj = Vector128.Create(0.0, -0.0);
    /// <summary>Mask vector to conjugate two complex values at once.</summary>
    private static readonly V4d CNJ = V4.Create(0.0, -0.0, 0.0, -0.0);

    private static readonly double Cos2πOver3 = Cos(Tau / 3);
    private static readonly double Sin2πOver3 = Sin(Tau / 3);
    private static readonly double Cos2πOver5 = Cos(Tau / 5);
    private static readonly double Cos4πOver5 = Cos(4 * PI / 5);
    private static readonly double Sin2πOver5 = Sin(Tau / 5);
    private static readonly double Sin4πOver5 = Sin(4 * PI / 5);
    private static readonly double CosπOver3 = Cos(-PI / 3);
    private static readonly double SinπOver3 = Sin(-PI / 3);

    /// <summary>Number of entries, so far.</summary>
    private int count;
    /// <summary>Instructions for the transformation.</summary>
    private Plan[] plans = new Plan[8];
    /// <summary>Data buffer for the plan.</summary>
    private readonly double[] buffer;
    /// <summary>Precomputed data for Rader's algorithm.</summary>
    private readonly double[] precr;
    /// <summary>Array pool for Bluestein's algorithm.</summary>
    private readonly SharedPool pool;

    /// <summary>Generates a FFT plan for a complex FFT's with length N.</summary>
    /// <param name="n">FFT length, in complex numbers.</param>
    /// <param name="buffer">Buffer for the plan.</param>
    private FftPlan(int n, double[] buffer)
    {
        this.buffer = buffer;
        int precrsize = GetSpaceRequirements(n);
        precr = precrsize > 0 ? new double[precrsize] : [];
        int precrptr = 0;
        int bluesteinSize = 1;
        CreatePlan(n, 1, true, true, ref bluesteinSize, ref precrptr);
        pool = new(bluesteinSize);

        // Buffer size is determined as follows:
        // * N is factorized, factoring out all small factors.
        // * Prime factor F>RaderThreshold requires 4*FindSmooth(2*F-1)
        //   real entries to store precomputed Quantities for Bluestein's transformation
        // * Prime factor F<=RaderThreshold does NOT require precomputed storage
        static int GetSpaceRequirements(int n)
        {
            int size = 0;
            while (n % 2 == 0) n /= 2;
            while (n % 3 == 0) n /= 3;
            while (n % 5 == 0) n /= 5;
            for (int f = MAX_RADIX + 1; f <= n; f++)
            {
                while (n % f == 0)
                {
                    if (f > RaderThreshold)
                        size += 4 * FindSmooth(2 * f - 1);
                    else
                        size += 2 * (f - 1) + GetSpaceRequirements(f - 1);
                    n /= f;
                }
            }
            return size;
        }
    }

    /// <summary>Generates a FFT plan for a complex FFT's with length N.</summary>
    /// <param name="n">FFT length, in complex numbers.</param>
    public FftPlan(int n) : this(n, new double[n + n]) { }

    /// <summary>Describes the transform plan.</summary>
    /// <returns>A list of transformation steps.</returns>
    public string Describe()
    {
        StringBuilder sb = new StringBuilder()
            .AppendLine($"FFT plan for size = {buffer.Length / 2}, {pool.ArraySize}");
        string indent = "  ";
        for (int i = 1; i < count; i++)
        {
            ref Plan entry = ref plans[i];
            switch (entry.Type)
            {
                case Op.Jump: continue;
                case Op.Rader:
                case Op.Bluestein:
                    sb.AppendLine($"{indent}{entry.Describe()} {{");
                    indent += "  ";
                    i += 2;
                    break;
                case Op.Start:
                    sb.AppendLine($"{indent}{entry.Describe()} {{");
                    indent += "  ";
                    break;
                case Op.End:
                    if (indent.Length == 2)
                        continue;
                    indent = indent[..^2];
                    sb.AppendLine(indent + "}");
                    break;
                default:
                    sb.AppendLine($"{indent}{entry.Describe()}");
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>The smallest smooth number (divisible only by 2, 3, 5) >= max(N,2).</summary>
    private static int FindSmooth(int n)
    {
        int best = n - 1;
        best |= best >> 1;
        best |= best >> 2;
        best |= best >> 4;
        best |= best >> 8;
        best |= best >> 16;
        best++;
        FindSmoothRec(n, 1, 2, ref best);
        return best;

        static void FindSmoothRec(int n, int seed, int leastFactor, ref int best)
        {
            if (seed >= n)
            {
                best = Min(best, seed);
                return;
            }
            if (leastFactor <= 2)
                FindSmoothRec(n, seed * 2, 2, ref best);
            if (leastFactor <= 3)
                FindSmoothRec(n, seed * 3, 3, ref best);
            if (leastFactor <= 5)
                FindSmoothRec(n, seed * 5, 5, ref best);
        }
    }

    /// <summary>Builds the transformation plan recursively.</summary>
    /// <param name="n">FFT length, in complex numbers.</param>
    /// <param name="k">Numbers of repetitions, >= 1.</param>
    /// <param name="child">If true, the plan will be surrounded with Start/End.</param>
    /// <param name="topmost">
    /// If true, the plan may use the global buffer for transpositions
    /// and there is no other plan  which executes in parallel.
    /// </param>
    /// <param name="bluesteinSize">Storage required for the Bluestein buffer.</param>
    /// <param name="precPtr">Index to precomputed data.</param>
    private unsafe void CreatePlan(
        int n, int k, bool child, bool topmost,
        ref int bluesteinSize,
        ref int precPtr)
    {
        (int n1, int n2) = Factorize(n);

        // Generate the top-level plan.
        if (topmost && n > RecursiveThreshold)
        {
            Push(Op.Start, k, n);
            if (n1 == 0)
            {
                // Determine size of Bluestein's buffer.
                int m = FindSmooth(2 * n - 1);
                bluesteinSize = Max(2 * m, bluesteinSize);
                // Generate plan for Bluestein's FFT.
                PushFull(Op.Bluestein, k, n, m, precPtr, 0);
                int row0 = count;
                PushOp(Op.Jump);
                CreatePlan(m, 1, true, true, ref bluesteinSize, ref precPtr);
                plans[row0].Param0 = count;
                PushOp(Op.End);
                // Fill precomputed buffer.
                fixed (double* prec = &precr[precPtr])
                    PrecomputeBluestein(m, prec);
                // Update pointer to the precomputed area.
                precPtr += 4 * m;
            }
            else
            {
                // Composite FFT with recursive Cooley-Tukey.
                PushTranspose(k, n, n1);
                int row0 = count;
                Push(Op.ParallelCall, k * n2, n1);
                Push(Op.TwiddleFactors, k, n, n1);
                PushTranspose(k, n, n2);
                int row1 = count;
                Push(Op.ParallelCall, k * n1, n2);
                PushTranspose(k, n, n1);
                PushOp(Op.End);
                plans[row0].Param0 = count;
                CreatePlan(n1, 1, true, false, ref bluesteinSize, ref precPtr);
                plans[row1].Param0 = count;
                CreatePlan(n2, 1, true, false, ref bluesteinSize, ref precPtr);
            }
        }
        else
        {
            // Handle FFT's with N1*N2=0: either small-N or prime-factor
            if (child)
                Push(Op.Start, k, n);
            if (n1 == 0)
            {
                if (n <= MAX_RADIX)
                    // Small-N FFT
                    Push(Op.ComplexCodelet, k, n);
                else if (n <= RaderThreshold)
                {
                    // Handle prime-factor FFT's with Rader's FFT
                    FindPrimitiveRoot(n, out int gq, out int giq);
                    PushFull(Op.Rader, k, n, gq, giq, precPtr);
                    fixed (double* prec = &precr[precPtr])
                        PrecomputeRader(giq, prec);
                    precPtr += 2 * (n - 1);
                    int row0 = count;
                    PushOp(Op.Jump);
                    CreatePlan(n - 1, 1, true, false, ref bluesteinSize, ref precPtr);
                    plans[row0].Param0 = count;
                }
                else
                {
                    // Handle prime-factor FFT's with Bluestein's FFT
                    int m = FindSmooth(2 * n - 1);
                    bluesteinSize = Max(2 * m, bluesteinSize);
                    PushFull(Op.Bluestein, k, n, m, precPtr, 0);
                    fixed (double* prec = &precr[precPtr])
                        PrecomputeBluestein(m, prec);
                    precPtr += 4 * m;
                    int row0 = count;
                    PushOp(Op.Jump);
                    CreatePlan(m, 1, true, false, ref bluesteinSize, ref precPtr);
                    plans[row0].Param0 = count;
                }
                if (child)
                    PushOp(Op.End);
            }
            // Handle Cooley-Tukey FFT with small N1
            else if (n1 <= MAX_RADIX)
            {
                // Specialized transformation for small N1:
                // * N2 short in-place FFT's, each N1-point, with integrated twiddle factors
                // * N1 long FFT's
                // * final transposition
                Push(Op.IntegratedCodelet, k, n1, 2 * n2);
                CreatePlan(n2, k * n1, false, false, ref bluesteinSize, ref precPtr);
                PushTranspose(k, n, n1);
                if (child)
                    PushOp(Op.End);
            }
            // Handle general Cooley-Tukey FFT, either "flat" or "recursive"
            else if (n <= RecursiveThreshold)
            {
                // General code for large N1/N2, "flat" version without explicit recurrence
                // (nested subplans are inserted directly into the body of the plan)
                PushTranspose(k, n, n1);
                CreatePlan(n1, k * n2, false, false, ref bluesteinSize, ref precPtr);
                Push(Op.TwiddleFactors, k, n, n1);
                PushTranspose(k, n, n2);
                CreatePlan(n2, k * n1, false, false, ref bluesteinSize, ref precPtr);
                PushTranspose(k, n, n1);
                if (child)
                    PushOp(Op.End);
            }
            else
            {
                // General code for large N1/N2. Nested subplans are separated from the plan body.
                PushTranspose(k, n, n1);
                int row0 = count;
                Push(Op.ParallelCall, k * n2, n1);
                Push(Op.TwiddleFactors, k, n, n1);
                PushTranspose(k, n, n2);
                int row1 = count;
                Push(Op.ParallelCall, k * n1, n2);
                PushTranspose(k, n, n1);
                if (child)
                    PushOp(Op.End);

                // Generate child subplans & insert reference to parent plans
                plans[row0].Param0 = count;
                CreatePlan(n1, 1, true, false, ref bluesteinSize, ref precPtr);
                plans[row1].Param0 = count;
                CreatePlan(n2, 1, true, false, ref bluesteinSize, ref precPtr);
            }
        }

        void PushOp(Op type)
        {
            if (count >= plans.Length)
                Array.Resize(ref plans, 2 * plans.Length);
            plans[count++] = new(type);
        }

        void Push(Op type, int cnt, int size, int param0 = 0)
        {
            if (count >= plans.Length)
                Array.Resize(ref plans, 2 * plans.Length);
            plans[count++] = new(type, cnt, size, param0);
        }

        void PushFull(Op type, int cnt, int size,
            int param0, int param1, int param2)
        {
            if (count >= plans.Length)
                Array.Resize(ref plans, 2 * plans.Length);
            plans[count++] = new(type, cnt, size, param0)
            {
                Param1 = param1,
                Param2 = param2,
            };
        }

        void PushTranspose(int reps, int size, int dims)
        {
            if (count >= plans.Length)
                Array.Resize(ref plans, 2 * plans.Length);
            plans[count++] = new(dims <= BLOCK_SIZE && size / dims <= BLOCK_SIZE
                ? Op.SmallTranspose
                : Op.Transpose,
                reps, size, dims);
        }

        static (int n1, int n2) Factorize(int n)
        {
            // Small N.
            if (n <= MAX_RADIX)
                return (0, 0);
            // Large N, recursive split.
            if (n > RecursiveThreshold)
            {
                for (int j = (int)Ceiling(Sqrt(n)) + 1; j >= 2; j--)
                {
                    (int q, int r) = DivRem(n, j);
                    if (r == 0)
                        return j > q ? (q, j) : (j, q);
                }
                return (0, 0);
            }
            // N > MAX_RADIX, try to find a good codelet.
            for (int j = MAX_RADIX; j >= 2; j--)
            {
                (int q, int r) = DivRem(n, j);
                if (r == 0)
                    return j > q ? (q, j) : (j, q);
            }
            // Try to factorize N into a product of any primes.
            for (int j = MAX_RADIX + 1; j * j <= n; j++)
            {
                (int q, int r) = DivRem(n, j);
                if (r == 0)
                    return j > q ? (q, j) : (j, q);
            }
            return (0, 0);
        }

        void PrecomputeRader(int rootInverse, double* prec)
        {
            double TwoPiOverN = -Tau / n;
            for (int q = 0, kiq = 1; q <= n - 2; q++, kiq = kiq * rootInverse % n)
            {
                double v = TwoPiOverN * kiq;
                prec[2 * q] = Cos(v); prec[2 * q + 1] = Sin(v);
            }
            // Use the parent plan's buffer for the local FFT.
            FftPlan plan = new(n - 1, buffer);
            plan.Execute(1, prec, plan.buffer);
        }

        void PrecomputeBluestein(int m, double* prec)
        {
            // Fill first half of prec with b[k] = exp(iπ*k^2/n)
            double πOverN = PI / n;
            if (Avx.IsSupported)
            {
                int max = n & ~1;
                int kSqr = 1;
                for (int k = 0; k < max; k += 2)
                {
                    int k2 = k + k;
                    kSqr += k2 - 1;
                    double v = kSqr * πOverN;
                    kSqr += k2 + 1;
                    double w = kSqr * πOverN;
                    var b = V4.Create(Cos(v), Sin(v), Cos(w), Sin(w));
                    Avx.Store(prec + k2, b);
                    Sse2.Store(prec + 2 * ((m - k) % m), b.GetLower());
                    Sse2.Store(prec + 2 * ((m - k - 1) % m), b.GetUpper());
                }
                if (max != n)
                {
                    double v = (max * max) * πOverN;
                    var b = Vector128.Create(Cos(v), Sin(v));
                    Sse2.Store(prec + 2 * max, b);
                    Sse2.Store(prec + 2 * ((m - max) % m), b);
                }
            }
            else
            {
                for (int k = 0; k < n; k++)
                {
                    double v = (k * k) * πOverN;
                    double bx = Cos(v), by = Sin(v);
                    prec[2 * k] = bx; prec[2 * k + 1] = by;
                    prec[2 * ((m - k) % m)] = bx; prec[2 * ((m - k) % m) + 1] = by;
                }
            }
            long size = (long)m * (2 * sizeof(double));
            Buffer.MemoryCopy(prec, prec + 2 * m, size, size);
            // Precomputed FFT.
            FftPlan plan = new(m);
            plan.Execute(1, prec + 2 * m, plan.buffer);
        }
    }

    /// <summary>Applies a transformation plan to array <paramref name="a"/>.</summary>
    /// <param name="a">The input/output array.</param>
    public unsafe void Execute(double* a) => Execute(1, a, buffer);

    /// <summary>Applies a subplan to the input/output array A.</summary>
    /// <param name="row">Subplan index plus one.</param>
    /// <param name="a">The input/output array.</param>
    /// <param name="buffer">Temporary buffer.</param>
    /// <param name="reps">Repetition count.</param>
    private unsafe void Execute(int row, double* a, Span<double> buffer, int reps = 1)
    {
        for (ref Plan entry0 = ref MM.GetArrayDataReference(plans); ;)
        {
            ref Plan e = ref Unsafe.Add(ref entry0, row);
            switch (e.Type)
            {
                case Op.End:
                    return;

                case Op.Jump:
                    row = e.Param0;
                    continue;

                case Op.ParallelCall:
                    {
                        int param0 = e.Param0;
                        int childSize = Unsafe.Add(ref entry0, param0++).TotalSize;
                        int chunkSize = Max(RecursiveThreshold / childSize, 1);
                        int count = reps * e.Count;
                        for (int i = 0; i < count; i += chunkSize)
                        {
                            chunkSize = Min(chunkSize, count - i);
                            Execute(param0, a + i * childSize, buffer, chunkSize);
                        }
                    }
                    row++;
                    continue;

                case Op.ComplexCodelet:
                    ComplexCodelet(a, reps * e.Count, e.Size);
                    row++;
                    continue;

                case Op.IntegratedCodelet:
                    switch (e.Size)
                    {
                        case 2: Codelet2(a, reps * e.Count, e.Param0); break;
                        case 3: Codelet3(a, reps * e.Count, e.Param0); break;
                        case 4: Codelet4(a, reps * e.Count, e.Param0); break;
                        case 5: Codelet5(a, reps * e.Count, e.Param0); break;
                        case 6: Codelet6(a, reps * e.Count, e.Param0); break;
                    }
                    row++;
                    continue;

                case Op.Transpose:
                    fixed (double* b = buffer)
                    {
                        int sz = e.Size, cnt = reps * e.Count, n1 = e.Param0, n2 = sz / n1;
                        for (double* pa = a; cnt-- > 0; pa += 2 * sz)
                            if (Avx.IsSupported)
                                ComplexTranspose((Complex*)pa, (Complex*)b, n1, n2);
                            else
                                ComplexTranspose(pa, b, n1, n2);
                    }
                    row++;
                    continue;

                case Op.SmallTranspose:
                    fixed (double* b = buffer)
                    {
                        int sz = e.Size, cnt = reps * e.Count, n1 = e.Param0, n2 = sz / n1;
                        for (double* pa = a; cnt-- > 0; pa += 2 * sz)
                            if (Avx.IsSupported)
                                SmallComplexTranspose((Complex*)pa, (Complex*)b, n1, n2);
                            else
                                ComplexTranspose(pa, b, n1, n2);
                    }
                    row++;
                    continue;

                case Op.Bluestein:
                    ApplyBluestein(a, reps * e.Count, e.Size, row + 3, e.Param0, e.Param1);
                    row++;
                    continue;

                case Op.Rader:
                    ApplyRader(a, reps * e.Count, e.Size, row + 3,
                        e.Param0, e.Param1, e.Param2, buffer);
                    row++;
                    continue;

                case Op.TwiddleFactors:
                    {
                        int size = e.Size, count = reps * e.Count, p = e.Param0;
                        for (int i = 0, n2 = size / p; i < count; i++)
                            TwiddleFactors(a + i * size * 2, p, n2);
                    }
                    row++;
                    continue;
            }
        }
    }

    /// <summary>Applies complex Rader's FFT to the input/output array.</summary>
    /// <param name="a">The input output array.</param>
    /// <param name="count">Number of repeated operands.</param>
    /// <param name="n">Original data length.</param>
    /// <param name="subplan">Index of the subplan.</param>
    /// <param name="rq">Primitive root modulo N.</param>
    /// <param name="riq">Inverse of primitive root modulo N.</param>
    /// <param name="offs">Offset of the precomputed data for the plan.</param>
    /// <param name="buf">Temporary array.</param>
    private unsafe void ApplyRader(
        double* a, int count, int n, int subplan,
        int rq, int riq, int offs, Span<double> buf)
    {
        fixed (double* b = buf)
        {
            double* pb = b;
            double rx = 0, ry = 0, x0 = 0, y0 = 0;
            int n1 = n - 1;
            Vector128<double> r = Vector128<double>.Zero, v = r;
            for (int idx = 0; idx < count; idx++)
            {
                if (Avx.IsSupported)
                {
                    r = v = Sse2.LoadVector128(a);
                    for (int i = 0, p = 0, kq = 1; i < n1; i++, p += 2, kq = kq * rq % n)
                    {
                        var aa = Sse2.LoadVector128(a + 2 * kq);
                        Sse2.Store(pb + p, aa);
                        r = Sse2.Add(r, aa);
                    }
                }
                else
                {
                    rx = a[0]; ry = a[1];
                    x0 = rx; y0 = ry;
                    for (int i = 0, p = 0, kq = 1; i < n1; i++, p += 2, kq = kq * rq % n)
                    {
                        double ax = a[2 * kq], ay = a[2 * kq + 1];
                        pb[p] = ax; pb[p + 1] = ay;
                        rx += ax; ry += ay;
                    }
                }
                long size = (long)n1 * (2 * sizeof(double));
                Buffer.MemoryCopy(pb, a, size, size);

                // Convolution.
                Execute(subplan, a, buf);
                fixed (double* pz = &precr[offs])
                {
                    int j = 0, p0 = 0;
                    if (Avx.IsSupported)
                    {
                        int top = n1 & 0x7FFF_FFFE;
                        for (; j < top; j += 2, p0 += 4)
                        {
                            V4d v1 = Avx.LoadVector256(a + p0);
                            V4d v2 = Avx.LoadVector256(pz + p0);
                            V4d v3 = v1 * v2;
                            v2 = Avx.Permute(v2, 5) ^ CNJ;
                            v1 = Avx.HorizontalSubtract(v3, v1 * v2);
                            Avx.Store(a + p0, v1 ^ CNJ);
                        }
                    }
                    for (; j < n1; j++, p0 += 2)
                    {
                        double ax = a[p0], ay = a[p0 + 1];
                        double bx = pz[p0], by = pz[p0 + 1];
                        a[p0] = ax * bx - ay * by; a[p0 + 1] = -(ax * by + ay * bx);
                    }
                }
                Execute(subplan, a, buf);
                if (Avx.IsSupported)
                {
                    V4d d = V4.Create(1.0 / n1) ^ CNJ;
                    V4d vv = V4.Create(v, v);
                    int max = n1 & ~1, p = 0, kiq = 1;
                    Sse2.Store(pb, r);
                    for (int i = 0; i < max; i += 2, p += 4)
                    {
                        V4d aa = Avx.LoadVector256(a + p) * d;
                        Avx.Store(a + p, aa);
                        aa += vv;
                        Sse2.Store(pb + 2 * kiq, aa.GetLower());
                        kiq = kiq * riq % n;
                        Sse2.Store(pb + 2 * kiq, aa.GetUpper());
                        kiq = kiq * riq % n;
                    }
                    if (max != n1)
                    {
                        var aa = Sse2.LoadVector128(a + p) * d.GetLower();
                        Sse2.Store(a + p, aa);
                        Sse2.Store(pb + 2 * kiq, aa + v);
                    }
                }
                else
                {
                    pb[0] = rx; pb[1] = ry;
                    for (int i = 0, p = 0, kiq = 1; i < n1; i++, p += 2, kiq = kiq * riq % n)
                        (pb[2 * kiq], pb[2 * kiq + 1])
                            = (x0 + (a[p] /= n1), y0 + (a[p + 1] /= -n1));
                }
                size += 2 * sizeof(double);
                Buffer.MemoryCopy(pb, a, size, size);
                a += n * 2;
                pb += n * 2;
            }
        }
    }

    /// <summary>Applies complex Bluestein's FFT to array <paramref name="a"/>.</summary>
    /// <param name="a">The input/output array.</param>
    /// <param name="count">Number of repeated operands.</param>
    /// <param name="n">Original data length.</param>
    /// <param name="subplan">Subplan used by the transformation.</param>
    /// <param name="m">Padded data length.</param>
    /// <param name="offs">Offset of the precomputed sequence.</param>
    [SkipLocalsInit]
    private unsafe void ApplyBluestein(
        double* a, int count, int n, int subplan, int m, int offs)
    {
#pragma warning disable IDE0302 // Simplify collection initialization
        Span<double> bufa = stackalloc double[0];
        Span<double> bufb = stackalloc double[0];
#pragma warning restore IDE0302 // Simplify collection initialization
        double[]? bA = null, bB = null;
        if (pool.ArraySize <= 150)
        {
            bufa = stackalloc double[pool.ArraySize];
            bufb = stackalloc double[pool.ArraySize];
        }
        else if (2 * pool.ArraySize < buffer.Length)
        {
            bufa = buffer.AsSpan(0, pool.ArraySize);
            bufb = buffer.AsSpan(pool.ArraySize, pool.ArraySize);
        }
        else
        {
            (bA, bB) = pool.Rent();
            bufa = bA; bufb = bB;
        }
        var dm = V4.Create((double)m);
        fixed (double* b = bufa, z = &precr[offs])
            for (int op = 0; op < count; op++)
            {
                // B = A * conj(Z), pad B with zeros. Z[k] = exp(iπ*k^2/N)
                int i = 0, p0 = 0;
                if (Avx.IsSupported)
                {
                    int top = n & 0x7FFF_FFFE;
                    for (; i < top; i += 2, p0 += 4)
                    {
                        var v1 = Avx.LoadVector256(z + p0);
                        var v2 = Avx.LoadVector256(a + p0);
                        var v3 = v1 * v2;
                        v2 = Avx.Permute(v2, 5) ^ CNJ;
                        Avx.Store(b + p0, Avx.HorizontalAdd(v3, v1 * v2));
                    }
                }
                for (; i < n; i++, p0 += 2)
                {
                    double x = a[p0], y = a[p0 + 1];
                    double bx = z[p0], by = -z[p0 + 1];
                    b[p0] = x * bx - y * by; b[p0 + 1] = x * by + y * bx;
                }
                bufa.Slice(2 * n, 2 * (m - n)).Clear();

                // Perform convolution of A and Z.
                Execute(subplan, b, bufb);
                i = 0; p0 = 0;
                int p1 = 2 * m;
                if (Avx.IsSupported)
                {
                    int top = n & 0x7FFF_FFFE;
                    for (; i < top; i += 2, p0 += 4, p1 += 4)
                    {
                        var v1 = Avx.LoadVector256(b + p0);
                        var v2 = Avx.LoadVector256(z + p1);
                        var v3 = v1 * v2;
                        v2 = Avx.Permute(v2, 5) ^ CNJ;
                        v1 = Avx.HorizontalSubtract(v3, v1 * v2);
                        Avx.Store(b + p0, v1 ^ CNJ);
                    }
                }
                for (; i < m; i++, p0 += 2, p1 += 2)
                {
                    double ax = b[p0], ay = b[p0 + 1];
                    double bx = z[p1], by = z[p1 + 1];
                    b[p0] = ax * bx - ay * by; b[p0 + 1] = -(ax * by + ay * bx);
                }
                Execute(subplan, b, bufb);

                // Post processing: A= conj(Z)*conj(A)/M
                i = 0; p0 = 0;
                if (Avx.IsSupported)
                {
                    int top = n & 0x7FFF_FFFE;
                    for (; i < top; i += 2, p0 += 4)
                    {
                        var v1 = Avx.LoadVector256(z + p0);
                        var v2 = Avx.LoadVector256(b + p0) / dm;
                        var v3 = v1 * v2;
                        v2 = Avx.Permute(v2, 5) ^ CNJ;
                        v1 = Avx.HorizontalSubtract(v3, v1 * v2);
                        Avx.Store(a + p0, v1 ^ CNJ);
                    }
                }
                for (; i < n; i++, p0 += 2)
                {
                    double bx = z[p0], by = z[p0 + 1];
                    double rx = b[p0] / m, ry = -(b[p0 + 1] / m);
                    a[p0] = rx * bx - ry * -by; a[p0 + 1] = rx * -by + ry * bx;
                }
                a += n + n;
            }
        if (bA != null)
            pool.Return(bA, bB!);
    }

    /// <summary>Applies a complex codelet FFT to array <paramref name="a"/>.</summary>
    /// <param name="a">The input/output array.</param>
    /// <param name="count">Operands count.</param>
    /// <param name="size">Operands size.</param>
    private static unsafe void ComplexCodelet(double* a, int count, int size)
    {
        switch (size)
        {
            case 2:
                for (; count-- > 0; a += 4)
                {
                    double a0x = a[0], a0y = a[1];
                    double a1x = a[2], a1y = a[3];
                    a[0] = a0x + a1x; a[1] = a0y + a1y;
                    a[2] = a0x - a1x; a[3] = a0y - a1y;
                }
                break;
            case 3:
                {
                    double c1 = Cos2πOver3 - 1, c2 = Sin2πOver3;
                    var c1v = Vector128.Create(c1);
                    var c2v = Vector128.Create(c2, -c2);
                    for (; count-- > 0; a += 6)
                        if (Fma.IsSupported)
                        {
                            var a0 = Sse2.LoadVector128(a);
                            var a1 = Sse2.LoadVector128(a + 2);
                            var a2 = Sse2.LoadVector128(a + 4);
                            var t1 = Sse2.Add(a1, a2);
                            a0 = Sse2.Add(a0, t1);
                            var m2 = Sse2.Multiply(Avx.Permute(Sse2.Subtract(a1, a2), 5), c2v);
                            var s1 = Fma.MultiplyAdd(c1v, t1, a0);
                            Sse2.Store(a, a0);
                            Sse2.Store(a + 2, Sse2.Add(s1, m2));
                            Sse2.Store(a + 4, Sse2.Subtract(s1, m2));
                        }
                        else
                        {
                            double a0x = a[0], a0y = a[1];
                            double a1x = a[2], a1y = a[3];
                            double a2x = a[4], a2y = a[5];
                            double t1x = a1x + a2x, t1y = a1y + a2y;
                            a0x += t1x; a0y += t1y;
                            double m2x = c2 * (a1y - a2y), m2y = c2 * (a2x - a1x);
                            double s1x = a0x + c1 * t1x, s1y = a0y + c1 * t1y;
                            a[0] = a0x; a[1] = a0y;
                            a[2] = s1x + m2x; a[3] = s1y + m2y;
                            a[4] = s1x - m2x; a[5] = s1y - m2y;
                        }
                }
                break;
            case 4:
                for (; count-- > 0; a += 8)
                    if (Avx.IsSupported)
                    {
                        var a0 = Sse2.LoadVector128(a);
                        var a1 = Sse2.LoadVector128(a + 2);
                        var a2 = Sse2.LoadVector128(a + 4);
                        var a3 = Sse2.LoadVector128(a + 6);
                        var t1 = Sse2.Add(a0, a2);
                        var t2 = Sse2.Add(a1, a3);
                        var m2 = Sse2.Subtract(a0, a2);
                        var m3 = Sse2.Xor(Avx.Permute(Sse2.Subtract(a1, a3), 5), cnj);
                        Sse2.Store(a, Sse2.Add(t1, t2));
                        Sse2.Store(a + 2, Sse2.Add(m2, m3));
                        Sse2.Store(a + 4, Sse2.Subtract(t1, t2));
                        Sse2.Store(a + 6, Sse2.Subtract(m2, m3));
                    }
                    else
                    {
                        double a0x = a[0], a0y = a[1];
                        double a1x = a[2], a1y = a[3];
                        double a2x = a[4], a2y = a[5];
                        double a3x = a[6], a3y = a[7];
                        double t1x = a0x + a2x, t1y = a0y + a2y;
                        double t2x = a1x + a3x, t2y = a1y + a3y;
                        double m2x = a0x - a2x, m2y = a0y - a2y;
                        double m3x = a1y - a3y, m3y = a3x - a1x;
                        a[0] = t1x + t2x; a[1] = t1y + t2y;
                        a[4] = t1x - t2x; a[5] = t1y - t2y;
                        a[2] = m2x + m3x; a[3] = m2y + m3y;
                        a[6] = m2x - m3x; a[7] = m2y - m3y;
                    }
                break;
            case 5:
                {
                    double c1 = (Cos2πOver5 + Cos4πOver5) / 2 - 1;
                    double c2 = (Cos2πOver5 - Cos4πOver5) / 2;
                    double c3 = -Sin2πOver5;
                    double c4 = -(Sin2πOver5 + Sin4πOver5), c5 = Sin2πOver5 - Sin4πOver5;
                    var c1v = Vector128.Create(c1);
                    var c2v = Vector128.Create(c2);
                    var c3v = Vector128.Create(c3, -c3);
                    var c4v = Vector128.Create(c4, -c4);
                    var c5v = Vector128.Create(-c5, c5);
                    for (; count-- > 0; a += 10)
                        if (Fma.IsSupported)
                        {
                            var a0 = Sse2.LoadVector128(a);
                            var a1 = Sse2.LoadVector128(a + 2);
                            var a2 = Sse2.LoadVector128(a + 4);
                            var a3 = Sse2.LoadVector128(a + 6);
                            var a4 = Sse2.LoadVector128(a + 8);
                            var t1 = Sse2.Add(a1, a4);
                            var t2 = Sse2.Add(a2, a3);
                            var t3 = Sse2.Subtract(a1, a4);
                            var t4 = Sse2.Subtract(a3, a2);
                            var t5 = Sse2.Add(t1, t2);
                            a0 = Sse2.Add(a0, t5);
                            var m2 = Sse2.Multiply(c2v, Sse2.Subtract(t1, t2));
                            var m3 = Avx.Permute(Sse2.Multiply(Sse2.Add(t3, t4), c3v), 5);
                            var s3 = Fma.MultiplyAdd(Avx.Permute(t4, 5), c4v, m3);
                            var s5 = Fma.MultiplyAdd(Avx.Permute(t3, 5), c5v, m3);
                            var s1 = Fma.MultiplyAdd(t5, c1v, a0);
                            var s2 = Sse2.Add(s1, m2);
                            var s4 = Sse2.Subtract(s1, m2);
                            Sse2.Store(a, a0);
                            Sse2.Store(a + 2, Sse2.Add(s2, s3));
                            Sse2.Store(a + 4, Sse2.Add(s4, s5));
                            Sse2.Store(a + 6, Sse2.Subtract(s4, s5));
                            Sse2.Store(a + 8, Sse2.Subtract(s2, s3));
                        }
                        else
                        {
                            double t1x = a[2] + a[8], t1y = a[3] + a[9];
                            double t2x = a[4] + a[6], t2y = a[5] + a[7];
                            double t3x = a[2] - a[8], t3y = a[3] - a[9];
                            double t4x = a[6] - a[4], t4y = a[7] - a[5];
                            double t5x = t1x + t2x, t5y = t1y + t2y;
                            a[0] += t5x; a[1] += t5y;
                            double m2x = c2 * (t1x - t2x), m2y = c2 * (t1y - t2y);
                            double m3x = -c3 * (t3y + t4y), m3y = c3 * (t3x + t4x);
                            double s3x = m3x + c4 * t4y, s3y = m3y - c4 * t4x;
                            double s5x = m3x - c5 * t3y, s5y = m3y + c5 * t3x;
                            double s1x = a[0] + c1 * t5x, s1y = a[1] + c1 * t5y;
                            double s2x = s1x + m2x, s2y = s1y + m2y;
                            double s4x = s1x - m2x, s4y = s1y - m2y;
                            a[2] = s2x + s3x; a[3] = s2y + s3y;
                            a[4] = s4x + s5x; a[5] = s4y + s5y;
                            a[6] = s4x - s5x; a[7] = s4y - s5y;
                            a[8] = s2x - s3x; a[9] = s2y - s3y;
                        }
                }
                break;
            case 6:
                {
                    double c1 = Cos2πOver3 - 1, c2 = Sin2πOver3, c3 = CosπOver3, c4 = SinπOver3;
                    var c1v = Vector128.Create(c1);
                    var c2v = Vector128.Create(-c2, c2);
                    var c3v = Vector128.Create(c3);
                    var c4v = Vector128.Create(-c4, c4);
                    for (; count-- > 0; a += 12)
                        if (Fma.IsSupported)
                        {
                            var a0 = Sse2.LoadVector128(a);
                            var a1 = Sse2.LoadVector128(a + 2);
                            var a2 = Sse2.LoadVector128(a + 4);
                            var a3 = Sse2.LoadVector128(a + 6);
                            var a4 = Sse2.LoadVector128(a + 8);
                            var a5 = Sse2.LoadVector128(a + 10);
                            var v = a0; a0 = Sse2.Add(a0, a3); a3 = Sse2.Subtract(v, a3);
                            v = a1; a1 = Sse2.Add(a1, a4); a4 = Sse2.Subtract(v, a4);
                            v = a2; a2 = Sse2.Add(a2, a5); a5 = Sse2.Subtract(v, a5);
                            var t4 = Fma.MultiplyAdd(a4, c3v,
                                Sse2.Multiply(Avx.Permute(a4, 5), c4v));
                            var t5 = Fma.MultiplySubtract(
                                Avx.Permute(a5, 5), c4v, Sse2.Multiply(a5, c3v));
                            var t1 = Sse2.Add(a1, a2);
                            a0 = Sse2.Add(a0, t1);
                            var m2 = Sse2.Multiply(Avx.Permute(Sse2.Subtract(a2, a1), 5), c2v);
                            var s1 = Fma.MultiplyAdd(t1, c1v, a0);
                            a1 = Sse2.Add(s1, m2);
                            a2 = Sse2.Subtract(s1, m2);
                            t1 = Sse2.Add(t4, t5);
                            a3 = Sse2.Add(a3, t1);
                            m2 = Sse2.Multiply(Avx.Permute(Sse2.Subtract(t5, t4), 5), c2v);
                            s1 = Fma.MultiplyAdd(t1, c1v, a3);
                            Sse2.Store(a, a0);
                            Sse2.Store(a + 2, a3);
                            Sse2.Store(a + 4, a1);
                            Sse2.Store(a + 6, Sse2.Add(s1, m2));
                            Sse2.Store(a + 8, a2);
                            Sse2.Store(a + 10, Sse2.Subtract(s1, m2));
                        }
                        else
                        {
                            double a0x = a[0], a0y = a[1];
                            double a1x = a[2], a1y = a[3];
                            double a2x = a[4], a2y = a[5];
                            double a3x = a[6], a3y = a[7];
                            double a4x = a[8], a4y = a[9];
                            double a5x = a[10], a5y = a[11];
                            double v0 = a0x, v1 = a0y;
                            a0x += a3x; a0y += a3y;
                            a3x = v0 - a3x; a3y = v1 - a3y;
                            v0 = a1x; v1 = a1y;
                            a1x += a4x; a1y += a4y;
                            a4x = v0 - a4x; a4y = v1 - a4y;
                            v0 = a2x; v1 = a2y;
                            a2x += a5x; a2y += a5y;
                            a5x = v0 - a5x; a5y = v1 - a5y;
                            double t4x = a4x * c3 - a4y * c4;
                            double t4y = a4y * c3 + a4x * c4;
                            double t5x = -a5x * c3 - a5y * c4;
                            double t5y = -a5y * c3 + a5x * c4;
                            double t1x = a1x + a2x, t1y = a1y + a2y;
                            a0x += t1x; a0y += t1y;
                            double m2x = c2 * (a1y - a2y), m2y = c2 * (a2x - a1x);
                            double s1x = a0x + c1 * t1x, s1y = a0y + c1 * t1y;
                            a1x = s1x + m2x; a1y = s1y + m2y;
                            a2x = s1x - m2x; a2y = s1y - m2y;
                            t1x = t4x + t5x; t1y = t4y + t5y;
                            a3x += t1x; a3y += t1y;
                            m2x = c2 * (t4y - t5y); m2y = c2 * (t5x - t4x);
                            s1x = a3x + c1 * t1x; s1y = a3y + c1 * t1y;
                            a[0] = a0x; a[1] = a0y;
                            a[2] = a3x; a[3] = a3y;
                            a[4] = a1x; a[5] = a1y;
                            a[6] = s1x + m2x; a[7] = s1y + m2y;
                            a[8] = a2x; a[9] = a2y;
                            a[10] = s1x - m2x; a[11] = s1y - m2y;
                        }
                }
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Sqr(double X) => X * X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe static void StoreTwiddled(
        double* target, in Vector128<double> vector, double twx, double twy)
    {
        if (Fma.IsSupported)
            Sse2.Store(target, Fma.MultiplyAdd(
                vector, Vector128.Create(twx), Sse2.Multiply(
                Avx.Permute(vector, 5), Vector128.Create(-twy, twy))));
    }

    /// <summary>Integrated codelet for size 2.</summary>
    /// <param name="a">Input/output array.</param>
    /// <param name="count">Operands count.</param>
    /// <param name="mv">Micro-vector size.</param>
    private static unsafe void Codelet2(double* a, int count, int mv)
    {
        int m = mv / 2;
        double θ = -PI / m;
        double tw0 = -2 * Sqr(Sin(0.5 * θ)), tw1 = Sin(θ);
        do
        {
            if (Sse2.IsSupported)
            {
                var a0 = Sse2.LoadVector128(a);
                var a1 = Sse2.LoadVector128(a + mv);
                Sse2.Store(a, Sse2.Add(a0, a1));
                Sse2.Store(a + mv, Sse2.Subtract(a0, a1));
            }
            else
            {
                double a0x = a[0], a0y = a[1];
                double a1x = a[mv], a1y = a[mv + 1];
                a[0] = a0x + a1x; a[1] = a0y + a1y;
                a[mv] = a0x - a1x; a[mv + 1] = a0y - a1y;
            }
            double* p = a + 2;
            double twx = 1 + tw0, twy = tw1;
            for (int j = 1; j < m; j++, p += 2)
            {
                if (Fma.IsSupported)
                {
                    var a0 = Sse2.LoadVector128(p);
                    var a1 = Sse2.LoadVector128(p + mv);
                    Sse2.Store(p, Sse2.Add(a0, a1));
                    var v2 = Sse2.Subtract(a0, a1);
                    StoreTwiddled(p + mv, v2, twx, twy);
                }
                else
                {
                    double a0x = p[0], a0y = p[1];
                    double a1x = p[mv], a1y = p[mv + 1];
                    double v2 = a0x - a1x, v3 = a0y - a1y;
                    p[0] = a0x + a1x; p[1] = a0y + a1y;
                    p[mv] = v2 * twx - v3 * twy; p[mv + 1] = v3 * twx + v2 * twy;
                }
                if ((j + 1) % UPDATE_TW == 0)
                {
                    θ = -PI * (j + 1) / m;
                    (twx, twy) = (1.0 - 2 * Sqr(Sin(0.5 * θ)), Sin(θ));
                }
                else
                {
                    θ = twx * tw0 - twy * tw1;
                    twy += twx * tw1 + twy * tw0;
                    twx += θ;
                }
            }
            a += 2 * mv;
        } while (--count > 0);
    }

    /// <summary>Integrated codelet for size 3.</summary>
    /// <param name="a">Input/output array.</param>
    /// <param name="count">Operands count.</param>
    /// <param name="mv">Micro-vector size.</param>
    private static unsafe void Codelet3(double* a, int count, int mv)
    {
        int m = mv / 2;
        double θ = -Tau / (3 * m);
        double tw0 = -2 * Sqr(Sin(0.5 * θ)), tw1 = Sin(θ);
        double c1 = Cos2πOver3 - 1, c2 = Sin2πOver3;
        var c1v = Vector128.Create(c1);
        var c2v = Vector128.Create(c2, -c2);
        int off4 = mv + mv;
        do
        {
            if (Fma.IsSupported)
            {
                var a0 = Sse2.LoadVector128(a);
                var a1 = Sse2.LoadVector128(a + mv);
                var a2 = Sse2.LoadVector128(a + off4);
                var t1 = Sse2.Add(a1, a2);
                a0 = Sse2.Add(a0, t1);
                var s1 = Fma.MultiplyAdd(t1, c1v, a0);
                var s2 = Sse2.Multiply(Avx.Permute(Sse2.Subtract(a1, a2), 5), c2v);
                Sse2.Store(a, a0);
                Sse2.Store(a + mv, Sse2.Add(s1, s2));
                Sse2.Store(a + off4, Sse2.Subtract(s1, s2));
            }
            else
            {
                double a0x = a[0], a0y = a[1];
                double a1x = a[mv], a1y = a[mv + 1];
                double a2x = a[off4], a2y = a[off4 + 1];
                double t1x = a1x + a2x, t1y = a1y + a2y;
                a0x += t1x; a0y += t1y;
                double s1x = a0x + c1 * t1x, s1y = a0y + c1 * t1y;
                double s2x = c2 * (a1y - a2y), s2y = c2 * (a2x - a1x);
                a[0] = a0x; a[1] = a0y;
                a[mv] = s1x + s2x; a[mv + 1] = s1y + s2y;
                a[off4] = s1x - s2x; a[off4 + 1] = s1y - s2y;
            }
            double* p = a + 2;
            double twx = 1.0 + tw0, twy = tw1;
            for (int j = 1; j < m; j++, p += 2)
            {
                if (Fma.IsSupported)
                {
                    var a0 = Sse2.LoadVector128(p);
                    var a1 = Sse2.LoadVector128(p + mv);
                    var a2 = Sse2.LoadVector128(p + off4);
                    var t1 = Sse2.Add(a1, a2);
                    a0 = Sse2.Add(a0, t1);
                    var s1 = Fma.MultiplyAdd(t1, c1v, a0);
                    var s2 = Sse2.Multiply(Avx.Permute(Sse2.Subtract(a1, a2), 5), c2v);
                    (a1, a2) = (Sse2.Add(s1, s2), Sse2.Subtract(s1, s2));
                    double tw2x = (twx + twy) * (twx - twy), tw2y = 2 * twx * twy;
                    Sse2.Store(p, a0);
                    StoreTwiddled(p + mv, a1, twx, twy);
                    StoreTwiddled(p + off4, a2, tw2x, tw2y);
                }
                else
                {
                    double a0x = p[0], a0y = p[1];
                    double a1x = p[mv], a1y = p[mv + 1];
                    double a2x = p[off4], a2y = p[off4 + 1];
                    double t1x = a1x + a2x, t1y = a1y + a2y;
                    a0x += t1x; a0y += t1y;
                    double m1x = c1 * t1x, m1y = c1 * t1y;
                    double s1x = a0x + m1x, s1y = a0y + m1y;
                    double s2x = c2 * (a1y - a2y), s2y = c2 * (a2x - a1x);
                    a1x = s1x + s2x; a1y = s1y + s2y;
                    a2x = s1x - s2x; a2y = s1y - s2y;
                    double tw2x = (twx + twy) * (twx - twy), tw2y = 2 * twx * twy;
                    p[0] = a0x; p[1] = a0y;
                    p[mv] = a1x * twx - a1y * twy; p[mv + 1] = a1y * twx + a1x * twy;
                    p[off4] = a2x * tw2x - a2y * tw2y; p[off4 + 1] = a2y * tw2x + a2x * tw2y;
                }
                if ((j + 1) % UPDATE_TW == 0)
                {
                    θ = -Tau * (j + 1) / (3 * m);
                    (twx, twy) = (1.0 - 2 * Sqr(Sin(0.5 * θ)), Sin(θ));
                }
                else
                {
                    θ = twx * tw0 - twy * tw1;
                    twy += twx * tw1 + twy * tw0;
                    twx += θ;
                }
            }
            a += 3 * mv;
        } while (--count > 0);
    }

    /// <summary>Integrated codelet for size 4.</summary>
    /// <param name="a">Input/output array.</param>
    /// <param name="count">Operands count.</param>
    /// <param name="mv">Micro-vector size.</param>
    private static unsafe void Codelet4(double* a, int count, int mv)
    {
        int m = mv / 2;
        double θ = -PI / mv;
        double tw0 = -2 * Sqr(Sin(0.5 * θ)), tw1 = Sin(θ);
        int off4 = mv + mv, off6 = off4 + mv;
        do
        {
            if (Fma.IsSupported)
            {
                var a0 = Sse2.LoadVector128(a);
                var a1 = Sse2.LoadVector128(a + mv);
                var a2 = Sse2.LoadVector128(a + off4);
                var a3 = Sse2.LoadVector128(a + off6);
                var t1 = Sse2.Add(a0, a2);
                var t2 = Sse2.Add(a1, a3);
                var m2 = Sse2.Subtract(a0, a2);
                var m3 = Sse2.Xor(Avx.Permute(Sse2.Subtract(a1, a3), 5), cnj);
                Sse2.Store(a, Sse2.Add(t1, t2));
                Sse2.Store(a + mv, Sse2.Add(m2, m3));
                Sse2.Store(a + off4, Sse2.Subtract(t1, t2));
                Sse2.Store(a + off6, Sse2.Subtract(m2, m3));
            }
            else
            {
                double a0x = a[0], a0y = a[1];
                double a1x = a[mv], a1y = a[mv + 1];
                double a2x = a[off4], a2y = a[off4 + 1];
                double a3x = a[off6], a3y = a[off6 + 1];
                double t1x = a0x + a2x, t1y = a0y + a2y;
                double t2x = a1x + a3x, t2y = a1y + a3y;
                double m2x = a0x - a2x, m2y = a0y - a2y;
                double m3x = a1y - a3y, m3y = a3x - a1x;
                a[0] = t1x + t2x; a[1] = t1y + t2y;
                a[mv] = m2x + m3x; a[mv + 1] = m2y + m3y;
                a[off4] = t1x - t2x; a[off4 + 1] = t1y - t2y;
                a[off6] = m2x - m3x; a[off6 + 1] = m2y - m3y;
            }
            double twx = 1.0 + tw0, twy = tw1;
            double* p = a + 2;
            for (int j = 1; j < m; j++, p += 2)
            {
                if (Fma.IsSupported)
                {
                    var a0 = Sse2.LoadVector128(p);
                    var a1 = Sse2.LoadVector128(p + mv);
                    var a2 = Sse2.LoadVector128(p + off4);
                    var a3 = Sse2.LoadVector128(p + off6);
                    var t1 = Sse2.Add(a0, a2);
                    var t2 = Sse2.Add(a1, a3);
                    var m1 = Sse2.Subtract(a0, a2);
                    var m2 = Sse2.Xor(Avx.Permute(Sse2.Subtract(a1, a3), 5), cnj);
                    double tw2x = (twx + twy) * (twx - twy);
                    double tw2y = 2 * twx * twy;
                    double tw3x = twx * tw2x - twy * tw2y;
                    double tw3y = twx * tw2y + twy * tw2x;
                    Sse2.Store(p, Sse2.Add(t1, t2));
                    StoreTwiddled(p + mv, Sse2.Add(m1, m2), twx, twy);
                    StoreTwiddled(p + off4, Sse2.Subtract(t1, t2), tw2x, tw2y);
                    StoreTwiddled(p + off6, Sse2.Subtract(m1, m2), tw3x, tw3y);
                }
                else
                {
                    double a0x = p[0], a0y = p[1];
                    double a1x = p[mv], a1y = p[mv + 1];
                    double a2x = p[off4], a2y = p[off4 + 1];
                    double a3x = p[off6], a3y = p[off6 + 1];
                    double t1x = a0x + a2x, t1y = a0y + a2y;
                    double t2x = a1x + a3x, t2y = a1y + a3y;
                    double m2x = a0x - a2x, m2y = a0y - a2y;
                    double m3x = a1y - a3y, m3y = a3x - a1x;
                    double tw2x = (twx + twy) * (twx - twy), tw2y = 2 * twx * twy;
                    double tw3x = twx * tw2x - twy * tw2y;
                    double tw3y = twx * tw2y + twy * tw2x;
                    a1x = m2x + m3x; a1y = m2y + m3y;
                    a2x = t1x - t2x; a2y = t1y - t2y;
                    a3x = m2x - m3x; a3y = m2y - m3y;
                    p[0] = t1x + t2x; p[1] = t1y + t2y;
                    p[mv] = a1x * twx - a1y * twy; p[mv + 1] = a1y * twx + a1x * twy;
                    p[off4] = a2x * tw2x - a2y * tw2y; p[off4 + 1] = a2y * tw2x + a2x * tw2y;
                    p[off6] = a3x * tw3x - a3y * tw3y; p[off6 + 1] = a3y * tw3x + a3x * tw3y;
                }
                if ((j + 1) % UPDATE_TW == 0)
                {
                    θ = -PI * (j + 1) / mv;
                    (twx, twy) = (1.0 - 2 * Sqr(Sin(0.5 * θ)), Sin(θ));
                }
                else
                {
                    θ = twx * tw0 - twy * tw1;
                    twy += twx * tw1 + twy * tw0;
                    twx += θ;
                }
            }
            a += 4 * mv;
        } while (--count > 0);
    }

    /// <summary>Integrated codelet for size 5.</summary>
    /// <param name="a">Input/output array.</param>
    /// <param name="count">Operands count.</param>
    /// <param name="mv">Micro-vector size.</param>
    private static unsafe void Codelet5(double* a, int count, int mv)
    {
        int m = mv / 2;
        double θ = -Tau / (5 * m);
        double tw0 = -2 * Sqr(Sin(0.5 * θ)), tw1 = Sin(θ);
        double c1 = (Cos2πOver5 + Cos4πOver5) / 2 - 1, c2 = (Cos2πOver5 - Cos4πOver5) / 2;
        double c3 = -Sin2πOver5;
        double c4 = -(Sin2πOver5 + Sin4πOver5), c5 = Sin2πOver5 - Sin4πOver5;
        var c1v = Vector128.Create(c1);
        var c2v = Vector128.Create(c2);
        var c3v = Vector128.Create(c3, -c3);
        var c4v = Vector128.Create(c4, -c4);
        var c5v = Vector128.Create(c5, -c5);
        int off4 = mv + mv, off6 = off4 + mv, off8 = off6 + mv;
        do
        {
            if (Fma.IsSupported)
            {
                var a0 = Sse2.LoadVector128(a);
                var a1 = Sse2.LoadVector128(a + mv);
                var a2 = Sse2.LoadVector128(a + off4);
                var a3 = Sse2.LoadVector128(a + off6);
                var a4 = Sse2.LoadVector128(a + off8);
                var (t1, t2) = (Sse2.Add(a1, a4), Sse2.Add(a2, a3));
                var (t3, t4) = (Sse2.Subtract(a1, a4), Sse2.Subtract(a3, a2));
                var t5 = Sse2.Add(t1, t2);
                a0 = Sse2.Add(a0, t5);
                var m1 = Avx.Permute(Sse2.Multiply(Sse2.Add(t3, t4), c3v), 5);
                var s3 = Sse2.Subtract(m1, Avx.Permute(Sse2.Multiply(t4, c4v), 5));
                var s5 = Sse2.Add(Avx.Permute(Sse2.Multiply(t3, c5v), 5), m1);
                var s1 = Fma.MultiplyAdd(t5, c1v, a0);
                m1 = Sse2.Multiply(Sse2.Subtract(t1, t2), c2v);
                var (s2, s4) = (Sse2.Add(s1, m1), Sse2.Subtract(s1, m1));
                Sse2.Store(a, a0);
                Sse2.Store(a + mv, Sse2.Add(s2, s3));
                Sse2.Store(a + off4, Sse2.Add(s4, s5));
                Sse2.Store(a + off6, Sse2.Subtract(s4, s5));
                Sse2.Store(a + off8, Sse2.Subtract(s2, s3));
            }
            else
            {
                double a0x = a[0], a0y = a[1];
                double a1x = a[mv], a1y = a[mv + 1];
                double a2x = a[off4], a2y = a[off4 + 1];
                double a3x = a[off6], a3y = a[off6 + 1];
                double a4x = a[off8], a4y = a[off8 + 1];
                double t1x = a1x + a4x, t1y = a1y + a4y;
                double t2x = a2x + a3x, t2y = a2y + a3y;
                double t3x = a1x - a4x, t3y = a1y - a4y;
                double t4x = a3x - a2x, t4y = a3y - a2y;
                double t5x = t1x + t2x, t5y = t1y + t2y;
                a0x += t5x; a0y += t5y;
                double m1x = -c3 * (t3y + t4y), m1y = c3 * (t3x + t4x);
                double s3x = m1x + c4 * t4y, s3y = m1y - c4 * t4x;
                double s5x = m1x - c5 * t3y, s5y = m1y + c5 * t3x;
                double s1x = c1 * t5x + a0x, s1y = c1 * t5y + a0y;
                m1x = c2 * (t1x - t2x); m1y = c2 * (t1y - t2y);
                double s2x = s1x + m1x, s2y = s1y + m1y;
                double s4x = s1x - m1x, s4y = s1y - m1y;
                a[0] = a0x; a[1] = a0y;
                a[mv] = s2x + s3x; a[mv + 1] = s2y + s3y;
                a[off4] = s4x + s5x; a[off4 + 1] = s4y + s5y;
                a[off6] = s4x - s5x; a[off6 + 1] = s4y - s5y;
                a[off8] = s2x - s3x; a[off8 + 1] = s2y - s3y;
            }
            double twx = 1.0 + tw0, twy = tw1;
            double* p = a + 2;
            for (int j = 1; j < m; j++, p += 2)
            {
                if (Fma.IsSupported)
                {
                    var a0 = Sse2.LoadVector128(p);
                    var a1 = Sse2.LoadVector128(p + mv);
                    var a2 = Sse2.LoadVector128(p + off4);
                    var a3 = Sse2.LoadVector128(p + off6);
                    var a4 = Sse2.LoadVector128(p + off8);
                    var (t1, t2) = (Sse2.Add(a1, a4), Sse2.Add(a2, a3));
                    var (t3, t4) = (Sse2.Subtract(a1, a4), Sse2.Subtract(a3, a2));
                    var t5 = Sse2.Add(t1, t2);
                    a0 = Sse2.Add(a0, t5);
                    var m1 = Avx.Permute(Sse2.Multiply(Sse2.Add(t3, t4), c3v), 5);
                    var s3 = Sse2.Subtract(m1, Avx.Permute(Sse2.Multiply(t4, c4v), 5));
                    var s5 = Sse2.Add(Avx.Permute(Sse2.Multiply(t3, c5v), 5), m1);
                    var s1 = Fma.MultiplyAdd(t5, c1v, a0);
                    m1 = Sse2.Multiply(Sse2.Subtract(t1, t2), c2v);
                    var (s2, s4) = (Sse2.Add(s1, m1), Sse2.Subtract(s1, m1));
                    double tw2x = (twx + twy) * (twx - twy), tw2y = 2 * twx * twy;
                    double tw3x = twx * tw2x - twy * tw2y;
                    double tw3y = twx * tw2y + twy * tw2x;
                    double tw4x = (tw2x + tw2y) * (tw2x - tw2y), tw4y = 2 * tw2x * tw2y;
                    a1 = Sse2.Add(s2, s3); a2 = Sse2.Add(s4, s5);
                    a3 = Sse2.Subtract(s4, s5); a4 = Sse2.Subtract(s2, s3);
                    Sse2.Store(p, a0);
                    StoreTwiddled(p + mv, a1, twx, twy);
                    StoreTwiddled(p + off4, a2, tw2x, tw2y);
                    StoreTwiddled(p + off6, a3, tw3x, tw3y);
                    StoreTwiddled(p + off8, a4, tw4x, tw4y);
                }
                else
                {
                    double a0x = p[0], a0y = p[1];
                    double a1x = p[mv], a1y = p[mv + 1];
                    double a2x = p[off4], a2y = p[off4 + 1];
                    double a3x = p[off6], a3y = p[off6 + 1];
                    double a4x = p[off8], a4y = p[off8 + 1];
                    double t1x = a1x + a4x, t1y = a1y + a4y;
                    double t2x = a2x + a3x, t2y = a2y + a3y;
                    double t3x = a1x - a4x, t3y = a1y - a4y;
                    double t4x = a3x - a2x, t4y = a3y - a2y;
                    double t5x = t1x + t2x, t5y = t1y + t2y;
                    double q0x = a0x + t5x, q0y = a0y + t5y;
                    double m1x = -c3 * (t3y + t4y), m1y = c3 * (t3x + t4x);
                    double s3x = m1x + c4 * t4y, s3y = m1y - c4 * t4x;
                    double s5x = m1x - c5 * t3y, s5y = m1y + c5 * t3x;
                    double s1x = q0x + c1 * t5x, s1y = q0y + c1 * t5y;
                    m1x = c2 * (t1x - t2x); m1y = c2 * (t1y - t2y);
                    double s2x = s1x + m1x, s2y = s1y + m1y;
                    double s4x = s1x - m1x, s4y = s1y - m1y;
                    double tw2x = (twx + twy) * (twx - twy), tw2y = 2 * twx * twy;
                    double tw3x = twx * tw2x - twy * tw2y;
                    double tw3y = twx * tw2y + twy * tw2x;
                    double tw4x = (tw2x + tw2y) * (tw2x - tw2y), tw4y = 2 * tw2x * tw2y;
                    a1x = s2x + s3x; a1y = s2y + s3y;
                    a2x = s4x + s5x; a2y = s4y + s5y;
                    a3x = s4x - s5x; a3y = s4y - s5y;
                    a4x = s2x - s3x; a4y = s2y - s3y;
                    p[0] = q0x; p[1] = q0y;
                    p[mv] = a1x * twx - a1y * twy; p[mv + 1] = a1x * twy + a1y * twx;
                    p[off4] = a2x * tw2x - a2y * tw2y; p[off4 + 1] = a2x * tw2y + a2y * tw2x;
                    p[off6] = a3x * tw3x - a3y * tw3y; p[off6 + 1] = a3x * tw3y + a3y * tw3x;
                    p[off8] = a4x * tw4x - a4y * tw4y; p[off8 + 1] = a4x * tw4y + a4y * tw4x;
                }
                if ((j + 1) % UPDATE_TW == 0)
                {
                    θ = -Tau * (j + 1) / (5 * m);
                    (twx, twy) = (1.0 - 2 * Sqr(Sin(0.5 * θ)), Sin(θ));
                }
                else
                {
                    θ = twx * tw0 - twy * tw1;
                    twy += twx * tw1 + twy * tw0;
                    twx += θ;
                }
            }
            a += 5 * mv;
        } while (--count > 0);
    }

    /// <summary>Integrated codelet for size 6.</summary>
    /// <param name="a">Input/output array.</param>
    /// <param name="count">Operands count.</param>
    /// <param name="mv">Micro-vector size.</param>
    private static unsafe void Codelet6(double* a, int count, int mv)
    {
        int m = mv / 2;
        double c1 = Cos2πOver3 - 1, c2 = Sin2πOver3, c3 = CosπOver3, c4 = SinπOver3;
        var c1v = Vector128.Create(c1);
        var c2v = Vector128.Create(-c2, c2);
        var c3v = Vector128.Create(c3);
        var c4v = Vector128.Create(-c4, c4);
        double θ = -PI / (3 * m);
        double tw0 = -2 * Sqr(Sin(0.5 * θ)), tw1 = Sin(θ);
        int off4 = mv + mv, off6 = off4 + mv, off8 = off6 + mv, off10 = off8 + mv;
        do
        {
            if (Fma.IsSupported)
            {
                var a0 = Sse2.LoadVector128(a);
                var a1 = Sse2.LoadVector128(a + mv);
                var a2 = Sse2.LoadVector128(a + off4);
                var a3 = Sse2.LoadVector128(a + off6);
                var a4 = Sse2.LoadVector128(a + off8);
                var a5 = Sse2.LoadVector128(a + off10);
                var v0 = a0; a0 = Sse2.Add(a0, a3); a3 = Sse2.Subtract(v0, a3);
                v0 = a1; a1 = Sse2.Add(a1, a4); a4 = Sse2.Subtract(v0, a4);
                v0 = a2; a2 = Sse2.Add(a2, a5); a5 = Sse2.Subtract(v0, a5);
                a4 = Fma.MultiplyAdd(a4, c3v, Sse2.Multiply(Avx.Permute(a4, 5), c4v));
                a5 = Fma.MultiplySubtract(Avx.Permute(a5, 5), c4v, Sse2.Multiply(a5, c3v));
                var t1 = Sse2.Add(a1, a2); a0 = Sse2.Add(a0, t1);
                var s1 = Fma.MultiplyAdd(t1, c1v, a0);
                var s2 = Sse2.Multiply(Avx.Permute(Sse2.Subtract(a2, a1), 5), c2v);
                a1 = Sse2.Add(s1, s2); a2 = Sse2.Subtract(s1, s2);
                t1 = Sse2.Add(a4, a5); a3 = Sse2.Add(a3, t1);
                s1 = Fma.MultiplyAdd(t1, c1v, a3);
                s2 = Sse2.Multiply(Avx.Permute(Sse2.Subtract(a5, a4), 5), c2v);
                Sse2.Store(a, a0);
                Sse2.Store(a + mv, a3);
                Sse2.Store(a + off4, a1);
                Sse2.Store(a + off6, Sse2.Add(s1, s2));
                Sse2.Store(a + off8, a2);
                Sse2.Store(a + off10, Sse2.Subtract(s1, s2));
            }
            else
            {
                double a0x = a[0], a0y = a[1];
                double a1x = a[mv], a1y = a[mv + 1];
                double a2x = a[off4], a2y = a[off4 + 1];
                double a3x = a[off6], a3y = a[off6 + 1];
                double a4x = a[off8], a4y = a[off8 + 1];
                double a5x = a[off10], a5y = a[off10 + 1];
                double v0 = a0x, v1 = a0y;
                a0x += a3x; a0y += a3y;
                a3x = v0 - a3x; a3y = v1 - a3y;
                v0 = a1x; v1 = a1y;
                a1x += a4x; a1y += a4y;
                a4x = v0 - a4x; a4y = v1 - a4y;
                v0 = a2x; v1 = a2y;
                a2x += a5x; a2y += a5y;
                a5x = v0 - a5x; a5y = v1 - a5y;
                v0 = a4x * c3 - a4y * c4;
                v1 = a4x * c4 + a4y * c3;
                a4x = v0; a4y = v1;
                v0 = -a5x * c3 - a5y * c4;
                v1 = a5x * c4 - a5y * c3;
                a5x = v0; a5y = v1;
                double t1x = a1x + a2x, t1y = a1y + a2y;
                a0x += t1x; a0y += t1y;
                double s1x = a0x + c1 * t1x, s1y = a0y + c1 * t1y;
                double s2x = c2 * (a1y - a2y), s2y = c2 * (a2x - a1x);
                a1x = s1x + s2x; a1y = s1y + s2y;
                a2x = s1x - s2x; a2y = s1y - s2y;
                t1x = a4x + a5x; t1y = a4y + a5y;
                a3x += t1x; a3y += t1y;
                s1x = a3x + c1 * t1x; s1y = a3y + c1 * t1y;
                s2x = c2 * (a4y - a5y); s2y = c2 * (a5x - a4x);
                a[0] = a0x; a[1] = a0y;
                a[mv] = a3x; a[mv + 1] = a3y;
                a[off4] = a1x; a[off4 + 1] = a1y;
                a[off6] = s1x + s2x; a[off6 + 1] = s1y + s2y;
                a[off8] = a2x; a[off8 + 1] = a2y;
                a[off10] = s1x - s2x; a[off10 + 1] = s1y - s2y;
            }
            double twx = 1.0 + tw0, twy = tw1;
            double* p = a + 2;
            for (int j = 1; j < m; j++, p += 2)
            {
                if (Fma.IsSupported)
                {
                    var a0 = Sse2.LoadVector128(p);
                    var a1 = Sse2.LoadVector128(p + mv);
                    var a2 = Sse2.LoadVector128(p + off4);
                    var a3 = Sse2.LoadVector128(p + off6);
                    var a4 = Sse2.LoadVector128(p + off8);
                    var a5 = Sse2.LoadVector128(p + off10);
                    var v0 = a0; a0 = Sse2.Add(a0, a3); a3 = Sse2.Subtract(v0, a3);
                    v0 = a1; a1 = Sse2.Add(a1, a4); a4 = Sse2.Subtract(v0, a4);
                    v0 = a2; a2 = Sse2.Add(a2, a5); a5 = Sse2.Subtract(v0, a5);
                    a4 = Fma.MultiplyAdd(a4, c3v, Sse2.Multiply(Avx.Permute(a4, 5), c4v));
                    a5 = Fma.MultiplySubtract(Avx.Permute(a5, 5), c4v, Sse2.Multiply(a5, c3v));
                    var t1 = Sse2.Add(a1, a2); a0 = Sse2.Add(a0, t1);
                    var s1 = Fma.MultiplyAdd(t1, c1v, a0);
                    var s2 = Sse2.Multiply(Avx.Permute(Sse2.Subtract(a2, a1), 5), c2v);
                    a1 = Sse2.Add(s1, s2); a2 = Sse2.Subtract(s1, s2);
                    t1 = Sse2.Add(a4, a5); a3 = Sse2.Add(a3, t1);
                    s1 = Fma.MultiplyAdd(t1, c1v, a3);
                    s2 = Sse2.Multiply(Avx.Permute(Sse2.Subtract(a5, a4), 5), c2v);
                    double tw2x = (twx + twy) * (twx - twy), tw2y = 2 * twx * twy;
                    double tw3x = twx * tw2x - twy * tw2y;
                    double tw3y = twx * tw2y + twy * tw2x;
                    double tw4x = (tw2x + tw2y) * (tw2x - tw2y), tw4y = 2 * tw2x * tw2y;
                    double tw5x = tw3x * tw2x - tw3y * tw2y;
                    double tw5y = tw3x * tw2y + tw3y * tw2x;
                    Sse2.Store(p, a0);
                    StoreTwiddled(p + mv, a3, twx, twy);
                    StoreTwiddled(p + off4, a1, tw2x, tw2y);
                    StoreTwiddled(p + off6, Sse2.Add(s1, s2), tw3x, tw3y);
                    StoreTwiddled(p + off8, a2, tw4x, tw4y);
                    StoreTwiddled(p + off10, Sse2.Subtract(s1, s2), tw5x, tw5y);
                }
                else
                {
                    double a0x = p[0], a0y = p[1];
                    double a1x = p[mv], a1y = p[mv + 1];
                    double a2x = p[off4], a2y = p[off4 + 1];
                    double a3x = p[off6], a3y = p[off6 + 1];
                    double a4x = p[off8], a4y = p[off8 + 1];
                    double a5x = p[off10], a5y = p[off10 + 1];
                    double v0 = a0x, v1 = a0y;
                    a0x += a3x; a0y += a3y;
                    a3x = v0 - a3x; a3y = v1 - a3y;
                    v0 = a1x; v1 = a1y;
                    a1x += a4x; a1y += a4y;
                    a4x = v0 - a4x; a4y = v1 - a4y;
                    v0 = a2x; v1 = a2y;
                    a2x += a5x; a2y += a5y;
                    a5x = v0 - a5x; a5y = v1 - a5y;
                    v0 = a4x * c3 - a4y * c4;
                    v1 = a4x * c4 + a4y * c3;
                    a4x = v0; a4y = v1;
                    v0 = -a5x * c3 - a5y * c4;
                    v1 = a5x * c4 - a5y * c3;
                    a5x = v0; a5y = v1;
                    double t1x = a1x + a2x, t1y = a1y + a2y;
                    a0x += t1x; a0y += t1y;
                    double s1x = a0x + c1 * t1x, s1y = a0y + c1 * t1y;
                    double s2x = c2 * (a1y - a2y), s2y = c2 * (a2x - a1x);
                    a1x = s1x + s2x; a1y = s1y + s2y;
                    a2x = s1x - s2x; a2y = s1y - s2y;
                    t1x = a4x + a5x; t1y = a4y + a5y;
                    a3x += t1x; a3y += t1y;
                    s1x = a3x + c1 * t1x; s1y = a3y + c1 * t1y;
                    s2x = c2 * (a4y - a5y); s2y = c2 * (a5x - a4x);
                    a4x = s1x + s2x; a4y = s1y + s2y;
                    a5x = s1x - s2x; a5y = s1y - s2y;
                    double tw2x = (twx + twy) * (twx - twy), tw2y = 2 * twx * twy;
                    double tw3x = twx * tw2x - twy * tw2y;
                    double tw3y = twx * tw2y + twy * tw2x;
                    double tw4x = tw2x * tw2x - tw2y * tw2y, tw4y = 2 * tw2x * tw2y;
                    double tw5x = tw3x * tw2x - tw3y * tw2y;
                    double tw5y = tw3x * tw2y + tw3y * tw2x;
                    p[0] = a0x; p[1] = a0y;
                    p[mv] = a3x * twx - a3y * twy; p[mv + 1] = a3y * twx + a3x * twy;
                    p[off4] = a1x * tw2x - a1y * tw2y; p[off4 + 1] = a1y * tw2x + a1x * tw2y;
                    p[off6] = a4x * tw3x - a4y * tw3y; p[off6 + 1] = a4y * tw3x + a4x * tw3y;
                    p[off8] = a2x * tw4x - a2y * tw4y; p[off8 + 1] = a2y * tw4x + a2x * tw4y;
                    p[off10] = a5x * tw5x - a5y * tw5y; p[off10 + 1] = a5y * tw5x + a5x * tw5y;
                }
                if ((j + 1) % UPDATE_TW == 0)
                {
                    θ = -PI * (j + 1) / (3 * m);
                    (twx, twy) = (1.0 - 2 * Sqr(Sin(0.5 * θ)), Sin(θ));
                }
                else
                {
                    θ = twx * tw0 - twy * tw1;
                    twy += twx * tw1 + twy * tw0;
                    twx += θ;
                }
            }
            a += 6 * mv;
        } while (--count > 0);
    }

    /// <summary>Multiplication by twiddle factors for complex Cooley-Tukey FFT.</summary>
    private static unsafe void TwiddleFactors(double* a, int n1, int n2)
    {
        //  for (j in 1..n2-1)
        //    for (k in 1..n1-1)
        //      m=n1*j+k;
        //      (X, Y) = a[2m];
        //      (TwX, TwY) = CosSin(-2πjk/(n1*n2));
        //      a[2m]=(X*TwX-Y*TwY, X*TwY+Y*TwX);
        const int UPDATE_TW2 = UPDATE_TW / 2;
        int n = n1 * n2, halfN1 = n1 / 2;
        double θ = -Tau / n;
        double twBaseX = 1.0 - 2 * Sqr(Sin(0.5 * θ)), twBaseY = Sin(θ);
        double twRowX = 1, twRowY = 0;
        for (int i = 0; i < n2; i++)
        {
            // Initialize twiddle factor for current row
            double twx = 1, twy = 0;
            var twRow = Vector128.Create(twRowX, twRowY);
            // This loop is unrolled to process two complex numbers at once.
            for (int j = 0; j < halfN1; j++, a += 4)
            {
                // Rotate first complex, update twiddle factor.
                // Rotate second complex and update twiddle factor conditionally.
                if (Fma.IsSupported)
                {
                    var twX = Vector128.Create(twx);
                    var twY = Vector128.Create(-twy, twy);
                    var av = Sse2.LoadVector128(a);
                    Sse2.Store(a, Fma.MultiplyAdd(av, twX,
                        Sse2.Multiply(Avx.Permute(av, 5), twY)));
                    av = Fma.MultiplyAdd(twRow, twX, Sse2.Multiply(Avx.Permute(twRow, 5), twY));
                    twx = av.ToScalar();
                    twy = av.GetElement(1);
                    av = Sse2.LoadVector128(a + 2);
                    StoreTwiddled(a + 2, av, twx, twy);
                }
                else
                {
                    double x = a[0], y = a[1];
                    a[0] = x * twx - y * twy; a[1] = x * twy + y * twx;
                    double tmpX = twx * twRowX - twy * twRowY;
                    twy = twx * twRowY + twy * twRowX;
                    twx = tmpX;
                    x = a[2]; y = a[3];
                    a[2] = x * twx - y * twy; a[3] = x * twy + y * twx;
                }
                if ((j + 1) % UPDATE_TW2 == 0 && j < halfN1 - 1)
                {
                    θ = -4 * PI * i * (j + 1) / n;
                    (twx, twy) = (1.0 - 2 * Sqr(Sin(0.5 * θ)), Sin(θ));
                }
                else
                {
                    double tmpX = twx * twRowX - twy * twRowY;
                    twy = twx * twRowY + twy * twRowX;
                    twx = tmpX;
                }
            }
            if (n1 % 2 == 1)
            {
                // Handle residual chunk
                double x = a[0], y = a[1];
                a[0] = x * twx - y * twy; a[1] = x * twy + y * twx;
                a += 2;
            }
            // Update TwRow: TwRow(new) = TwRow(old)*TwBase
            if (i < n2 - 1)
            {
                if ((i + 1) % UPDATE_TW == 0)
                {
                    θ = -PI * (i + 1) / n;
                    (twRowX, twRowY) = (1.0 - 2 * Sqr(Sin(θ)), Sin(2 * θ));
                }
                else
                {
                    double tmpX = twRowX * twBaseX - twRowY * twBaseY;
                    twRowY = twRowX * twBaseY + twRowY * twBaseX;
                    twRowX = tmpX;
                }
            }
        }
    }

    private static void FindPrimitiveRoot(int n, out int proot, out int invproot)
    {
        proot = 0;
        // Because N is prime, Euler totient function is equal to N-1
        int φn = n - 1;
        // Test different values of pRoot, from 2 to N-1.
        // * compute φ(N)
        // * determine the different prime factors of φ(N), say p1, ..., pk
        // * for every element m of Zn*, compute m^(φ(N)/π) mod N for i=1..k
        //   using a fast algorithm for modular exponentiation.
        // * a number m for which these k results are all different from 1 is a primitive root.
        for (int candidate = 2; candidate <= n - 1; candidate++)
        {
            // Scan different prime factors of φ(n). Here:
            // * F is a current candidate factor
            // * Q is a current quotient - amount which was left after dividing φ(n)
            //   by all previous factors
            bool allnonone = true;
            for (int q = φn, f = 2; q > 1; f++)
                if (q % f == 0)
                {
                    int t = ModExp(candidate, φn / f, n);
                    if (t == 1)
                    {
                        allnonone = false;
                        break;
                    }
                    while (q % f == 0)
                        q /= f;
                }
            if (allnonone)
            {
                proot = candidate;
                break;
            }
        }
        // Extended Euclidean algorithm to find multiplicative inverse.
        int x = 0, lastx = 1, y = 1, lasty = 0;
        int a = proot, b = n;
        while (b != 0)
        {
            int q = DivRem(a, b, out int t);
            a = b; b = t; t = lastx - q * x;
            lastx = x; x = t; t = lasty - q * y;
            lasty = y; y = t;
        }
        while (lastx < 0)
            lastx += n;
        invproot = lastx;

        static int ModExp(int a, int b, int n) =>
            b == 0 ? 1 : b == 1 ? a
            : b % 2 == 0
            ? ModExp(ModMul(a, a, n), b / 2, n)
            : ModMul(ModExp(ModMul(a, a, n), b / 2, n), a, n);

        static int ModMul(int a, int b, int n) =>
            b == 0 || a == 0 ? 0 : b == 1 ? a : a == 1 ? b
            : (int)((long)a * b % n);
    }

    /// <summary>Provides a simple pool of arrays.</summary>
    private struct SharedPool(int arraySize)
    {
        private sealed class Entry
        {
            public double[]? obj1, obj2;
            public Entry? next_entry;
        }

        /// <summary>List of recycled arrays.</summary>
        private Entry? recycled_objects;
        /// <summary>List of recycled entries.</summary>
        private Entry? recycled_entries;

        /// <summary>Size of new arrays.</summary>
        public int ArraySize { get; } = arraySize;

        public (double[] obj1, double[] obj2) Rent()
        {
            if (recycled_objects != null)
            {
                Entry result = recycled_objects;
                recycled_objects = recycled_objects.next_entry;
                var ret = (result.obj1!, result.obj2!);
                result.obj1 = result.obj2 = null;
                result.next_entry = recycled_entries;
                recycled_entries = result;
                return ret;
            }
            return (new double[ArraySize], new double[ArraySize]);
        }

        public void Return(double[] obj1, double[] obj2)
        {
            Entry new_entry;
            if (recycled_entries != null)
            {
                new_entry = recycled_entries;
                recycled_entries = new_entry.next_entry;
            }
            else
                new_entry = new();
            new_entry.obj1 = obj1;
            new_entry.obj2 = obj2;
            new_entry.next_entry = recycled_objects;
            recycled_objects = new_entry;
        }
    }
};
