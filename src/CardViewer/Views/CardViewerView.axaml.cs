using System.ComponentModel;
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

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is CardViewerViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            ApplySidebarWidth(vm);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CardViewerViewModel.IsSidebarOpen) &&
            sender is CardViewerViewModel vm)
        {
            ApplySidebarWidth(vm);
        }
    }

    private void ApplySidebarWidth(CardViewerViewModel vm)
    {
        var col = SidebarGrid.ColumnDefinitions[0];
        if (vm.IsSidebarOpen)
        {
            col.Width = new GridLength(vm.SidebarWidth);
            col.MinWidth = 120;
            col.MaxWidth = 500;
        }
        else
        {
            if (col.Width.IsAbsolute)
                vm.SidebarWidth = col.Width.Value;
            col.MinWidth = 0;
            col.MaxWidth = double.PositiveInfinity;
            col.Width = GridLength.Auto;
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        SaveSidebarWidth();
    }

    private void SaveSidebarWidth()
    {
        if (DataContext is CardViewerViewModel vm)
        {
            var col = SidebarGrid.ColumnDefinitions[0];
            if (col.Width.IsAbsolute)
                vm.SidebarWidth = col.Width.Value;
        }
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
