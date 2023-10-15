namespace Austra;

/// <summary>
/// Base class implementing <see cref="INotifyPropertyChanged"/>.
/// </summary>
[Serializable]
public abstract class Entity : INotifyPropertyChanged
{
    private static readonly PropertyChangedEventArgs nullArgs = new(null);

    /// <summary>Implementation for the <see cref="PropertyChanged"/> event.</summary>
    [NonSerialized]
    private PropertyChangedEventHandler? notifier;

    /// <summary>Notifies that all properties must be refreshed.</summary>
    public void NotifyChanges() => notifier?.Invoke(this, nullArgs);

    /// <summary>Notifies about a property that has changed.</summary>
    /// <param name="propertyName">Name of the changed property.</param>
    public void OnPropertyChanged(string propertyName) =>
        notifier?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected void OnPropertyChanged(PropertyChangedEventArgs args) =>
        notifier?.Invoke(this, args);

    protected void OnPropertyChanged(string propertyName1, string propertyName2)
    {
        var pc = notifier;
        if (pc != null)
        {
            pc(this, new PropertyChangedEventArgs(propertyName1));
            pc(this, new PropertyChangedEventArgs(propertyName2));
        }
    }

    protected void OnPropertyChanged(string pName1, string pName2, string pName3)
    {
        var pc = notifier;
        if (pc != null)
        {
            pc(this, new PropertyChangedEventArgs(pName1));
            pc(this, new PropertyChangedEventArgs(pName2));
            pc(this, new PropertyChangedEventArgs(pName3));
        }
    }

    protected void OnPropertyChanged(params string[] names)
    {
        var pc = notifier;
        if (pc != null)
            foreach (string name in names)
                pc(this, new(name));
    }

    protected bool SetField<T>(
        ref T field, T value,
        [CallerMemberName] string property = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        notifier?.Invoke(this, new(property));
        return true;
    }

    protected bool SetFieldAndProp<T>(
        ref T field,
        T value,
        string additionalProperty,
        [CallerMemberName] string property = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        var pc = notifier;
        if (pc != null)
        {
            pc(this, new(property));
            pc(this, new(additionalProperty));
        }
        return true;
    }

    /// <summary>Occurs when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add { notifier += value; }
        remove { notifier -= value; }
    }

    /// <summary>
    /// Cleans any subscribers to the <see cref="PropertyChanged"/> event.
    /// Must only be called for memberwise clones of an <see cref="Entity"/>.
    /// </summary>
    public void CleanNotifier() => notifier = null;
}
