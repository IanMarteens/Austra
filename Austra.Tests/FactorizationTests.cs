namespace Austra.Tests;

[TestFixture]
public class FactorizationTests
{
    /// <summary>
    /// Check that eigenvectors are calculated correctly for symmetrical matrices.
    /// </summary>
    [Test]
    public void EvdSymTest([Values(12, 18, 25, 32, 33, 39)] int size)
    {
        // Generate a random lower triangular matrix.
        LMatrix m = new(size, Random.Shared);
        // Create a symmetric matrix from the lower triangular matrix.
        Matrix sm = m.MultiplyTranspose(m);
        EVD evd = sm.SymEVD();
        Matrix VΛV = evd.Vectors * evd.D * evd.Vectors.Transpose();
        Assert.That((sm - VΛV).AMax, Is.LessThan(1E-12));
    }

    /// <summary>
    /// Check that eigenvectors are calculated correctly for general matrices.
    /// </summary>
    [Test]
    public void EvdTest([Values(8, 15, 18, 32, 33)] int size)
    {
        // Generate a random squared matrix.
        Matrix m = new(size, size, new Random(12), 1);
        EVD evd = m.EVD(false);
        Matrix AV = m * evd.Vectors;
        Matrix VΛ = evd.Vectors * evd.D;
        Assert.That((AV - VΛ).AMax(), Is.LessThan(1E-11));
    }

    /// <summary>
    /// Check LU decomposition for solving linear systems.
    /// </summary>
    [Test]
    public void LUSolveTest([Values(27, 32, 37)] int size)
    {
        Matrix m = new(size, Random.Shared);
        while (m.Determinant() == 0)
            m = new(size, Random.Shared);
        Vector v = new(size, Random.Shared);
        Vector answer = m.LU().Solve(v);
        Assert.That((m * answer - v).AMax(), Is.LessThan(1E-12));
    }

    [Test]
    public void LUInvertTest([Values(27, 32, 67)] int size)
    {
        Matrix m = new(size, Random.Shared);
        while (m.Determinant() < 1E-7)
            m = new(size, Random.Shared);
        Matrix answer = m.LU().Solve(Matrix.Identity(size));
        Assert.That((m * answer - Matrix.Identity(size)).AMax(), Is.LessThan(1E-12));
    }

    /// <summary>
    /// Cholesky can factorize identity matrix.
    /// </summary>
    /// <param name="order">Matrix order.</param>
    [Test]
    public void CholeskyCanFactorizeIdentity([Values(1, 10, 100)] int order)
    {
        var m = Matrix.Identity(order);
        var chol = m.Cholesky().L;
        Assert.Multiple(() =>
        {
            Assert.That(chol.Rows, Is.EqualTo(m.Rows));
            Assert.That(chol.Cols, Is.EqualTo(m.Cols));
        });
        for (var i = 0; i < chol.Rows; i++)
            for (var j = 0; j < chol.Cols; j++)
                Assert.That(chol[i, j], Is.EqualTo(i == j ? 1.0 : 0.0));
    }

    [Test]
    public void CholeskyTest([Values(9, 16, 33)] int order)
    {
        var lm = new LMatrix(order, order, Random.Shared, 0.2);
        var m = lm * lm.Transpose();
        var chol = m.Cholesky().L;
        var m1 = chol * chol.Transpose();
        Assert.That((m - m1).AMax(), Is.LessThan(1E-12));
    }

    /// <summary>
    /// Check Cholesky decomposition for solving linear systems.
    /// </summary>
    [Test]
    public void CholeskySolveVector([Values(27, 32, 37)] int size)
    {
        LMatrix lm = new(size, size, Random.Shared, 0.2);
        while (lm.Determinant() == 0)
            lm = new(size, Random.Shared);
        Matrix m = lm * lm.Transpose();
        Vector v = new(size, Random.Shared);
        Vector answer = m.Cholesky().Solve(v);
        // Yes, this is a very loose tolerance, but it's the best we can do with Cholesky.
        Assert.That((m * answer - v).AMax(), Is.LessThan(2E-05));
    }

    [Test]
    public void CholeskyInvert([Values(27, 32, 37)] int size)
    {
        LMatrix lm = new(size, size, Random.Shared, 0.2);
        while (lm.Determinant() < 1E-12)
            lm = new(size, Random.Shared);
        Matrix m = lm.MultiplyTranspose(lm);
        Matrix inverse = m.Cholesky().Solve(Matrix.Identity(size));
        Assert.That((m * inverse - Matrix.Identity(size)).AMax(), Is.LessThan(1E-5));
    }
}
