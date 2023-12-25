using System.Runtime.Intrinsics;

namespace Austra.Tests;

[TestFixture]
public class SimdTests
{
    private readonly Random rnd = new();

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
}
