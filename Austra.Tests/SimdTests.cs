using System.Runtime.Intrinsics;

namespace Austra.Tests;

[TestFixture]
public class SimdTests
{
    private readonly Random rnd = new();
    private readonly NormalRandom nrm = new();

    [Test]
    public void TestLog8d()
    {
        double m = rnd.NextDouble() < 0.5 ? 2 : 1;
        var v1 = Vector512.Create(
            m * rnd.NextDouble(), m * rnd.NextDouble(), m * rnd.NextDouble(), m * rnd.NextDouble(),
            m * rnd.NextDouble(), m * rnd.NextDouble(), m * rnd.NextDouble(), m * rnd.NextDouble());
        var ln1 = Vector512.Create(
            Math.Log(v1[0]), Math.Log(v1[1]), Math.Log(v1[2]), Math.Log(v1[3]),
            Math.Log(v1[4]), Math.Log(v1[5]), Math.Log(v1[6]), Math.Log(v1[7]));
        var ln2 = Simd.Log(v1);
        var d = ln1 - ln2;
        var diff = Vector512.Sum(d);
        Assert.That(Math.Abs(diff), Is.LessThan(1E-12));
    }

    [Test]
    public void TestAtan8d()
    {
        var v1 = Vector512.Create(
            nrm.NextDouble(), nrm.NextDouble(), nrm.NextDouble(), nrm.NextDouble(),
            nrm.NextDouble(), nrm.NextDouble(), nrm.NextDouble(), nrm.NextDouble());
        var v2 = Vector512.Create(
            nrm.NextDouble(), nrm.NextDouble(), nrm.NextDouble(), nrm.NextDouble(),
            nrm.NextDouble(), nrm.NextDouble(), nrm.NextDouble(), nrm.NextDouble());
        var at1 = Vector512.Create(
            Math.Atan2(v1[0], v2[0]), Math.Atan2(v1[1], v2[1]), Math.Atan2(v1[2], v2[2]), Math.Atan2(v1[3], v2[3]),
            Math.Atan2(v1[4], v2[4]), Math.Atan2(v1[5], v2[5]), Math.Atan2(v1[6], v2[6]), Math.Atan2(v1[7], v2[7]));
        var at2 = Simd.Atan2(v1, v2);
        var d = at1 - at2;
        var diff = Vector512.Sum(d);
        Assert.That(Math.Abs(diff), Is.LessThan(1E-12));
    }

    [Test]
    public void TestRandom()
    {
        Accumulator acc = new((double[])new DVector(1024));
        Assert.Multiple(() =>
        {
            Assert.That(acc.Minimum, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(acc.Maximum, Is.LessThan(1.0));
        });
    }
}
