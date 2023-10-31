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
        Vector v = new(size, 1.0);
        Assert.That(v.Norm(), Is.EqualTo(Math.Sqrt(size)).Within(1E-16));
    }

    [Test]
    public void VectorSquared([Values(12, 256, 999)] int size)
    {
        Vector v = new(size, Random.Shared);
        Assert.That(v.Squared(), Is.EqualTo(v * v).Within(1E-13));
    }

    /// <summary>
    /// Check that vector subtraction yields a true zero vector.
    /// </summary>
    [Test]
    public void VectorDifference([Values(12, 25, 45)] int size)
    {
        Vector v = new(size, Random.Shared);
        Assert.That((v - v).Norm(), Is.EqualTo(0));
    }

    /// <summary>
    /// Check that vector subtraction yields a true zero vector.
    /// </summary>
    [Test]
    public void VectorDistance([Values(511)] int size)
    {
        Vector v = new(size, Random.Shared), v1 = v + 4;
        Assert.That(v.Distance(v1), Is.EqualTo(4));
    }

    [Test]
    public void CheckIndexOf()
    {
        Vector v = new(1024, Random.Shared);
        int index = Random.Shared.Next(1024);
        v[index] = Math.PI;
        Assert.That(v.IndexOf(Math.PI), Is.EqualTo(index));
    }

    [Test]
    public void CheckIndexOfFrom()
    {
        Vector v = new(1024, Random.Shared);
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
        Vector v = new(1024, Random.Shared);
        Assert.That(v.IndexOf(Math.PI), Is.EqualTo(-1));
    }

    [Test]
    public void CheckIndexOfFromNotFound()
    {
        Vector v = new(1024, Random.Shared);
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
        Vector x = new(1024, Random.Shared);
        Vector y = new(1024, Random.Shared);
        Vector z = new(1024, Random.Shared);
        Vector d = x.PointwiseMultiply(y) + z;
        Vector e = x.MultiplyAdd(y, z);
        Assert.That((d - e).AMax(), Is.LessThan(1E-14));
    }

    [Test]
    public void CheckMultiplyAddScalar()
    {
        Vector x = new(1024, Random.Shared);
        double y = Random.Shared.NextDouble();
        Vector z = new(1024, Random.Shared);
        Vector d = x * y + z;
        Vector e = x.MultiplyAdd(y, z);
        Assert.That((d - e).AMax(), Is.LessThan(1E-14));
    }

    [Test]
    public void CheckMultiplySubtract()
    {
        Vector x = new(1024, Random.Shared);
        Vector y = new(1024, Random.Shared);
        Vector z = new(1024, Random.Shared);
        Vector d = x.PointwiseMultiply(y) - z;
        Vector e = x.MultiplySubtract(y, z);
        Assert.That((d - e).AMax(), Is.LessThan(1E-14));
    }

    [Test]
    public void CheckMultiplySubtractScalar()
    {
        Vector x = new(1024, Random.Shared);
        double y = Random.Shared.NextDouble();
        Vector z = new(1024, Random.Shared);
        Vector d = x * y - z;
        Vector e = x.MultiplySubtract(y, z);
        Assert.That((d - e).AMax(), Is.LessThan(1E-14));
    }

    [Test]
    public void CheckVectorSqrt([Values(12, 256, 1023)] int size)
    {
        Vector x = new(size, Random.Shared);
        Vector y = x.Sqrt();
        Assert.That(x, Has.Length.EqualTo(y.Length));
        for (int i = 0; i < x.Length; i++)
            Assert.That(y[i], Is.EqualTo(Math.Sqrt(x[i])));
    }

    [Test]
    public void CheckVectorAbs([Values(12, 256, 1023)] int size)
    {
        Vector x = new Vector(size, NormalRandom.Shared) * 2;
        Vector y = x.Abs();
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
        ComplexVector v = new(values);
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
        ComplexVector v = new(values);
        Complex[] w = (Complex[])v;
        Assert.That(w, Is.EqualTo(values));
    }

    [Test]
    public void CheckComplexVector2ComplexArray()
    {
        ComplexVector v = new(515, Random.Shared);
        Complex[] values = (Complex[])v;
        Assert.That(v, Has.Length.EqualTo(values.Length));
        for (int i = 0; i < values.Length; i++)
            Assert.That(v[i], Is.EqualTo(values[i]));
    }

    [Test]
    public void CheckComplexVectorScale()
    {
        ComplexVector v = new(510, NormalRandom.Shared);
        double scale = Random.Shared.NextDouble() + 0.5;
        ComplexVector w = v * scale;
        Assert.That(w, Has.Length.EqualTo(v.Length));
        for (int i = 0; i < w.Length; i++)
            Assert.That((w[i] - scale * v[i]).Magnitude, Is.LessThan(1E-15));
    }

    [Test]
    public void CheckComplexVectorPointMult()
    {
        ComplexVector v = new(510, NormalRandom.Shared);
        ComplexVector w = new(510, NormalRandom.Shared);
        ComplexVector z = v.PointwiseMultiply(w);
        Assert.That(z, Has.Length.EqualTo(v.Length));
        for (int i = 0; i < v.Length; i++)
            Assert.That((z[i] - v[i] * w[i]).Magnitude, Is.LessThan(1E-14));
    }

    [Test]
    public void CheckComplexVectorPointDiv()
    {
        ComplexVector v = new(510, NormalRandom.Shared);
        ComplexVector w = new(510, NormalRandom.Shared);
        ComplexVector z = v.PointwiseDivide(w);
        Assert.That(z, Has.Length.EqualTo(v.Length));
        for (int i = 0; i < v.Length; i++)
            Assert.That((z[i] - v[i] / w[i]).Magnitude, Is.LessThan(2E-14));
    }

    [Test]
    public void CheckPointwiseMultDiv()
    {
        ComplexVector x = new ComplexVector(510, Random.Shared) + 0.1;
        ComplexVector y = new ComplexVector(510, Random.Shared) + 0.1;
        ComplexVector t = x.PointwiseDivide(y).PointwiseMultiply(y) - x;
        Assert.That(t.AbsMax(), Is.LessThan(1E-14));
    }
}