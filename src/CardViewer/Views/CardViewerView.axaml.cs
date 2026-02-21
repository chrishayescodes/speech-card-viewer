using Avalonia.Controls;
using Avalonia.Input;
using CardViewer.Models;
using CardViewer.ViewModels;

namespace CardViewer.Views;

public partial class CardViewerView : UserControl
{
    public CardViewerView()
    {
        InitializeComponent();
        OutlineSidebar.SelectionChanged += OnSidebarSelectionChanged;
    }

    private void OnSidebarSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is CardViewerViewModel vm &&
            OutlineSidebar.SelectedItem is OutlineNode node)
        {
            vm.NavigateToNodeCommand.Execute(node);
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is CardViewerViewModel vm)
        {
            switch (e.Key)
            {
                case Key.Right:
                case Key.Down:
                case Key.Space:
                case Key.Enter:
                    vm.NextCardCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Up:
                case Key.Back:
                    vm.PreviousCardCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
