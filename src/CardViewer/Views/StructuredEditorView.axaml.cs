using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CardViewer.Models;
using CardViewer.ViewModels;

namespace CardViewer.Views;

public partial class StructuredEditorView : UserControl
{
    public StructuredEditorView()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not StructuredEditorViewModel vm)
            return;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.Enter when ctrl:
                vm.AddItemCommand.Execute(null);
                e.Handled = true;
                // Focus the new node's TextBox after the UI updates
                Dispatcher.UIThread.Post(FocusSelectedTextBox, DispatcherPriority.Background);
                break;
            case Key.Enter when !ctrl:
                // Close text edit — move focus to the tree
                if (e.Source is TextBox)
                {
                    OutlineTree.Focus();
                    e.Handled = true;
                }
                break;
            case Key.Tab when shift:
                vm.PromoteCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Tab when !shift:
                vm.DemoteCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete when e.Source is not TextBox:
            case Key.Back when ctrl && e.Source is not TextBox:
                vm.RemoveItemCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Up when ctrl:
                vm.MoveUpCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Down when ctrl:
                vm.MoveDownCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    public void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is OutlineNode node
            && DataContext is StructuredEditorViewModel vm)
        {
            vm.SelectedNode = node;
            vm.RemoveItemCommand.Execute(null);
        }
    }

    public void OnPreviewClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is OutlineNode node)
        {
            // Walk up to OutlineEditorView to raise the navigation event
            var editorView = this.FindAncestorOfType<OutlineEditorView>();
            if (editorView?.DataContext is OutlineEditorViewModel editorVm)
            {
                editorVm.RaiseNodeDoubleClicked(node);
            }
        }
    }

    private void FocusSelectedTextBox()
    {
        // Find the TreeViewItem for the selected node and focus its TextBox
        var selected = OutlineTree.SelectedItem;
        if (selected == null) return;

        var container = OutlineTree.ContainerFromItem(selected);
        if (container is TreeViewItem tvi)
        {
            var textBox = tvi.FindDescendantOfType<TextBox>();
            if (textBox != null)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        }
    }
}
