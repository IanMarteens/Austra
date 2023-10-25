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
        Matrix m = new(size, new NormalRandom());
        Assert.That((m.Transpose().Transpose() - m).AMax(), Is.EqualTo(0));
    }

    /// <summary>
    /// Check in-place matrix transpose.
    /// </summary>
    [Test]
    public void CheckInplaceMatrixTranspose([Values(32, 49, 61)] int size)
    {
        Matrix m = new(size, new NormalRandom());
        Matrix m1 = m.Transpose();
        CommonMatrix.Transpose((double[,])m);
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
}
