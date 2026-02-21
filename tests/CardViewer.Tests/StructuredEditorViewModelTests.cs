using CardViewer.ViewModels;

namespace CardViewer.Tests;

public class StructuredEditorViewModelTests
{
    private readonly StructuredEditorViewModel _vm = new();

    // --- AddItem ---

    [Fact]
    public void AddItem_EmptyList_AddsOneNode()
    {
        _vm.AddItemCommand.Execute(null);

        Assert.Single(_vm.Nodes);
        Assert.Equal("New item", _vm.Nodes[0].Title);
        Assert.Same(_vm.SelectedNode, _vm.Nodes[0]);
    }

    [Fact]
    public void AddItem_WithSelectedRoot_AddsSiblingAfter()
    {
        _vm.LoadFromText("First\nSecond");
        _vm.SelectedNode = _vm.Nodes[0];

        _vm.AddItemCommand.Execute(null);

        Assert.Equal(3, _vm.Nodes.Count);
        Assert.Equal("New item", _vm.Nodes[1].Title);
        Assert.Equal("Second", _vm.Nodes[2].Title);
        Assert.Same(_vm.SelectedNode, _vm.Nodes[1]);
    }

    [Fact]
    public void AddItem_WithSelectedChild_AddsSiblingAfterChild()
    {
        _vm.LoadFromText("Parent\n   Child1\n   Child2");
        _vm.SelectedNode = _vm.Nodes[0].Children[0]; // Child1

        _vm.AddItemCommand.Execute(null);

        Assert.Equal(3, _vm.Nodes[0].Children.Count);
        Assert.Equal("Child1", _vm.Nodes[0].Children[0].Title);
        Assert.Equal("New item", _vm.Nodes[0].Children[1].Title);
        Assert.Equal("Child2", _vm.Nodes[0].Children[2].Title);
    }

    // --- AddChild ---

    [Fact]
    public void AddChild_WithSelectedNode_AddsChildToSelected()
    {
        _vm.LoadFromText("Parent");
        _vm.SelectedNode = _vm.Nodes[0];

        _vm.AddChildCommand.Execute(null);

        Assert.Single(_vm.Nodes[0].Children);
        Assert.Equal("New item", _vm.Nodes[0].Children[0].Title);
        Assert.Same(_vm.Nodes[0], _vm.Nodes[0].Children[0].Parent);
        Assert.Same(_vm.SelectedNode, _vm.Nodes[0].Children[0]);
    }

    [Fact]
    public void AddChild_NothingSelected_FallsBackToAddItem()
    {
        _vm.SelectedNode = null;
        _vm.AddChildCommand.Execute(null);

        Assert.Single(_vm.Nodes);
        Assert.Equal("New item", _vm.Nodes[0].Title);
    }

    // --- RemoveItem ---

    [Fact]
    public void RemoveItem_SelectsPreviousSibling()
    {
        _vm.LoadFromText("First\nSecond\nThird");
        _vm.SelectedNode = _vm.Nodes[1]; // Second

        _vm.RemoveItemCommand.Execute(null);

        Assert.Equal(2, _vm.Nodes.Count);
        Assert.Equal("First", _vm.SelectedNode?.Title);
    }

    [Fact]
    public void RemoveItem_FirstItem_SelectsNext()
    {
        _vm.LoadFromText("First\nSecond");
        _vm.SelectedNode = _vm.Nodes[0];

        _vm.RemoveItemCommand.Execute(null);

        Assert.Single(_vm.Nodes);
        Assert.Equal("Second", _vm.SelectedNode?.Title);
    }

    [Fact]
    public void RemoveItem_LastChild_SelectsParent()
    {
        _vm.LoadFromText("Parent\n   OnlyChild");
        _vm.SelectedNode = _vm.Nodes[0].Children[0];

        _vm.RemoveItemCommand.Execute(null);

        Assert.Empty(_vm.Nodes[0].Children);
        Assert.Equal("Parent", _vm.SelectedNode?.Title);
    }

    [Fact]
    public void RemoveItem_NothingSelected_DoesNothing()
    {
        _vm.LoadFromText("Item");
        _vm.SelectedNode = null;

        _vm.RemoveItemCommand.Execute(null);

        Assert.Single(_vm.Nodes);
    }

    // --- Promote ---

    [Fact]
    public void Promote_ChildBecomesSiblingOfParent()
    {
        _vm.LoadFromText("Parent\n   Child");
        _vm.SelectedNode = _vm.Nodes[0].Children[0]; // Child

        _vm.PromoteCommand.Execute(null);

        Assert.Equal(2, _vm.Nodes.Count);
        Assert.Equal("Parent", _vm.Nodes[0].Title);
        Assert.Equal("Child", _vm.Nodes[1].Title);
        Assert.Null(_vm.Nodes[1].Parent);
    }

