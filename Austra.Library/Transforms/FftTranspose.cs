namespace Austra.Library.Transforms;

/// <summary>Represents a Fast Fourier Transform plan.</summary>
public sealed partial class FftPlan
{
    /// <summary>Complex numbers in a row for complex transpose.</summary>
    private const int BLOCK_SIZE = 16;

    /// <summary>Transpose complex matrix stored in 1-dimensional array.</summary>
    private static unsafe void ComplexTranspose(double* a, double* b, int m, int n)
    {
        Transpose(a, n, b, m, m, n);
        int size = 2 * sizeof(double) * m * n;
        Buffer.MemoryCopy(b, a, size, size);

        static void Transpose(double* a, int astride, double* b, int bstride, int m, int n)
        {
            if (n + m <= 16)
                for (int i = 0, m2 = 2 * bstride; i < m; i++)
                {
                    int idx1 = 2 * i, idx2 = idx1 * astride;
                    for (int j = 0; j < n; j++, idx1 += m2, idx2 += 2)
                        (b[idx1], b[idx1 + 1]) = (a[idx2], a[idx2 + 1]);
                }
            else if (n > m)
            {
                // Horizontal to vertical.
                int n1 = n <= 14 ? n / 2 : (((n + 14) >> 1) & ~7);
                Transpose(a, astride, b, bstride, m, n1);
                Transpose(a + 2 * n1, astride, b + 2 * n1 * bstride, bstride, m, n - n1);
            }
            else
            {
                // Vertical to horizontal.
                int m1 = m <= 14 ? m / 2 : (((m + 14) >> 1) & ~7);
                Transpose(a, astride, b, bstride, m1, n);
                Transpose(a + 2 * m1 * astride, astride, b + 2 * m1, bstride, m - m1, n);
            }
        }
    }

