namespace Austra.Tests;

[TestFixture]
public class MatrixTests
{
    /// <summary>
    /// Check that a cloned matrix is equal to the original.
    /// </summary>
    [Test]
    public void CheckMatrixEquality()
    {
        Matrix m = new(32, 28, Random.Shared);
        Matrix m1 = m.Clone();
        Assert.That(m, Is.EqualTo(m1));
    }

    /// <summary>
    /// Check matrix transpose.
    /// </summary>
    [Test]
    public void CheckMatrixTranspose([Values(32, 49, 61)] int size)
    {
        int idx = 1;
        Matrix m = new(size, (i, j) => idx++);
        Assert.That((m.Transpose().Transpose() - m).AMax(), Is.EqualTo(0));
    }

    /// <summary>
    /// Check in-place matrix transpose.
    /// </summary>
    [Test]
    public void CheckInplaceMatrixTranspose([Values(32, 49, 61, 63, 128)] int size)
    {
        Matrix m = new(size, new NormalRandom());
        Matrix m1 = m.Transpose();
        CommonMatrix.Transpose(m.Rows, m.Cols, (double[])m);
        Assert.That((m - m1).AMax(), Is.EqualTo(0));
    }

    /// <summary>
    /// Check matrix inversion using LU decomposition.
    /// </summary>
    [Test]
    public void InvertMatrix([Values(27, 32, 37)] int size)
    {
        Matrix m = new(size, Random.Shared);
        while (m.Determinant() == 0)
            m = new(size, Random.Shared);
        Matrix m1 = m.Inverse();
        Assert.That((m * m1 - Matrix.Identity(size)).AMax(), Is.LessThan(1E-12));
    }

    /// <summary>
    /// Check that lower matrix cells are correctly scanned.
    /// </summary>
    [Test]
    public void CheckLMatrixAMin([Values(32, 49, 61)] int size)
    {
        LMatrix m = new(size + 3, size, Random.Shared, offset: 0.01);
        Assert.That(m.AMin(), Is.GreaterThanOrEqualTo(0.01));
    }

    /// <summary>
    /// Check matrix multiply by transposed is ok.
    /// </summary>
    [Test]
    public void CheckMultiplyTranspose([Values(32, 49, 61)] int size)
    {
        Matrix m1 = new(size, new NormalRandom());
        Matrix m2 = new(size, new NormalRandom());
        Matrix p1 = m1.MultiplyTranspose(m2);
        Matrix p2 = m1 * m2.Transpose();
        Assert.That((p1 - p2).AMax(), Is.LessThanOrEqualTo(1E-12));
    }

    /// <summary>
    /// Check that vector transformed by transposed is ok.
    /// </summary>
    [Test]
    public void CheckVectorMultiplyTranspose([Values(32, 49, 61)] int size)
    {
        Matrix m1 = new(size, new NormalRandom());
        DVector v1 = new(size, new NormalRandom());
        DVector r1 = m1.TransposeMultiply(v1);
        DVector r2 = m1.Transpose() * v1;
        Assert.That((r1 - r2).AMax(), Is.LessThanOrEqualTo(1E-13));
    }

    [Test]
    public void CheckMultiplyAdd([Values(32, 49, 61)] int size)
    {
        Matrix m1 = new(size, new NormalRandom());
        DVector v1 = new(size, new NormalRandom());
        DVector v2 = new(size, new NormalRandom());
        DVector r1 = m1.MultiplyAdd(v1, v2);
        DVector r2 = m1 * v1 + v2;
        Assert.That((r1 - r2).AMax(), Is.LessThanOrEqualTo(1E-14));
    }

    [Test]
    public void CheckLMultiplyAdd([Values(32, 49, 61)] int size)
    {
        LMatrix m1 = new(size, new NormalRandom());
        DVector v1 = new(size, new NormalRandom());
        DVector v2 = new(size, new NormalRandom());
        DVector r1 = m1.MultiplyAdd(v1, v2);
        DVector r2 = m1 * v1 + v2;
        Assert.That((r1 - r2).AMax(), Is.LessThanOrEqualTo(1E-14));
    }