    [Fact]
    public void Promote_AdoptsSubsequentSiblings()
    {
        _vm.LoadFromText("Parent\n   A\n   B\n   C");
        _vm.SelectedNode = _vm.Nodes[0].Children[1]; // B

        _vm.PromoteCommand.Execute(null);

        // B becomes root sibling of Parent; C becomes child of B
        Assert.Equal(2, _vm.Nodes.Count);
        Assert.Equal("Parent", _vm.Nodes[0].Title);
        Assert.Equal("B", _vm.Nodes[1].Title);
        Assert.Single(_vm.Nodes[0].Children); // Only A remains
        Assert.Single(_vm.Nodes[1].Children); // C moved under B
        Assert.Equal("C", _vm.Nodes[1].Children[0].Title);
    }

    [Fact]
    public void Promote_RootNode_DoesNothing()
    {
        _vm.LoadFromText("Root");
        _vm.SelectedNode = _vm.Nodes[0];

        _vm.PromoteCommand.Execute(null);

        Assert.Single(_vm.Nodes);
        Assert.Equal("Root", _vm.Nodes[0].Title);
    }

    // --- Demote ---

    [Fact]
    public void Demote_NestsUnderPreviousSibling()
    {
        _vm.LoadFromText("First\nSecond");
        _vm.SelectedNode = _vm.Nodes[1]; // Second

        _vm.DemoteCommand.Execute(null);

        Assert.Single(_vm.Nodes);
        Assert.Equal("First", _vm.Nodes[0].Title);
        Assert.Single(_vm.Nodes[0].Children);
        Assert.Equal("Second", _vm.Nodes[0].Children[0].Title);
    }

    [Fact]
    public void Demote_FirstSibling_DoesNothing()
    {
        _vm.LoadFromText("First\nSecond");
        _vm.SelectedNode = _vm.Nodes[0];

        _vm.DemoteCommand.Execute(null);

        Assert.Equal(2, _vm.Nodes.Count);
    }

    // --- MoveUp / MoveDown ---

    [Fact]
    public void MoveUp_SwapsWithPreviousSibling()
    {
        _vm.LoadFromText("A\nB\nC");
        _vm.SelectedNode = _vm.Nodes[1]; // B

        _vm.MoveUpCommand.Execute(null);

        Assert.Equal("B", _vm.Nodes[0].Title);
        Assert.Equal("A", _vm.Nodes[1].Title);
        Assert.Equal("C", _vm.Nodes[2].Title);
    }

    [Fact]
    public void MoveUp_AtTop_DoesNothing()
    {
        _vm.LoadFromText("A\nB");
        _vm.SelectedNode = _vm.Nodes[0];

        _vm.MoveUpCommand.Execute(null);

        Assert.Equal("A", _vm.Nodes[0].Title);
        Assert.Equal("B", _vm.Nodes[1].Title);
    }

    [Fact]
    public void MoveDown_SwapsWithNextSibling()
    {
        _vm.LoadFromText("A\nB\nC");
        _vm.SelectedNode = _vm.Nodes[1]; // B

        _vm.MoveDownCommand.Execute(null);

        Assert.Equal("A", _vm.Nodes[0].Title);
        Assert.Equal("C", _vm.Nodes[1].Title);
        Assert.Equal("B", _vm.Nodes[2].Title);
    }

    [Fact]
    public void MoveDown_AtBottom_DoesNothing()
    {
        _vm.LoadFromText("A\nB");
        _vm.SelectedNode = _vm.Nodes[1];

        _vm.MoveDownCommand.Execute(null);

        Assert.Equal("A", _vm.Nodes[0].Title);
        Assert.Equal("B", _vm.Nodes[1].Title);
    }

    // --- LoadFromText / ToMarkdownText round-trip ---

    [Fact]
    public void LoadFromText_ToMarkdownText_RoundTrips()
    {
        var text = "Root\n   Child1\n   Child2\n      Grandchild";
        _vm.LoadFromText(text);

        var output = _vm.ToMarkdownText();

        Assert.Equal(text, output);
    }

    // --- GetNodes ---

    [Fact]
    public void GetNodes_ReturnsCurrentNodeList()
    {
        _vm.LoadFromText("A\nB\nC");
        var nodes = _vm.GetNodes();

        Assert.Equal(3, nodes.Count);
        Assert.Equal("A", nodes[0].Title);
    }

    // --- ParseStatus ---

    [Fact]
    public void ParseStatus_UpdatesAfterAddItem()
    {
        _vm.AddItemCommand.Execute(null);
        Assert.Contains("1 items", _vm.ParseStatus);
        Assert.Contains("1 cards", _vm.ParseStatus);
    }

    [Fact]
    public void ParseStatus_UpdatesAfterRemoveItem()
    {
        _vm.LoadFromText("A\nB");
        _vm.SelectedNode = _vm.Nodes[0];
        _vm.RemoveItemCommand.Execute(null);

        Assert.Contains("1 items", _vm.ParseStatus);
    }

    [Fact]
    public void ParseStatus_CountsLeavesCorrectly()
    {
        _vm.LoadFromText("Parent\n   Child1\n   Child2");

        Assert.Contains("3 items", _vm.ParseStatus);
        Assert.Contains("2 cards", _vm.ParseStatus);
    }
}
