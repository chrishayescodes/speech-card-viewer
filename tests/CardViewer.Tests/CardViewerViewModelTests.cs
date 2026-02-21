using CardViewer.Models;
using CardViewer.Services;
using CardViewer.ViewModels;

namespace CardViewer.Tests;

public class CardViewerViewModelTests
{
    private readonly OutlineParser _parser = new();
    private readonly CardGenerator _generator = new();

    private (List<SpeechCard> cards, List<OutlineNode> nodes) BuildCards(string text)
    {
        var result = _parser.Parse(text);
        var cards = _generator.GenerateCards(result.Nodes);
        return (cards, result.Nodes);
    }

    // --- Construction ---

    [Fact]
    public void Constructor_DisplaysFirstCard()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C");
        var vm = new CardViewerViewModel(cards, nodes);

        Assert.Equal(0, vm.CurrentIndex);
        Assert.Equal("B", vm.CurrentCard?.Topic);
    }

    [Fact]
    public void Constructor_SetsNavigationState()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C");
        var vm = new CardViewerViewModel(cards, nodes);

        Assert.True(vm.CanGoNext);
        Assert.False(vm.CanGoPrevious);
        Assert.Equal("Card 1 of 2", vm.CardPosition);
    }

    [Fact]
    public void Constructor_LoadsOutlineNodes()
    {
        var (cards, nodes) = BuildCards("A\n   B");
        var vm = new CardViewerViewModel(cards, nodes);

        Assert.NotEmpty(vm.OutlineNodes);
    }

    [Fact]
    public void Constructor_SidebarOpenByDefault()
    {
        var (cards, nodes) = BuildCards("A\n   B");
        var vm = new CardViewerViewModel(cards, nodes);

        Assert.True(vm.IsSidebarOpen);
    }

    [Fact]
    public void Constructor_RespectsStartIndex()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C\n   D");
        var vm = new CardViewerViewModel(cards, nodes, startIndex: 2);

        Assert.Equal(2, vm.CurrentIndex);
        Assert.Equal("D", vm.CurrentCard?.Topic);
    }

    [Fact]
    public void Constructor_ClampsOutOfRangeStartIndex()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C");
        var vm = new CardViewerViewModel(cards, nodes, startIndex: 100);

        Assert.Equal(1, vm.CurrentIndex); // Clamped to last card
    }

    [Fact]
    public void Constructor_ShowTitlePassedThrough()
    {
        var (cards, nodes) = BuildCards("A\n   B");
        var vm = new CardViewerViewModel(cards, nodes, showTitle: "My Speech");

        Assert.Equal("My Speech", vm.ShowTitle);
    }

    // --- NextCard / PreviousCard ---

    [Fact]
    public void NextCard_AdvancesIndex()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C\n   D");
        var vm = new CardViewerViewModel(cards, nodes);

        vm.NextCardCommand.Execute(null);

        Assert.Equal(1, vm.CurrentIndex);
        Assert.Equal("C", vm.CurrentCard?.Topic);
        Assert.Equal("Card 2 of 3", vm.CardPosition);
    }

    [Fact]
    public void PreviousCard_RetreatsIndex()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C\n   D");
        var vm = new CardViewerViewModel(cards, nodes, startIndex: 2);

        vm.PreviousCardCommand.Execute(null);

        Assert.Equal(1, vm.CurrentIndex);
    }

    [Fact]
    public void NextCard_AtLastCard_DoesNothing()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C");
        var vm = new CardViewerViewModel(cards, nodes, startIndex: 1);

        vm.NextCardCommand.Execute(null);

        Assert.Equal(1, vm.CurrentIndex);
        Assert.False(vm.CanGoNext);
    }

    [Fact]
    public void PreviousCard_AtFirstCard_DoesNothing()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C");
        var vm = new CardViewerViewModel(cards, nodes);

        vm.PreviousCardCommand.Execute(null);

        Assert.Equal(0, vm.CurrentIndex);
        Assert.False(vm.CanGoPrevious);
    }

    [Fact]
    public void Navigation_UpdatesCanGoFlags()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C\n   D");
        var vm = new CardViewerViewModel(cards, nodes);

        Assert.True(vm.CanGoNext);
        Assert.False(vm.CanGoPrevious);

        vm.NextCardCommand.Execute(null);
        Assert.True(vm.CanGoNext);
        Assert.True(vm.CanGoPrevious);

        vm.NextCardCommand.Execute(null);
        Assert.False(vm.CanGoNext);
        Assert.True(vm.CanGoPrevious);
    }

    // --- Chapter Navigation ---

    [Fact]
    public void NextChapter_JumpsToNextChapter()
    {
        var text = "Speech\n   Intro\n      Hook\n      Story\n   Body\n      Point1\n      Point2";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes);

        // Start at first card (Hook under Intro)
        Assert.Equal("Hook", vm.CurrentCard?.Topic);

        vm.NextChapterCommand.Execute(null);

        // Should jump to first card of Body chapter
        Assert.Equal("Point1", vm.CurrentCard?.Topic);
    }

    [Fact]
    public void PreviousChapter_GoesToStartOfCurrentChapterFirst()
    {
        var text = "Speech\n   Intro\n      Hook\n      Story\n   Body\n      Point1\n      Point2";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes, startIndex: 1);

        // At Story (second card of Intro chapter)
        Assert.Equal("Story", vm.CurrentCard?.Topic);

        vm.PreviousChapterCommand.Execute(null);

        // Goes to start of current chapter first
        Assert.Equal("Hook", vm.CurrentCard?.Topic);
    }

    [Fact]
    public void PreviousChapter_AtStartOfChapter_GoesToPreviousChapter()
    {
        var text = "Speech\n   Intro\n      Hook\n      Story\n   Body\n      Point1";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes, startIndex: 2);

        // At Point1 (start of Body chapter)
        Assert.Equal("Point1", vm.CurrentCard?.Topic);

        vm.PreviousChapterCommand.Execute(null);

        // Goes to start of previous chapter (Intro -> Hook)
        Assert.Equal("Hook", vm.CurrentCard?.Topic);
    }

    [Fact]
    public void ChapterNavigation_CanGoFlags()
    {
        var text = "Speech\n   Intro\n      Hook\n   Body\n      Point1";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes);

        Assert.True(vm.CanGoNextChapter);
        Assert.False(vm.CanGoPreviousChapter);

        vm.NextChapterCommand.Execute(null);
        Assert.False(vm.CanGoNextChapter);
        Assert.True(vm.CanGoPreviousChapter);
    }

    // --- NavigateToNode ---

    [Fact]
    public void NavigateToNode_ExactMatch_NavigatesToCard()
    {
        var text = "A\n   B\n   C\n   D";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes);

        var nodeC = nodes[0].Children[1]; // C
        vm.NavigateToNodeCommand.Execute(nodeC);

        Assert.Equal("C", vm.CurrentCard?.Topic);
    }

    [Fact]
    public void NavigateToNode_ParentNode_GoesToFirstCardInSection()
    {
        var text = "Root\n   Parent\n      Child1\n      Child2";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes, startIndex: 1);

        var parentNode = nodes[0].Children[0]; // Parent
        vm.NavigateToNodeCommand.Execute(parentNode);

        Assert.Equal("Child1", vm.CurrentCard?.Topic);
    }

    // --- ExpandAll / CollapseAll ---

    [Fact]
    public void ExpandAll_SetsAllNodesExpanded()
    {
        var text = "A\n   B\n      C";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes);

        vm.CollapseAllCommand.Execute(null);
        vm.ExpandAllCommand.Execute(null);

        Assert.True(nodes[0].IsExpanded);
        Assert.True(nodes[0].Children[0].IsExpanded);
    }

    [Fact]
    public void CollapseAll_SetsAllNodesCollapsed()
    {
        var text = "A\n   B\n      C";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes);

        vm.CollapseAllCommand.Execute(null);

        Assert.False(nodes[0].IsExpanded);
        Assert.False(nodes[0].Children[0].IsExpanded);
    }

    // --- ToggleSidebar ---

    [Fact]
    public void ToggleSidebar_TogglesIsSidebarOpen()
    {
        var (cards, nodes) = BuildCards("A\n   B");
        var vm = new CardViewerViewModel(cards, nodes);

        Assert.True(vm.IsSidebarOpen);
        vm.ToggleSidebarCommand.Execute(null);
        Assert.False(vm.IsSidebarOpen);
        vm.ToggleSidebarCommand.Execute(null);
        Assert.True(vm.IsSidebarOpen);
    }

    // --- Highlight ---

    [Fact]
    public void HighlightCurrentNode_HighlightsCorrectNode()
    {
        var text = "A\n   B\n   C";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes);

        // B should be highlighted (first card)
        var nodeB = nodes[0].Children[0];
        var nodeC = nodes[0].Children[1];
        Assert.True(nodeB.IsHighlighted);
        Assert.False(nodeC.IsHighlighted);
    }

    [Fact]
    public void HighlightCurrentNode_ClearsPreviousHighlight()
    {
        var text = "A\n   B\n   C";
        var (cards, nodes) = BuildCards(text);
        var vm = new CardViewerViewModel(cards, nodes);

        vm.NextCardCommand.Execute(null);

        var nodeB = nodes[0].Children[0];
        var nodeC = nodes[0].Children[1];
        Assert.False(nodeB.IsHighlighted);
        Assert.True(nodeC.IsHighlighted);
    }

    [Fact]
    public void HighlightCurrentNode_ExpandsAncestors()
    {
        var text = "Root\n   Parent\n      Child";
        var (cards, nodes) = BuildCards(text);

        // Collapse all first
        nodes[0].IsExpanded = false;
        nodes[0].Children[0].IsExpanded = false;

        var vm = new CardViewerViewModel(cards, nodes);

        // After construction, ancestors of highlighted node should be expanded
        Assert.True(nodes[0].IsExpanded);
        Assert.True(nodes[0].Children[0].IsExpanded);
    }

    // --- Single card edge case ---

    [Fact]
    public void SingleCard_NoPreviousOrNext()
    {
        var (cards, nodes) = BuildCards("OnlyItem");
        var vm = new CardViewerViewModel(cards, nodes);

        Assert.False(vm.CanGoNext);
        Assert.False(vm.CanGoPrevious);
        Assert.Equal("Card 1 of 1", vm.CardPosition);
    }

    // --- TotalCards ---

    [Fact]
    public void TotalCards_ReturnsCorrectCount()
    {
        var (cards, nodes) = BuildCards("A\n   B\n   C\n   D");
        var vm = new CardViewerViewModel(cards, nodes);

        Assert.Equal(3, vm.TotalCards);
    }
}
