namespace Austra.Parser;

internal readonly struct ColorChanger : IDisposable
{
    private readonly ConsoleColor originalColor;

    public ColorChanger(ConsoleColor color)
    {
        originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
    }

    public void Dispose() =>
        Console.ForegroundColor = originalColor;
}
