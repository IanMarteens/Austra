namespace Ostara;

/// <summary>Commands for OSTARA.</summary>
public static class OstCommands
{
    public static RoutedCommand ParseType { get; } = new();
    public static RoutedCommand CloseAll {get; } = new ();
    public static RoutedCommand About { get; } = new();
    public static RoutedCommand HistoryUp { get; } = new();
    public static RoutedCommand HistoryDown { get; } = new();
    public static RoutedCommand FocusEditor { get; } = new();
    public static RoutedCommand PasteFromExcel { get; } = new();
}
