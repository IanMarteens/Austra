﻿namespace Austra.Library.Transforms;

/// <summary>Represents the result of a Fast Fourier Transform.</summary>
/// <remarks>Initializes a FftModel.</remarks>
/// <param name="spectrum">
/// The whole spectrum, as returned by <see cref="FFT.Transform(Complex[])"/>.
/// </param>
public abstract class FftModel(Complex[] spectrum) : IIndexable
{

    /// <summary>Gets the result of the FFT as a complex vector.</summary>
    public CVector Spectrum { get; } = new(spectrum);

    /// <summary>Gets the amplitudes of the spectrum, as a vector of real numbers.</summary>
    public DVector Amplitudes { get; protected set; }

    /// <summary>Gets the phases of the spectrum, as a vector of real numbers.</summary>
    public DVector Phases { get; protected set; }

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
    public CVector this[Range range] => Spectrum[range];

    /// <summary>
    /// Calculates the amplitudes and phases of the spectrum, from the whole spectrum.
    /// </summary>
    protected abstract void Calculate();

    /// <summary>
    /// Gets a short string describing the kind of FFT and the length of the spectrum.
    /// </summary>
    /// <returns>
    /// Number of items in the original vector, and number of items in the spectrum.
    /// </returns>
    public abstract string ToShortString();

    /// <summary>Gets a string representation of the FFT.</summary>
    /// <returns>A string representation of the <see cref="Amplitudes"/> vector.</returns>
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
            Amplitudes = Spectrum.Magnitudes(Spectrum.Length / 2);
            Phases = Spectrum.Phases(Spectrum.Length / 2);
        }
        else
        {
            Amplitudes = Spectrum.Magnitudes();
            Phases = Spectrum.Phases();
        }
    }

    /// <summary>A short description of the model's content.</summary>
    /// <returns>
    /// Number of items in the original vector, and number of items in the spectrum.
    /// </returns>
    public override string ToShortString() =>
        $"FFT : ℝ({Spectrum.Length}) ⊢ ℂ({Length})";

    /// <summary>Inverse of the FFT transform.</summary>
    /// <returns>The original samples.</returns>
    public DVector Inverse() => FFT.InverseReal((Complex[])Spectrum);
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
        Amplitudes = Spectrum.Magnitudes();
        Phases = Spectrum.Phases();
    }

    /// <summary>A short description of the model's content.</summary>
    /// <returns>
    /// Number of items in the original vector, and number of items in the spectrum.
    /// </returns>
    public override string ToShortString() =>
        $"FFT : ℂ({Spectrum.Length}) ⊢ ℂ({Length})";

    /// <summary>Inverse of the FFT transform.</summary>
    /// <returns>The original samples.</returns>
    public CVector Inverse()
    {
        Complex[] result = (Complex[])Spectrum.Clone();
        FFT.Inverse(result);
        return new(result);
    }
}

