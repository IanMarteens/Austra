namespace Austra.Tests;

[TestFixture]
public class FftTests
{
    /// <summary>
    /// In a real FFT, the first element of the result is the sum of the input,
    /// aka as the Direct Current (DC) component.
    /// </summary>
    [Test]
    public void TestDC([Values(583, 1024, 1027)] int size)
    {
        Vector v = new(size, Random.Shared);
        Complex[] result = FFT.Transform((double[])v);
        Assert.That(result[0].Magnitude, Is.EqualTo(v.Sum()).Within(1E-12));
    }
}