    /// <summary>Transpose complex matrix stored in 1-dimensional array.</summary>
    private static unsafe void ComplexTranspose(Complex* a, Complex* b, int m, int n)
    {
        for (int rowBase = 0; rowBase < m; rowBase += BLOCK_SIZE)
        {
            int rowLimit = Min(rowBase + BLOCK_SIZE, m);
            for (int colBase = 0; colBase < n; colBase += BLOCK_SIZE)
            {
                int colLimit = Min(colBase + BLOCK_SIZE, n);
                switch (colLimit - colBase)
                {
                    case BLOCK_SIZE:        // 16
                        {
                            int rowTop = rowLimit & ~1, row = rowBase;
                            for (; row < rowTop; row += 2)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                var d2 = Avx.LoadVector256((double*)(a0 + 2));
                                var d3 = Avx.LoadVector256((double*)(a0 + 4));
                                var d4 = Avx.LoadVector256((double*)(a0 + 6));
                                var d5 = Avx.LoadVector256((double*)(a0 + 8));
                                var d6 = Avx.LoadVector256((double*)(a0 + 10));
                                var d7 = Avx.LoadVector256((double*)(a0 + 12));
                                var d8 = Avx.LoadVector256((double*)(a0 + 14));
                                a0 += n;
                                var e1 = Avx.LoadVector256((double*)a0);
                                var e2 = Avx.LoadVector256((double*)(a0 + 2));
                                var e3 = Avx.LoadVector256((double*)(a0 + 4));
                                var e4 = Avx.LoadVector256((double*)(a0 + 6));
                                var e5 = Avx.LoadVector256((double*)(a0 + 8));
                                var e6 = Avx.LoadVector256((double*)(a0 + 10));
                                var e7 = Avx.LoadVector256((double*)(a0 + 12));
                                var e8 = Avx.LoadVector256((double*)(a0 + 14));
                                Complex* b0 = b + row + colBase * m;
                                Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d5, e5, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d5, e5, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d6, e6, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d6, e6, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d7, e7, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d7, e7, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d8, e8, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d8, e8, 0x31));
                            }
                            if (row < rowLimit)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                var d2 = Avx.LoadVector256((double*)(a0 + 2));
                                var d3 = Avx.LoadVector256((double*)(a0 + 4));
                                var d4 = Avx.LoadVector256((double*)(a0 + 6));
                                var d5 = Avx.LoadVector256((double*)(a0 + 8));
                                var d6 = Avx.LoadVector256((double*)(a0 + 10));
                                var d7 = Avx.LoadVector256((double*)(a0 + 12));
                                var d8 = Avx.LoadVector256((double*)(a0 + 14));
                                Complex* b0 = b + row + colBase * m;
                                Sse2.Store((double*)b0, d1.GetLower());
                                Sse2.Store((double*)(b0 += m), d1.GetUpper());
                                Sse2.Store((double*)(b0 += m), d2.GetLower());
                                Sse2.Store((double*)(b0 += m), d2.GetUpper());
                                Sse2.Store((double*)(b0 += m), d3.GetLower());
                                Sse2.Store((double*)(b0 += m), d3.GetUpper());
                                Sse2.Store((double*)(b0 += m), d4.GetLower());
                                Sse2.Store((double*)(b0 += m), d4.GetUpper());
                                Sse2.Store((double*)(b0 += m), d5.GetLower());
                                Sse2.Store((double*)(b0 += m), d5.GetUpper());
                                Sse2.Store((double*)(b0 += m), d6.GetLower());
                                Sse2.Store((double*)(b0 += m), d6.GetUpper());
                                Sse2.Store((double*)(b0 += m), d7.GetLower());
                                Sse2.Store((double*)(b0 += m), d7.GetUpper());
                                Sse2.Store((double*)(b0 += m), d8.GetLower());
                                Sse2.Store((double*)(b0 += m), d8.GetUpper());
                            }
                        }
                        break;
                    case 11:
                        for (int row = rowBase; row < rowLimit; row++)
                        {
                            Complex* a0 = a + row * n + colBase;
                            var d1 = Avx.LoadVector256((double*)a0);
                            var d2 = Avx.LoadVector256((double*)(a0 + 2));
                            var d3 = Avx.LoadVector256((double*)(a0 + 4));
                            var d4 = Avx.LoadVector256((double*)(a0 + 6));
                            var d5 = Avx.LoadVector256((double*)(a0 + 8));
                            var d6 = Sse2.LoadVector128((double*)(a0 + 10));
                            Complex* b0 = b + row + colBase * m;
                            Sse2.Store((double*)b0, d1.GetLower());
                            Sse2.Store((double*)(b0 += m), d1.GetUpper());
                            Sse2.Store((double*)(b0 += m), d2.GetLower());
                            Sse2.Store((double*)(b0 += m), d2.GetUpper());
                            Sse2.Store((double*)(b0 += m), d3.GetLower());
                            Sse2.Store((double*)(b0 += m), d3.GetUpper());
                            Sse2.Store((double*)(b0 += m), d4.GetLower());
                            Sse2.Store((double*)(b0 += m), d4.GetUpper());
                            Sse2.Store((double*)(b0 += m), d5.GetLower());
                            Sse2.Store((double*)(b0 += m), d5.GetUpper());
                            Sse2.Store((double*)(b0 += m), d6);
                        }
                        break;
                    case BLOCK_SIZE / 2:    // 8
                        {
                            int rowTop = rowLimit & ~1, row = rowBase;
                            for (; row < rowTop; row += 2)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                var d2 = Avx.LoadVector256((double*)(a0 + 2));
                                var d3 = Avx.LoadVector256((double*)(a0 + 4));
                                var d4 = Avx.LoadVector256((double*)(a0 + 6));
                                a0 += n;
                                var e1 = Avx.LoadVector256((double*)a0);
                                var e2 = Avx.LoadVector256((double*)(a0 + 2));
                                var e3 = Avx.LoadVector256((double*)(a0 + 4));
                                var e4 = Avx.LoadVector256((double*)(a0 + 6));
                                Complex* b0 = b + row + colBase * m;
                                Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x31));
                            }
                            if (row < rowLimit)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                var d2 = Avx.LoadVector256((double*)(a0 + 2));
                                var d3 = Avx.LoadVector256((double*)(a0 + 4));
                                var d4 = Avx.LoadVector256((double*)(a0 + 6));
                                Complex* b0 = b + row + colBase * m;
                                Sse2.Store((double*)b0, d1.GetLower());
                                Sse2.Store((double*)(b0 += m), d1.GetUpper());
                                Sse2.Store((double*)(b0 += m), d2.GetLower());
                                Sse2.Store((double*)(b0 += m), d2.GetUpper());
                                Sse2.Store((double*)(b0 += m), d3.GetLower());
                                Sse2.Store((double*)(b0 += m), d3.GetUpper());
                                Sse2.Store((double*)(b0 += m), d4.GetLower());
                                Sse2.Store((double*)(b0 += m), d4.GetUpper());
                            }
                        }
                        break;
                    case 6:
                        {
                            int rowTop = rowLimit & ~1, row = rowBase;
                            for (; row < rowTop; row += 2)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                var d2 = Avx.LoadVector256((double*)(a0 + 2));
                                var d3 = Avx.LoadVector256((double*)(a0 + 4));
                                a0 += n;
                                var e1 = Avx.LoadVector256((double*)a0);
                                var e2 = Avx.LoadVector256((double*)(a0 + 2));
                                var e3 = Avx.LoadVector256((double*)(a0 + 4));
                                Complex* b0 = b + row + colBase * m;
                                Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x31));
                            }
                            if (row < rowLimit)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                var d2 = Avx.LoadVector256((double*)(a0 + 2));
                                var d3 = Avx.LoadVector256((double*)(a0 + 4));
                                Complex* b0 = b + row + colBase * m;
                                Sse2.Store((double*)b0, d1.GetLower());
                                Sse2.Store((double*)(b0 += m), d1.GetUpper());
                                Sse2.Store((double*)(b0 += m), d2.GetLower());
                                Sse2.Store((double*)(b0 += m), d2.GetUpper());
                                Sse2.Store((double*)(b0 += m), d3.GetLower());
                                Sse2.Store((double*)(b0 += m), d3.GetUpper());
                            }
                        }
                        break;
                    case 5:
                        for (int row = rowBase; row < rowLimit; row++)
                        {
                            Complex* a0 = a + row * n + colBase;
                            var d1 = Avx.LoadVector256((double*)a0);
                            var d2 = Avx.LoadVector256((double*)(a0 + 2));
                            var d3 = Sse2.LoadVector128((double*)(a0 + 4));
                            Complex* b0 = b + row + colBase * m;
                            Sse2.Store((double*)b0, d1.GetLower());
                            Sse2.Store((double*)(b0 += m), d1.GetUpper());
                            Sse2.Store((double*)(b0 += m), d2.GetLower());
                            Sse2.Store((double*)(b0 += m), d2.GetUpper());
                            Sse2.Store((double*)(b0 += m), d3);
                        }
                        break;
                    case BLOCK_SIZE / 4:    // 4
                        {
                            int rowTop = rowLimit & ~1, row = rowBase;
                            for (; row < rowTop; row += 2)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                var d2 = Avx.LoadVector256((double*)(a0 + 2));
                                a0 += n;
                                var e1 = Avx.LoadVector256((double*)a0);
                                var e2 = Avx.LoadVector256((double*)(a0 + 2));
                                Complex* b0 = b + row + colBase * m;
                                Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                            }
                            if (row < rowLimit)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                var d2 = Avx.LoadVector256((double*)(a0 + 2));
                                Complex* b0 = b + row + colBase * m;
                                Sse2.Store((double*)b0, d1.GetLower());
                                Sse2.Store((double*)(b0 += m), d1.GetUpper());
                                Sse2.Store((double*)(b0 += m), d2.GetLower());
                                Sse2.Store((double*)(b0 += m), d2.GetUpper());
                            }
                        }
                        break;
                    case BLOCK_SIZE / 8:    // 2
                        {
                            int rowTop = rowLimit & ~1, row = rowBase;
                            for (; row < rowTop; row += 2)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                a0 += n;
                                var e1 = Avx.LoadVector256((double*)a0);
                                Complex* b0 = b + row + colBase * m;
                                Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                                Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                            }
                            if (row < rowLimit)
                            {
                                Complex* a0 = a + row * n + colBase;
                                var d1 = Avx.LoadVector256((double*)a0);
                                Complex* b0 = b + row + colBase * m;
                                Sse2.Store((double*)b0, d1.GetLower());
                                Sse2.Store((double*)(b0 += m), d1.GetUpper());
                            }
                        }
                        break;
                    default:
                        for (int row = rowBase; row < rowLimit; row++)
                        {
                            Complex* a0 = a + row * n + colBase;
                            for (int col = colBase; col < colLimit; col++)
                                b[col * m + row] = *(a0++);
                        }
                        break;
                }
            }
        }
        int size = 2 * sizeof(double) * m * n;
        Buffer.MemoryCopy(b, a, size, size);
    }

    /// <summary>Transpose a small complex matrix stored in 1-dimensional array.</summary>
    private static unsafe void SmallComplexTranspose(Complex* a, Complex* b, int m, int n)
    {
        int row = 0;
        switch (n)
        {
            case 16:
                for (int rowTop = m & ~1; row < rowTop; row += 2)
                {
                    Complex* a0 = a + row * 16;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Avx.LoadVector256((double*)(a0 + 4));
                    var d4 = Avx.LoadVector256((double*)(a0 + 6));
                    var d5 = Avx.LoadVector256((double*)(a0 + 8));
                    var d6 = Avx.LoadVector256((double*)(a0 + 10));
                    var d7 = Avx.LoadVector256((double*)(a0 + 12));
                    var d8 = Avx.LoadVector256((double*)(a0 + 14));
                    var e1 = Avx.LoadVector256((double*)(a0 + 16));
                    var e2 = Avx.LoadVector256((double*)(a0 + 18));
                    var e3 = Avx.LoadVector256((double*)(a0 + 20));
                    var e4 = Avx.LoadVector256((double*)(a0 + 22));
                    var e5 = Avx.LoadVector256((double*)(a0 + 24));
                    var e6 = Avx.LoadVector256((double*)(a0 + 26));
                    var e7 = Avx.LoadVector256((double*)(a0 + 28));
                    var e8 = Avx.LoadVector256((double*)(a0 + 30));
                    Complex* b0 = b + row;
                    Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d5, e5, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d5, e5, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d6, e6, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d6, e6, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d7, e7, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d7, e7, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d8, e8, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d8, e8, 0x31));
                }
                if (row < m)
                {
                    Complex* a0 = a + row * 16;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Avx.LoadVector256((double*)(a0 + 4));
                    var d4 = Avx.LoadVector256((double*)(a0 + 6));
                    var d5 = Avx.LoadVector256((double*)(a0 + 8));
                    var d6 = Avx.LoadVector256((double*)(a0 + 10));
                    var d7 = Avx.LoadVector256((double*)(a0 + 12));
                    var d8 = Avx.LoadVector256((double*)(a0 + 14));
                    Complex* b0 = b + row;
                    Sse2.Store((double*)b0, d1.GetLower());
                    Sse2.Store((double*)(b0 += m), d1.GetUpper());
                    Sse2.Store((double*)(b0 += m), d2.GetLower());
                    Sse2.Store((double*)(b0 += m), d2.GetUpper());
                    Sse2.Store((double*)(b0 += m), d3.GetLower());
                    Sse2.Store((double*)(b0 += m), d3.GetUpper());
                    Sse2.Store((double*)(b0 += m), d4.GetLower());
                    Sse2.Store((double*)(b0 += m), d4.GetUpper());
                    Sse2.Store((double*)(b0 += m), d5.GetLower());
                    Sse2.Store((double*)(b0 += m), d5.GetUpper());
                    Sse2.Store((double*)(b0 += m), d6.GetLower());
                    Sse2.Store((double*)(b0 += m), d6.GetUpper());
                    Sse2.Store((double*)(b0 += m), d7.GetLower());
                    Sse2.Store((double*)(b0 += m), d7.GetUpper());
                    Sse2.Store((double*)(b0 += m), d8.GetLower());
                    Sse2.Store((double*)(b0 += m), d8.GetUpper());
                }
                break;
            case 10:
                for (int rowTop = m & ~1; row < rowTop; row += 2)
                {
                    Complex* a0 = a + row * 10;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Avx.LoadVector256((double*)(a0 + 4));
                    var d4 = Avx.LoadVector256((double*)(a0 + 6));
                    var d5 = Avx.LoadVector256((double*)(a0 + 8));
                    var e1 = Avx.LoadVector256((double*)(a0 + 10));
                    var e2 = Avx.LoadVector256((double*)(a0 + 12));
                    var e3 = Avx.LoadVector256((double*)(a0 + 14));
                    var e4 = Avx.LoadVector256((double*)(a0 + 16));
                    var e5 = Avx.LoadVector256((double*)(a0 + 18));
                    Complex* b0 = b + row;
                    Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d5, e5, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d5, e5, 0x31));
                }
                if (row < m)
                {
                    Complex* a0 = a + row * 10;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Avx.LoadVector256((double*)(a0 + 4));
                    var d4 = Avx.LoadVector256((double*)(a0 + 6));
                    var d5 = Avx.LoadVector256((double*)(a0 + 8));
                    Complex* b0 = b + row;
                    Sse2.Store((double*)b0, d1.GetLower());
                    Sse2.Store((double*)(b0 += m), d1.GetUpper());
                    Sse2.Store((double*)(b0 += m), d2.GetLower());
                    Sse2.Store((double*)(b0 += m), d2.GetUpper());
                    Sse2.Store((double*)(b0 += m), d3.GetLower());
                    Sse2.Store((double*)(b0 += m), d3.GetUpper());
                    Sse2.Store((double*)(b0 += m), d4.GetLower());
                    Sse2.Store((double*)(b0 += m), d4.GetUpper());
                    Sse2.Store((double*)(b0 += m), d5.GetLower());
                    Sse2.Store((double*)(b0 += m), d5.GetUpper());
                }
                break;
            case 8:
                for (int rowTop = m & ~1; row < rowTop; row += 2)
                {
                    Complex* a0 = a + row * 8;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Avx.LoadVector256((double*)(a0 + 4));
                    var d4 = Avx.LoadVector256((double*)(a0 + 6));
                    var e1 = Avx.LoadVector256((double*)(a0 + 8));
                    var e2 = Avx.LoadVector256((double*)(a0 + 10));
                    var e3 = Avx.LoadVector256((double*)(a0 + 12));
                    var e4 = Avx.LoadVector256((double*)(a0 + 14));
                    Complex* b0 = b + row;
                    Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d4, e4, 0x31));
                }
                if (row < m)
                {
                    Complex* a0 = a + row * 8;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Avx.LoadVector256((double*)(a0 + 4));
                    var d4 = Avx.LoadVector256((double*)(a0 + 6));
                    Complex* b0 = b + row;
                    Sse2.Store((double*)b0, d1.GetLower());
                    Sse2.Store((double*)(b0 += m), d1.GetUpper());
                    Sse2.Store((double*)(b0 += m), d2.GetLower());
                    Sse2.Store((double*)(b0 += m), d2.GetUpper());
                    Sse2.Store((double*)(b0 += m), d3.GetLower());
                    Sse2.Store((double*)(b0 += m), d3.GetUpper());
                    Sse2.Store((double*)(b0 += m), d4.GetLower());
                    Sse2.Store((double*)(b0 += m), d4.GetUpper());
                }
                break;
            case 6:
                for (int rowTop = m & ~1; row < rowTop; row += 2)
                {
                    Complex* a0 = a + row * 6;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Avx.LoadVector256((double*)(a0 + 4));
                    var e1 = Avx.LoadVector256((double*)(a0 + 6));
                    var e2 = Avx.LoadVector256((double*)(a0 + 8));
                    var e3 = Avx.LoadVector256((double*)(a0 + 10));
                    Complex* b0 = b + row;
                    Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d3, e3, 0x31));
                }
                if (row < m)
                {
                    Complex* a0 = a + row * 6;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Avx.LoadVector256((double*)(a0 + 4));
                    Complex* b0 = b + row;
                    Sse2.Store((double*)b0, d1.GetLower());
                    Sse2.Store((double*)(b0 += m), d1.GetUpper());
                    Sse2.Store((double*)(b0 += m), d2.GetLower());
                    Sse2.Store((double*)(b0 += m), d2.GetUpper());
                    Sse2.Store((double*)(b0 += m), d3.GetLower());
                    Sse2.Store((double*)(b0 += m), d3.GetUpper());
                }
                break;
            case 5:
                for (int rowTop = m & ~1; row < rowTop; row += 2)
                {
                    Complex* a0 = a + row * 5;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Sse2.LoadVector128((double*)(a0 + 4));
                    var e1 = Avx.LoadVector256((double*)(a0 + 5));
                    var e2 = Avx.LoadVector256((double*)(a0 + 7));
                    var e3 = Sse2.LoadVector128((double*)(a0 + 9));
                    Complex* b0 = b + row;
                    Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                    Avx.Store((double*)(b0 += m), V4.Create(d3, e3));
                }
                if (row < m)
                {
                    Complex* a0 = a + row * 5;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var d3 = Sse2.LoadVector128((double*)(a0 + 4));
                    Complex* b0 = b + row;
                    Sse2.Store((double*)b0, d1.GetLower());
                    Sse2.Store((double*)(b0 += m), d1.GetUpper());
                    Sse2.Store((double*)(b0 += m), d2.GetLower());
                    Sse2.Store((double*)(b0 += m), d2.GetUpper());
                    Sse2.Store((double*)(b0 += m), d3);
                }
                break;
            case 4:
                for (int rowTop = m & ~1; row < rowTop; row += 2)
                {
                    Complex* a0 = a + row * 4;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    var e1 = Avx.LoadVector256((double*)(a0 + 4));
                    var e2 = Avx.LoadVector256((double*)(a0 + 6));
                    Complex* b0 = b + row;
                    Avx.Store((double*)b0, Avx.Permute2x128(d1, e1, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d1, e1, 0x31));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x20));
                    Avx.Store((double*)(b0 += m), Avx.Permute2x128(d2, e2, 0x31));
                }
                if (row < m)
                {
                    Complex* a0 = a + row * 4;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Avx.LoadVector256((double*)(a0 + 2));
                    Complex* b0 = b + row;
                    Sse2.Store((double*)b0, d1.GetLower());
                    Sse2.Store((double*)(b0 += m), d1.GetUpper());
                    Sse2.Store((double*)(b0 += m), d2.GetLower());
                    Sse2.Store((double*)(b0 += m), d2.GetUpper());
                }
                break;
            case 3:
                for (; row < m; row++)
                {
                    Complex* a0 = a + row * 3;
                    var d1 = Avx.LoadVector256((double*)a0);
                    var d2 = Sse2.LoadVector128((double*)(a0 + 2));
                    Complex* b0 = b + row;
                    Sse2.Store((double*)b0, d1.GetLower());
                    Sse2.Store((double*)(b0 += m), d1.GetUpper());
                    Sse2.Store((double*)(b0 += m), d2);
                }
                break;
            default:
                for (; row < m; row++)
                {
                    Complex* a0 = a + row * n;
                    for (int col = 0; col < n; col++)
                        b[col * m + row] = *(a0++);
                }
                break;
        }
        int size = 2 * sizeof(double) * m * n;
        Buffer.MemoryCopy(b, a, size, size);
    }
}