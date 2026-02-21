using System.Collections.ObjectModel;
using CardViewer.Models;
using CardViewer.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CardViewer.ViewModels;

public partial class StructuredEditorViewModel : ViewModelBase
{
    private readonly OutlineParser _parser = new();

    [ObservableProperty]
    private ObservableCollection<OutlineNode> _nodes = new();

    [ObservableProperty]
    private OutlineNode? _selectedNode;

    [ObservableProperty]
    private string _parseStatus = "0 items, 0 cards";

    [RelayCommand]
    private void AddItem()
    {
        var newNode = new OutlineNode { Title = "New item" };

        if (SelectedNode == null)
        {
            Nodes.Add(newNode);
        }
        else if (SelectedNode.Parent != null)
        {
            // Add as sibling after selected
            var siblings = SelectedNode.Parent.Children;
            var idx = siblings.IndexOf(SelectedNode);
            newNode.Parent = SelectedNode.Parent;
            siblings.Insert(idx + 1, newNode);
        }
        else
        {
            // Selected is a root node — add sibling after it
            var idx = Nodes.IndexOf(SelectedNode);
            Nodes.Insert(idx + 1, newNode);
        }

        SelectedNode = newNode;
        UpdateStatus();
    }

    [RelayCommand]
    private void AddChild()
    {
        if (SelectedNode == null)
        {
            AddItem();
            return;
        }

        var newNode = new OutlineNode { Title = "New item" };
        newNode.Parent = SelectedNode;
        SelectedNode.Children.Add(newNode);
        SelectedNode = newNode;
        UpdateStatus();
    }

    [RelayCommand]
    private void RemoveItem()
    {
        if (SelectedNode == null) return;

        var toRemove = SelectedNode;
        var parent = toRemove.Parent;

        // Pick next selection before removing
        OutlineNode? nextSelection = null;
        if (parent != null)
        {
            var siblings = parent.Children;
            var idx = siblings.IndexOf(toRemove);
            if (idx > 0) nextSelection = siblings[idx - 1];
            else if (siblings.Count > 1) nextSelection = siblings[1];
            else nextSelection = parent;

            siblings.Remove(toRemove);
        }
        else
        {
            var idx = Nodes.IndexOf(toRemove);
            if (idx > 0) nextSelection = Nodes[idx - 1];
            else if (Nodes.Count > 1) nextSelection = Nodes[1];

            Nodes.Remove(toRemove);
        }

        SelectedNode = nextSelection;
        UpdateStatus();
    }

    [RelayCommand]
    private void Promote()
    {
        // Move selected node up one level (become sibling of its parent)
        if (SelectedNode?.Parent == null) return;

        var node = SelectedNode;
        var parent = node.Parent;
        var grandparent = parent.Parent;

        var idxInParent = parent.Children.IndexOf(node);
        parent.Children.Remove(node);

        // Any siblings after this node become children of this node
        while (parent.Children.Count > idxInParent)
        {
            var sibling = parent.Children[idxInParent];
            parent.Children.RemoveAt(idxInParent);
            sibling.Parent = node;
            node.Children.Add(sibling);
        }

        if (grandparent != null)
        {
            var parentIdx = grandparent.Children.IndexOf(parent);
            node.Parent = grandparent;
            grandparent.Children.Insert(parentIdx + 1, node);
        }
        else
        {
            var parentIdx = Nodes.IndexOf(parent);
            node.Parent = null;
            Nodes.Insert(parentIdx + 1, node);
        }

        SelectedNode = node;
        UpdateStatus();
    }

    [RelayCommand]
    private void Demote()
    {
        // Make selected node a child of its previous sibling
        if (SelectedNode == null) return;

        var node = SelectedNode;
        ObservableCollection<OutlineNode> siblings;

        if (node.Parent != null)
            siblings = node.Parent.Children;
        else
            siblings = Nodes;

        var idx = siblings.IndexOf(node);
        if (idx <= 0) return; // No previous sibling to nest under

        var prevSibling = siblings[idx - 1];
        siblings.Remove(node);
        node.Parent = prevSibling;
        prevSibling.Children.Add(node);

        SelectedNode = node;
        UpdateStatus();
    }

    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedNode == null) return;

        var node = SelectedNode;
        var siblings = node.Parent != null ? node.Parent.Children : Nodes;
        var idx = siblings.IndexOf(node);
        if (idx <= 0) return;

        siblings.Move(idx, idx - 1);
        SelectedNode = node;
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedNode == null) return;

        var node = SelectedNode;
        var siblings = node.Parent != null ? node.Parent.Children : Nodes;
        var idx = siblings.IndexOf(node);
        if (idx >= siblings.Count - 1) return;

        siblings.Move(idx, idx + 1);
        SelectedNode = node;
    }

    public void LoadFromText(string text)
    {
        var result = _parser.Parse(text);
        Nodes = new ObservableCollection<OutlineNode>(result.Nodes);
        UpdateStatus();
    }

    public string ToMarkdownText()
    {
        var lines = new List<string>();
        foreach (var node in Nodes)
            AppendNode(node, 0, lines);
        return string.Join("\n", lines);
    }

    public List<OutlineNode> GetNodes() => Nodes.ToList();

    private static void AppendNode(OutlineNode node, int depth, List<string> lines)
    {
        lines.Add(new string(' ', depth * 3) + node.Title);
        foreach (var child in node.Children)
            AppendNode(child, depth + 1, lines);
    }

    private void UpdateStatus()
    {
        int total = CountAll(Nodes);
        int leaves = CountLeaves(Nodes);
        ParseStatus = $"{total} items, {leaves} cards";
    }

    private static int CountAll(IEnumerable<OutlineNode> nodes)
    {
        int c = 0;
        foreach (var n in nodes) { c++; c += CountAll(n.Children); }
        return c;
    }

    private static int CountLeaves(IEnumerable<OutlineNode> nodes)
    {
        int c = 0;
        foreach (var n in nodes)
        {
            if (n.IsLeaf) c++;
            else c += CountLeaves(n.Children);
        }
        return c;
    }
}
