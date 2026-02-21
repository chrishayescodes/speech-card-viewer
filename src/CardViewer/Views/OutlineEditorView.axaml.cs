using Avalonia.Controls;
using Avalonia.Interactivity;
using CardViewer.Models;
using CardViewer.ViewModels;

namespace CardViewer.Views;

public partial class OutlineEditorView : UserControl
{
    public OutlineEditorView()
    {
        InitializeComponent();
    }

    public void OnPreviewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is OutlineEditorViewModel vm &&
            sender is Button btn && btn.DataContext is OutlineNode node)
        {
            vm.RaiseNodeDoubleClicked(node);
        }
    }
}
