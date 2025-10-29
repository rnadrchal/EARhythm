using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace ImageSequencer.Behaviors;

public class DropBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.Register(nameof(DropCommand), typeof(ICommand), typeof(DropBehavior));

    public ICommand DropCommand
    {
        get => (ICommand)GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.AllowDrop = true;
        AssociatedObject.Drop += OnDrop;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Drop -= OnDrop;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DropCommand?.CanExecute(e) == true)
            DropCommand.Execute(e);
    }
}