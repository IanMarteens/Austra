using System.Windows.Media;

namespace Ostara;

/// <summary>
/// This class provides the ability to easily attach Help functionality 
/// to Framework elements. To use it, you need to
/// add a reference to the HelpProvider in your XAML. The FilenameProperty
/// is used to specify the name of the helpfile, and the KeywordProperty specifies
/// the keyword to be used with the search.
/// </summary>
/// <remarks>
/// The FilenameProperty can be at a higher level of the visual tree than
/// the KeywordProperty, so you don't need to set the filename each time.
/// </remarks>
public static class Help
{
    /// <summary>Initialize a new instance of <see cref="Help"/>.</summary>
    static Help() =>
        // Rather than having to manually associate the Help command, let's take care of this here.
        CommandManager.RegisterClassCommandBinding(
            typeof(FrameworkElement),
            new CommandBinding(ApplicationCommands.Help, Executed, CanExecute));

    /// <summary>Filename Attached Dependency Property.</summary>
    public static readonly DependencyProperty FilenameProperty =
        DependencyProperty.RegisterAttached("Filename", typeof(string), typeof(Help));

    /// <summary>Gets the Filename property.</summary>
    public static string GetFilename(DependencyObject d) =>
        (string)d.GetValue(FilenameProperty);

    /// <summary>Sets the Filename property.</summary>
    public static void SetFilename(DependencyObject d, string value) =>
        d.SetValue(FilenameProperty, value);

    /// <summary>Keyword Attached Dependency Property.</summary>
    public static readonly DependencyProperty KeywordProperty =
        DependencyProperty.RegisterAttached("Keyword", typeof(string), typeof(Help));

    /// <summary>Gets the Keyword property.</summary>
    public static string GetKeyword(DependencyObject d) =>
        (string)d.GetValue(KeywordProperty);

    /// <summary>Sets the Keyword property.</summary>
    public static void SetKeyword(DependencyObject d, string value) =>
        d.SetValue(KeywordProperty, value);

    private static void CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (sender is FrameworkElement el)
        {
            string? fileName = FindFilename(el);
            if (!string.IsNullOrEmpty(fileName))
                args.CanExecute = true;
        }
    }

    private static void Executed(object sender, ExecutedRoutedEventArgs args)
    {
        // Call ShowHelp.
        DependencyObject? parent = args.OriginalSource as DependencyObject;
        string? keyword = FindKeyword(parent);
        if (!string.IsNullOrEmpty(keyword))
            Show(FindFilename(parent), keyword);
        else
            Show(FindFilename(parent));

        static string? FindKeyword(DependencyObject? sender)
        {
            while (sender != null)
            {
                string keyword = GetKeyword(sender);
                if (!string.IsNullOrEmpty(keyword))
                    return keyword;
                sender = VisualTreeHelper.GetParent(sender);
            }
            return null;
        }
    }

    private static string? FindFilename(DependencyObject? sender)
    {
        while (sender != null)
        {
            string fileName = GetFilename(sender);
            if (!string.IsNullOrEmpty(fileName))
                return fileName;
            sender = VisualTreeHelper.GetParent(sender);
        }
        return null;
    }

    public static void Show(string? fileName) =>
        System.Windows.Forms.Help.ShowHelp(null, fileName);

    public static void Show(string? fileName, string keyword) =>
        System.Windows.Forms.Help.ShowHelp(
            null,
            fileName,
            System.Windows.Forms.HelpNavigator.TopicId,
            keyword);
}
