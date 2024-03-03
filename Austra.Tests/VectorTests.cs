namespace Austra.Tests;

[TestFixture]
public class VectorTests
{
    [SetUp]
    public void Setup()
    {
    }

    /// <summary>
    /// Check that the Euclidean norm of a vector is calculated correctly.
    /// </summary>
    [Test]
    public void EuclideanNorm([Values(12, 25, 39)] int size)
    {
        DVector v = new(size, 1.0);
        Assert.That(v.Norm(), Is.EqualTo(Math.Sqrt(size)).Within(1E-16));
    }

    [Test]
    public void ComplexVectorNorm([Values(12, 25, 39)] int size)
    {
        CVector v = new(new DVector(size, 1d), new DVector(size, 1d));
        Assert.That(v.Norm(), Is.EqualTo(Math.Sqrt(size + size)).Within(1E-16));
    }

    [Test]
    public void VectorSquared([Values(12, 256, 999)] int size)
    {
        DVector v = new(size, Random.Shared);
        Assert.That(v.Squared(), Is.EqualTo(v * v).Within(1E-13));
    }

    /// <summary>
    /// Check that vector subtraction yields a true zero vector.
    /// </summary>
    [Test]
    public void VectorDifference([Values(12, 25, 45)] int size)
    {
        DVector v = new(size, Random.Shared);
        Assert.That((v - v).Norm(), Is.EqualTo(0));
    }

    /// <summary>
    /// Check that vector subtraction yields a true zero vector.
    /// </summary>
    [Test]
    public void VectorDistance([Values(511)] int size)
    {
        DVector v = new(size, Random.Shared), v1 = v + 4;
        Assert.That(v.Distance(v1), Is.EqualTo(4));
    }

    [Test]
    public void CheckIndexOf()
    {
        DVector v = new(1024, Random.Shared);
        int index = Random.Shared.Next(1024);
        v[index] = Math.PI;
        Assert.That(v.IndexOf(Math.PI), Is.EqualTo(index));
    }

    [Test]
    public void CheckIndexOfFrom()
    {
        DVector v = new(1024, Random.Shared);
        int index1 = Random.Shared.Next(1024);
        int index2 = Random.Shared.Next(1024);
        while (index2 == index1)
            index2 = Random.Shared.Next(1024);
        if (index2 < index1)
            (index1, index2) = (index2, index1);
        v[index1] = Math.PI;
        v[index2] = Math.PI;
        int i1 = v.IndexOf(Math.PI), i2 = -1;
        if (i1 >= 0)
            i2 = v.IndexOf(Math.PI, i1 + 1);
        Assert.Multiple(() =>
        {
            Assert.That(i1, Is.EqualTo(index1));
            Assert.That(i2, Is.EqualTo(index2));
        });
    }

    [Test]
    public void CheckIndexOfNotFound()
    {
        DVector v = new(1024, Random.Shared);
        Assert.That(v.IndexOf(Math.PI), Is.EqualTo(-1));
    }

    [Test]
    public void CheckIndexOfFromNotFound()
    {
        DVector v = new(1024, Random.Shared);
        int index = Random.Shared.Next(1024);
        v[index] = Math.PI;
        int i1 = v.IndexOf(Math.PI), i2 = -1;
        if (i1 >= 0)
            i2 = v.IndexOf(Math.PI, i1 + 1);
        Assert.Multiple(() =>
        {
            Assert.That(i1, Is.EqualTo(index));
            Assert.That(i2, Is.EqualTo(-1));
        });
    }

    [Test]
    public void CheckMultiplyAdd()
    {
        DVector x = new(1024, Random.Shared);
        DVector y = new(1024, Random.Shared);
        DVector z = new(1024, Random.Shared);
        DVector d = x.PointwiseMultiply(y) + z;
        DVector e = x.MultiplyAdd(y, z);
        Assert.That((d - e).AMax(), Is.LessThan(1E-14));
    }

    [Test]
    public void CheckMultiplyAddScalar([Values(921, 1024)]int size)
    {
        DVector x = new(size, Random.Shared);
        double y = Random.Shared.NextDouble();
        DVector z = new(size, Random.Shared);
        DVector d = x * y + z;
        DVector e = x.MultiplyAdd(y, z);
        Assert.That((d - e).AMax(), Is.LessThan(1E-14));
    }

    [Test]
    public void CheckMultiplySubtract()
    {
        DVector x = new(1024, Random.Shared);
        DVector y = new(1024, Random.Shared);
        DVector z = new(1024, Random.Shared);
        DVector d = x.PointwiseMultiply(y) - z;
        DVector e = x.MultiplySubtract(y, z);
        Assert.That((d - e).AMax(), Is.LessThan(1E-14));
    }

