using CardViewer.Models;
using CardViewer.ViewModels;

namespace CardViewer.Tests;

public class OutlineEditorViewModelTests
{
    // --- GetParsedNodes (Text Mode) ---

    [Fact]
    public void GetParsedNodes_TextMode_ParsesOutline()
    {
        var vm = new OutlineEditorViewModel();
        vm.OutlineText = "A\n   B\n   C";

        var nodes = vm.GetParsedNodes();

        Assert.Single(nodes);
        Assert.Equal("A", nodes[0].Title);
        Assert.Equal(2, nodes[0].Children.Count);
    }

    [Fact]
    public void GetParsedNodes_EmptyText_ReturnsEmpty()
    {
        var vm = new OutlineEditorViewModel();
        vm.OutlineText = "";

        var nodes = vm.GetParsedNodes();

        Assert.Empty(nodes);
    }

    // --- GetParsedNodes (Structured Mode) ---

    [Fact]
    public void GetParsedNodes_StructuredMode_ReturnsStructuredNodes()
    {
        var vm = new OutlineEditorViewModel();
        vm.OutlineText = "A\n   B";

        // Switch to structured mode (syncs text -> tree)
        vm.ToggleEditorModeCommand.Execute(null);

        var nodes = vm.GetParsedNodes();

        Assert.Single(nodes);
        Assert.Equal("A", nodes[0].Title);
        Assert.Single(nodes[0].Children);
    }

    // --- Toggle Editor Mode ---

    [Fact]
    public void ToggleEditorMode_TextToStructured_SyncsTextToTree()
    {
        var vm = new OutlineEditorViewModel();
        vm.OutlineText = "Root\n   Child";

        vm.ToggleEditorModeCommand.Execute(null);

        Assert.True(vm.IsStructuredMode);
        Assert.Equal("Text", vm.EditorModeLabel);

        var nodes = vm.StructuredEditor.GetNodes();
        Assert.Single(nodes);
        Assert.Equal("Root", nodes[0].Title);
    }

    [Fact]
    public void ToggleEditorMode_StructuredToText_SyncsTreeToText()
    {
        var vm = new OutlineEditorViewModel();
        vm.OutlineText = "Root\n   Child";
        vm.ToggleEditorModeCommand.Execute(null); // Now in structured mode

        // Modify in structured mode
        vm.StructuredEditor.SelectedNode = vm.StructuredEditor.Nodes[0];
        vm.StructuredEditor.AddChildCommand.Execute(null);
        vm.StructuredEditor.SelectedNode!.Title = "NewChild";

        vm.ToggleEditorModeCommand.Execute(null); // Back to text mode

        Assert.False(vm.IsStructuredMode);
        Assert.Equal("Structured", vm.EditorModeLabel);
        Assert.Contains("NewChild", vm.OutlineText);
    }

    // --- GetOutlineText ---

    [Fact]
    public void GetOutlineText_TextMode_ReturnsOutlineText()
    {
        var vm = new OutlineEditorViewModel();
        vm.OutlineText = "Hello world";

        Assert.Equal("Hello world", vm.GetOutlineText());
    }

    [Fact]
    public void GetOutlineText_StructuredMode_ReturnsMarkdownFromTree()
    {
        var vm = new OutlineEditorViewModel();
        vm.OutlineText = "A\n   B";
        vm.ToggleEditorModeCommand.Execute(null); // Structured mode

        var text = vm.GetOutlineText();
        Assert.Contains("A", text);
        Assert.Contains("B", text);
    }

    // --- ShowTitle ---

    [Fact]
    public void ShowTitle_AutoDetectsFromMarkdownHeader()
    {
        var vm = new OutlineEditorViewModel();
        vm.LoadOutlineText("# My Speech\n## Intro\n### Hook");

        Assert.Equal("My Speech", vm.ShowTitle);
    }

    [Fact]
    public void ShowTitle_AutoDetectsFromSingleRootNode()
    {
        var vm = new OutlineEditorViewModel();
        vm.LoadOutlineText("Only Root\n   Child1\n   Child2");

        Assert.Equal("Only Root", vm.ShowTitle);
    }

    [Fact]
    public void ShowTitle_PreservesExplicitTitle()
    {
        var vm = new OutlineEditorViewModel();
        vm.LoadOutlineText("Some content", "Custom Title");

        Assert.Equal("Custom Title", vm.ShowTitle);
    }

    [Fact]
    public void ShowTitle_EmptyWhenMultipleRootsNoHeader()
    {
        var vm = new OutlineEditorViewModel();
        vm.LoadOutlineText("Root1\nRoot2\nRoot3");

        Assert.Equal("", vm.ShowTitle);
    }

    // --- LoadOutlineText ---

    [Fact]
    public void LoadOutlineText_SetsOutlineText()
    {
        var vm = new OutlineEditorViewModel();
        vm.LoadOutlineText("My outline text");

        Assert.Equal("My outline text", vm.OutlineText);
    }

    [Fact]
    public void LoadOutlineText_WithTitle_SetsShowTitle()
    {
        var vm = new OutlineEditorViewModel();
        vm.LoadOutlineText("content", "My Title");

        Assert.Equal("My Title", vm.ShowTitle);
    }

    [Fact]
    public void LoadOutlineText_InStructuredMode_SyncsToTree()
    {
        var vm = new OutlineEditorViewModel();
        vm.OutlineText = "Old";
        vm.ToggleEditorModeCommand.Execute(null); // Enter structured mode

        vm.LoadOutlineText("New\n   Child");

        var nodes = vm.StructuredEditor.GetNodes();
        Assert.Single(nodes);
        Assert.Equal("New", nodes[0].Title);
    }

    // --- NodeDoubleClicked event ---

    [Fact]
    public void RaiseNodeDoubleClicked_FiresEvent()
    {
        var vm = new OutlineEditorViewModel();
        OutlineNode? received = null;
        vm.NodeDoubleClicked += n => received = n;

        var node = new OutlineNode { Title = "Test" };
        vm.RaiseNodeDoubleClicked(node);

        Assert.Same(node, received);
    }

    // --- Initial state ---

    [Fact]
    public void InitialState_TextModeActive()
    {
        var vm = new OutlineEditorViewModel();

        Assert.False(vm.IsStructuredMode);
        Assert.Equal("Structured", vm.EditorModeLabel);
        Assert.Equal("", vm.OutlineText);
        Assert.Equal("0 items, 0 cards", vm.ParseStatus);
    }
}
