namespace Austra;

public class DelegateCommand : ICommand
{
    private readonly Action<object?> execute;
    private readonly Predicate<object?>? canExecute;

    public DelegateCommand(Action<object?> execute, Predicate<object?> canExecute) =>
        (this.execute, this.canExecute) = (execute, canExecute);

    public DelegateCommand(Action execute, Predicate<object?> canExecute) =>
        (this.execute, this.canExecute) = (_ => execute(), canExecute);

    public DelegateCommand(Action<object?> execute, Func<bool> canExecute) =>
        (this.execute, this.canExecute) = (execute, _ => canExecute());

    public DelegateCommand(Action execute, Func<bool> canExecute) =>
        (this.execute, this.canExecute) = (_ => execute(), _ => canExecute());

    public DelegateCommand(Action<object?> execute) => this.execute = execute;

    public DelegateCommand(Action execute) => this.execute = _ => execute();

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) =>
        canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) =>
        execute(parameter);
}