    [Test]
    public void CheckMultiplySub([Values(32, 49, 61)] int size)
    {
        Matrix m1 = new(size, new NormalRandom());
        DVector v1 = new(size, new NormalRandom());
        DVector v2 = new(size, new NormalRandom());
        DVector r1 = m1.MultiplySubtract(v1, v2);
        DVector r2 = m1 * v1 - v2;
        Assert.That((r1 - r2).AMax(), Is.LessThanOrEqualTo(1E-14));
    }

    [Test]
    public void CheckLMultiplySub([Values(32, 49, 61)] int size)
    {
        LMatrix m1 = new(size, new NormalRandom());
        DVector v1 = new(size, new NormalRandom());
        DVector v2 = new(size, new NormalRandom());
        DVector r1 = m1.MultiplySubtract(v1, v2);
        DVector r2 = m1 * v1 - v2;
        Assert.That((r1 - r2).AMax(), Is.LessThanOrEqualTo(1E-14));
    }

    [Test]
    public void CheckLMatrixSolve([Values(32, 49, 61)] int size)
    {
        LMatrix m = new LMatrix(size, new Random()) + LMatrix.Identity(size) * 0.1;
        DVector v = new(size, new NormalRandom());
        DVector x = m.Solve(v);
        Assert.That((m * x - v).AMax(), Is.LessThanOrEqualTo(2E-6));
    }

    [Test]
    public void CheckRMatrixSolve([Values(32, 49, 61)] int size)
    {
        RMatrix m = new RMatrix(size, new Random()) + RMatrix.Identity(size) * 0.1;
        DVector v = new(size, new NormalRandom());
        DVector x = m.Solve(v);
        Assert.That((m * x - v).AMax(), Is.LessThanOrEqualTo(2E-6));
    }

    [Test]
    public void CheckLMatrixTransform([Values(32, 49, 61)] int size)
    {
        LMatrix m = new LMatrix(size, new Random()) + LMatrix.Identity(size) * 0.05;
        DVector v = new(size, new NormalRandom());
        Assert.That((m * v - (Matrix)m * v).AMax(), Is.LessThanOrEqualTo(1E-14));
    }

