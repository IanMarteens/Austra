namespace Austra.Library;

/// <summary>Represents the result of a Fast Fourier Transform.</summary>
public abstract class FftModel
{
    /// <summary>Initializes a FftModel.</summary>
    /// <param name="spectrum">
    /// The whole spectrum, as returned by <see cref="FFT.Transform(Complex[])"/>.
    /// </param>
    protected FftModel(Complex[] spectrum) => Spectrum = new(spectrum);

    /// <summary>Gets the result of the FFT as a complex vector.</summary>
    public ComplexVector Spectrum { get; }

    /// <summary>Gets the amplitudes of the spectrum, as a vector of real numbers.</summary>
    public Vector Amplitudes { get; protected set; }

    /// <summary>Gets the phases of the spectrum, as a vector of real numbers.</summary>
    public Vector Phases { get; protected set; }

    /// <summary>
    /// Gets the length of the <see cref="Amplitudes"/> and <see cref="Phases"/> vectors.
    /// </summary>
    public int Length => Amplitudes.Length;

    /// <summary>Gets the complex number at the specified index.</summary>
    /// <param name="index">Index of the number inside the whole spectrum.</param>
    /// <returns>The complex value at the specified index.</returns>
    public Complex this[int index] => Spectrum[index];

    /// <summary>Gets the complex number at the specified index.</summary>
    /// <param name="index">Index of the number inside the whole spectrum.</param>
    /// <returns>The complex value at the specified index.</returns>
    public Complex this[Index index] => Spectrum[index];

    /// <summary>Extracts a slice from the spectrum.</summary>
    /// <param name="range">The range of the slice.</param>
    /// <returns>A new complex vector representing the slice.</returns>
    public ComplexVector this[Range range] => Spectrum[range];

    /// <summary>
    /// Calculates the amplitudes and phases of the spectrum, from the whole spectrum.
    /// </summary>
    protected abstract void Calculate();

    /// <summary>Gets a string representation of the FFT.</summary>
    override public string ToString() => Amplitudes.ToString();
}

/// <summary>Represents the result of a real Fast Fourier Transform.</summary>
public sealed class FftRModel : FftModel
{
    /// <summary>When true, cuts the spectrum in halves.</summary>
    private bool cut = true;

    /// <summary>Initializes a FftRModel.</summary>
    /// <param name="spectrum">
    /// The whole spectrum, as returned by <see cref="FFT.Transform(Complex[])"/>.
    /// </param>
    public FftRModel(Complex[] spectrum) : base(spectrum) => Calculate();

    /// <summary>Gets or sets a value indicating whether the spectrum is cut in halves.</summary>
    public bool Cut
    {
        get => cut;
        set
        {
            if (value != cut)
            {
                cut = value;
                Calculate();
            }
        }
    }

    /// <summary>
    /// Calculates the amplitudes and phases of the spectrum,
    /// from the whole spectrum or half of it.
    /// </summary>
    protected override void Calculate()
    {
        if (cut)
        {
            Amplitudes = Spectrum.Take(Spectrum.Length / 2).Select(c => c.Magnitude).ToArray();
            Phases = Spectrum.Take(Spectrum.Length / 2).Select(c => c.Phase).ToArray();
        }
        else
        {
            Amplitudes = Spectrum.Select(c => c.Magnitude).ToArray();
            Phases = Spectrum.Select(c => c.Phase).ToArray();
        }
    }

    /// <summary>Inverse of the FFT transform.</summary>
    /// <returns>The original samples.</returns>
    public Vector Inverse() => FFT.InverseReal((Complex[])Spectrum);
}

/// <summary>Represents the result of a complex Fast Fourier Transform.</summary>
public sealed class FftCModel : FftModel
{
    /// <summary>Initializes a FftRModel.</summary>
    /// <param name="spectrum">
    /// The whole spectrum, as returned by <see cref="FFT.Transform(Complex[])"/>.
    /// </param>
    public FftCModel(Complex[] spectrum) : base(spectrum) => Calculate();

    /// <summary>
    /// Calculates the amplitudes and phases of the spectrum, from the whole spectrum.
    /// </summary>
    protected override void Calculate()
    {
        Amplitudes = Spectrum.Select(c => c.Magnitude).ToArray();
        Phases = Spectrum.Select(c => c.Phase).ToArray();
    }
    /// <summary>Inverse of the FFT transform.</summary>
    /// <returns>The original samples.</returns>
    public ComplexVector Inverse()
    {
        Complex[] result = (Complex[])Spectrum.Clone();
        FFT.Inverse(result);
        return new(result);
    }
}