    [Test]
    public void CheckMultiplySubtractScalar([Values(921, 1024)] int size)
    {
        DVector x = new(size, Random.Shared);
        double y = Random.Shared.NextDouble();
        DVector z = new(size, Random.Shared);
        DVector d = x * y - z;
        DVector e = x.MultiplySubtract(y, z);
        Assert.That((d - e).AMax(), Is.LessThan(1E-14));
    }

    [Test]
    public void CheckVectorSqrt([Values(12, 256, 1023)] int size)
    {
        DVector x = new(size, Random.Shared);
        DVector y = x.Sqrt();
        Assert.That(x, Has.Length.EqualTo(y.Length));
        for (int i = 0; i < x.Length; i++)
            Assert.That(y[i], Is.EqualTo(Math.Sqrt(x[i])));
    }

    [Test]
    public void CheckVectorAbs([Values(12, 256, 1023)] int size)
    {
        DVector x = new DVector(size, NormalRandom.Shared) * 2;
        DVector y = x.Abs();
        Assert.That(x, Has.Length.EqualTo(y.Length));
        for (int i = 0; i < x.Length; i++)
            Assert.That(y[i], Is.EqualTo(Math.Abs(x[i])));
    }

    [Test]
    public void CheckComplexVectorCtor() 
    {
        Complex[] values = new Complex[Random.Shared.Next(1023)];
        for (int i = 0; i < values.Length; i++)
            values[i] = new(Random.Shared.NextDouble(), Random.Shared.NextDouble());
        CVector v = new(values);
        Assert.That(v, Has.Length.EqualTo(values.Length));
        for (int i = 0; i < values.Length; i++)
            Assert.That(v[i], Is.EqualTo(values[i]));
    }

    [Test]
    public void CheckComplexVector2Array()
    {
        Complex[] values = new Complex[Random.Shared.Next(1023)];
        for (int i = 0; i < values.Length; i++)
            values[i] = new(Random.Shared.NextDouble(), Random.Shared.NextDouble());
        CVector v = new(values);
        Complex[] w = (Complex[])v;
        Assert.That(w, Is.EqualTo(values));
    }

    [Test]
    public void CheckComplexVector2ComplexArray()
    {
        CVector v = new(515, Random.Shared);
        Complex[] values = (Complex[])v;
        Assert.That(v, Has.Length.EqualTo(values.Length));
        for (int i = 0; i < values.Length; i++)
            Assert.That(v[i], Is.EqualTo(values[i]));
    }

    [Test]
    public void CheckComplexVectorScale()
    {
        CVector v = new(510, NormalRandom.Shared);
        double scale = Random.Shared.NextDouble() + 0.5;
        CVector w = v * scale;
        Assert.That(w, Has.Length.EqualTo(v.Length));
        for (int i = 0; i < w.Length; i++)
            Assert.That((w[i] - scale * v[i]).Magnitude, Is.LessThan(1E-15));
    }

    [Test]
    public void CheckComplexVectorPointMult()
    {
        CVector v = new(510, NormalRandom.Shared);
        CVector w = new(510, NormalRandom.Shared);
        CVector z = v.PointwiseMultiply(w);
        Assert.That(z, Has.Length.EqualTo(v.Length));
        for (int i = 0; i < v.Length; i++)
            Assert.That((z[i] - v[i] * w[i]).Magnitude, Is.LessThan(2E-14));
    }

    [Test]
    public void CheckComplexVectorPointDiv()
    {
        CVector v = new(510, NormalRandom.Shared);
        CVector w = new(510, NormalRandom.Shared);
        CVector z = v.PointwiseDivide(w);
        Assert.That(z, Has.Length.EqualTo(v.Length));
        for (int i = 0; i < v.Length; i++)
            Assert.That((z[i] - v[i] / w[i]).Magnitude, Is.LessThan(2E-14));
    }

    [Test]
    public void CheckPointwiseMultDiv()
    {
        CVector x = new CVector(510, Random.Shared) + 0.1;
        CVector y = new CVector(510, Random.Shared) + 0.1;
        CVector t = x.PointwiseDivide(y).PointwiseMultiply(y) - x;
        Assert.That(t.AbsMax(), Is.LessThan(1E-14));
    }

    [Test]
    public void CheckComplexPhase()
    {
        CVector x = new CVector(510, Random.Shared) + 0.1;
        int i = Random.Shared.Next(510);
        double phase1 = x.Phases()[i];
        double phase2 = x[i].Phase;
        Assert.That(phase1, Is.EqualTo(phase2).Within(1E-14));
    }

    [Test]
    public void CheckInplaceNegation()
    {
        DVector v1 = new(1024, Random.Shared);
        DVector v2 = -v1;
        Assert.That(v1, Is.EqualTo(v2.InplaceNegate()));
    }
}