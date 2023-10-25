namespace Austra;

internal static class GreekSymbols
{
    private static readonly Dictionary<Key, char> tmgLo = new()
    {
        [Key.A] = 'α',
        [Key.B] = 'β',
        [Key.C] = 'ψ',
        [Key.D] = 'δ',
        [Key.E] = 'ε',
        [Key.F] = 'φ',
        [Key.G] = 'γ',
        [Key.H] = 'η',
        [Key.J] = 'ξ',
        [Key.L] = 'λ',
        [Key.M] = 'μ',
        [Key.N] = 'ν',
        [Key.O] = 'ω',
        [Key.P] = 'π',
        [Key.R] = 'ρ',
        [Key.S] = 'σ',
        [Key.T] = 'τ',
        [Key.U] = 'Θ',
        [Key.Z] = 'ζ',
    };
    private static readonly Dictionary<Key, char> tmgUp = new()
    {
        [Key.C] = 'Ψ',
        [Key.D] = 'Δ',
        [Key.G] = 'Γ',
        [Key.F] = 'Φ',
        [Key.J] = 'Ξ',
        [Key.L] = 'Λ',
        [Key.O] = 'Ω',
        [Key.P] = 'Π',
        [Key.S] = 'Σ',
        [Key.U] = 'θ',
    };

    /// <summary>Checks if a key can be transformed into a Greek letter. </summary>
    /// <param name="key">The original key.</param>
    /// <param name="newChar">The translated key, or the default value.</param>
    /// <returns><see langword="true"/> if the key is accepted for translation.</returns>
    public static bool TryTransform(Key key, out char newChar)
    {
        newChar = default;
        return Keyboard.Modifiers == ModifierKeys.None && tmgLo.TryGetValue(key, out newChar)
            || Keyboard.Modifiers == ModifierKeys.Shift && tmgUp.TryGetValue(key, out newChar);
    }
}
