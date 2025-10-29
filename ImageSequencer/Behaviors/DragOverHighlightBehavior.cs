using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace ImageSequencer.Behaviors;

public class DragOverHighlightBehavior : Behavior<Border>
{
    public Brush HighlightBrush
    {
        get => (Brush)GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.Register(nameof(HighlightBrush), typeof(Brush), typeof(DragOverHighlightBehavior), new PropertyMetadata(Brushes.Green));

    private Brush? _originalBrush;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.AllowDrop = true;
        AssociatedObject.DragOver += OnDragOver;
        AssociatedObject.DragLeave += OnDragLeave;
        AssociatedObject.Drop += OnDrop;
        _originalBrush = AssociatedObject.BorderBrush;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.DragOver -= OnDragOver;
        AssociatedObject.DragLeave -= OnDragLeave;
        AssociatedObject.Drop -= OnDrop;
        AssociatedObject.BorderBrush = _originalBrush;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            AssociatedObject.BorderBrush = HighlightBrush;
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            AssociatedObject.BorderBrush = Brushes.Red;
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        AssociatedObject.BorderBrush = _originalBrush;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        AssociatedObject.BorderBrush = _originalBrush;
    }
}