    [Test]
    public void CheckMatrixMultiply([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        Matrix m1 = new Matrix(size, size, new Random(), 0.5, 1.6) + Matrix.Identity(size) * 0.02;
        Matrix m2 = new(size, size, new Random(), 0.5, 1.6);
        Matrix m3 = m1 * m2;
        NMatrix n3 = new NMatrix(m1).Multiply(new NMatrix(m2));
        Assert.That(new NMatrix(m3).AMax(n3), Is.LessThanOrEqualTo(1E-11));
    }

    [Test]
    public void CheckLSMatrixMultiply([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        LMatrix m1 = new(size, size, new Random(), 0.3, 1.5);
        Matrix m2 = new(size, size, new Random(), 0.3, 1.5);
        Matrix m3 = m1 * m2;
        NMatrix n3 = new NMatrix((Matrix)m1).Multiply(new NMatrix(m2));
        Assert.That(new NMatrix(m3).AMax(n3), Is.LessThanOrEqualTo(1E-12));
    }

    [Test]
    public void CheckSLMatrixMultiply([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        Matrix m1 = new(size, size, new Random(), 0.3, 1.5);
        LMatrix m2 = new(size, size, new Random(), 0.3, 1.5);
        Matrix m3 = m1 * m2;
        NMatrix n3 = new NMatrix(m1).Multiply(new NMatrix((Matrix)m2));
        Assert.That(new NMatrix(m3).AMax(n3), Is.LessThanOrEqualTo(1E-12));
    }

    [Test]
    public void CheckMatrixDiagonal([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        DVector v = new(size, Random.Shared);
        Matrix m = new(v);
        Assert.That(v, Is.EqualTo(m.Diagonal()));
    }

    [Test]
    public void CheckLMatrixTransposeMultiply([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        LMatrix m1 = new(size + 2, size + 5, new Random(), 0.3, 1.5);
        LMatrix m2 = new(size + 4, size + 5, new Random(), 0.3, 1.5);
        Matrix m3 = m1.MultiplyTranspose(m2);
        Matrix m4 = (Matrix)m1 * ((Matrix)m2).Transpose();
        Assert.That(m3.Distance(m4), Is.LessThanOrEqualTo(1E-10));
        m1 = new(size + 4, size, new Random(), 0.3, 1.5);
        m2 = new(size + 2, size, new Random(), 0.3, 1.5);
        m3 = m1.MultiplyTranspose(m2);
        m4 = (Matrix)m1 * ((Matrix)m2).Transpose();
        Assert.That(m3.Distance(m4), Is.LessThanOrEqualTo(1E-10));
    }

    [Test]
    public void CheckLMatrixSquare([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        LMatrix m1 = new(size + 2, size + 5, new Random(), 0.3, 1.5);
        Matrix m3 = m1.Square();
        Matrix m4 = (Matrix)m1 * ((Matrix)m1).Transpose();
        Assert.That(m3.Distance(m4), Is.LessThanOrEqualTo(1E-10));
        m1 = new(size + 4, size, new Random(), 0.3, 1.5);
        m3 = m1.Square();
        m4 = (Matrix)m1 * ((Matrix)m1).Transpose();
        Assert.That(m3.Distance(m4), Is.LessThanOrEqualTo(1E-10));
    }

    [Test]
    public void CheckRMatrixTransposeMultiply([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        RMatrix m1 = new(size + 2, size + 5, new Random(), 0.3, 1.5);
        RMatrix m2 = new(size + 4, size + 5, new Random(), 0.3, 1.5);
        Matrix m3 = m1.MultiplyTranspose(m2);
        Matrix m4 = (Matrix)m1 * ((Matrix)m2).Transpose();
        double distance = m3.Distance(m4);
        Assert.That(distance, Is.LessThanOrEqualTo(1E-10));
        m1 = new(size + 2, size, new Random(), 0.3, 1.5);
        m2 = new(size + 4, size, new Random(), 0.3, 1.5);
        m3 = m1.MultiplyTranspose(m2);
        m4 = (Matrix)m1 * ((Matrix)m2).Transpose();
        distance = m3.Distance(m4);
        Assert.That(distance, Is.LessThanOrEqualTo(1E-10));
    }

    [Test]
    public void CheckRMatrixSquare([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        RMatrix m1 = new(size + 2, size + 5, new Random(), 0.3, 1.5);
        Matrix m3 = m1.Square();
        Matrix m4 = (Matrix)m1 * ((Matrix)m1).Transpose();
        double distance = m3.Distance(m4);
        Assert.That(distance, Is.LessThanOrEqualTo(1E-10));
        m1 = new(size + 2, size, new Random(), 0.3, 1.5);
        m3 = m1.Square();
        m4 = (Matrix)m1 * ((Matrix)m1).Transpose();
        distance = m3.Distance(m4);
        Assert.That(distance, Is.LessThanOrEqualTo(1E-10));
    }

    [Test]
    public void CheckLMatrixTranspose([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        Random rnd = new();
        LMatrix m1 = new(size + rnd.Next(-1, 2) * 2, size, new Random(), 0.3, 1.5);
        Matrix m2 = (Matrix)m1;
        RMatrix m3 = m1.Transpose();
        Matrix m4 = m2.Transpose();
        double distance = m4.Distance((Matrix)m3);
        Assert.That(distance, Is.LessThanOrEqualTo(1E-14));
    }

    [Test]
    public void CheckRMatrixTranspose([Values(32, 35, 256, 257, 1024, 1025)] int size)
    {
        Random rnd = new();
        RMatrix m1 = new(size + rnd.Next(-1, 2) * 2, size, new Random(), 0.3, 1.5);
        Matrix m2 = (Matrix)m1;
        LMatrix m3 = m1.Transpose();
        Matrix m4 = m2.Transpose();
        double distance = m4.Distance((Matrix)m3);
        Assert.That(distance, Is.LessThanOrEqualTo(1E-14));
    }
}
