using System.Diagnostics;
using CardViewer.Models;
using CardViewer.Services;
using CardViewer.ViewModels;

namespace CardViewer.Tests;

public class StressTests
{
    private readonly OutlineParser _parser = new();
    private readonly CardGenerator _generator = new();

    // --- Helpers ---

    /// <summary>
    /// Generates a large outline text with the given number of chapters,
    /// sections per chapter, and items per section.
    /// </summary>
    private static string GenerateLargeOutline(int chapters, int sectionsPerChapter, int itemsPerSection)
    {
        var lines = new List<string>();
        for (int c = 0; c < chapters; c++)
        {
            lines.Add($"Chapter {c + 1}");
            for (int s = 0; s < sectionsPerChapter; s++)
            {
                lines.Add($"   Section {c + 1}.{s + 1}");
                for (int i = 0; i < itemsPerSection; i++)
                {
                    lines.Add($"      Item {c + 1}.{s + 1}.{i + 1}");
                }
            }
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Generates a flat outline with many root-level items.
    /// </summary>
    private static string GenerateFlatOutline(int count)
    {
        var lines = new List<string>();
        for (int i = 0; i < count; i++)
            lines.Add($"Item {i + 1}");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Generates a deeply nested outline.
    /// </summary>
    private static string GenerateDeepOutline(int depth)
    {
        var lines = new List<string>();
        for (int i = 0; i < depth; i++)
            lines.Add(new string(' ', i * 3) + $"Level {i + 1}");
        return string.Join("\n", lines);
    }

    // --- Parsing Stress Tests ---

    [Fact]
    public void Parse_LargeOutline_1000Nodes_CompletesQuickly()
    {
        // 20 chapters x 10 sections x 5 items = 1200 nodes total (including parents)
        var text = GenerateLargeOutline(20, 10, 5);

        var sw = Stopwatch.StartNew();
        var result = _parser.Parse(text);
        sw.Stop();

        Assert.True(result.TotalNodes >= 1000, $"Expected >= 1000 nodes, got {result.TotalNodes}");
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Parsing took {sw.ElapsedMilliseconds}ms, expected < 2000ms");
    }

    [Fact]
    public void Parse_VeryLargeOutline_5000Lines_CompletesQuickly()
    {
        var text = GenerateFlatOutline(5000);

        var sw = Stopwatch.StartNew();
        var result = _parser.Parse(text);
        sw.Stop();

        Assert.Equal(5000, result.TotalNodes);
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Parsing took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Parse_DeeplyNested_50Levels_CompletesQuickly()
    {
        var text = GenerateDeepOutline(50);

        var sw = Stopwatch.StartNew();
        var result = _parser.Parse(text);
        sw.Stop();

        Assert.Equal(50, result.TotalNodes);
        Assert.Equal(1, result.LeafCount);
        Assert.True(sw.ElapsedMilliseconds < 1000, $"Parsing took {sw.ElapsedMilliseconds}ms");
    }

    // --- Card Generation Stress Tests ---

    [Fact]
    public void GenerateCards_500Cards_CompletesQuickly()
    {
        // 50 chapters x 10 items = 500 leaf nodes = 500 cards
        var text = GenerateLargeOutline(50, 1, 10);
        var result = _parser.Parse(text);

        var sw = Stopwatch.StartNew();
        var cards = _generator.GenerateCards(result.Nodes);
        sw.Stop();

        Assert.True(cards.Count >= 500, $"Expected >= 500 cards, got {cards.Count}");
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Card generation took {sw.ElapsedMilliseconds}ms");

        // Verify numbering
        for (int i = 0; i < cards.Count; i++)
        {
            Assert.Equal(i + 1, cards[i].CardNumber);
            Assert.Equal(cards.Count, cards[i].TotalCards);
        }
    }

    // --- Navigation Stress Tests ---

    [Fact]
    public void RapidNavigation_CycleThroughAllCards()
    {
        var text = GenerateLargeOutline(20, 5, 5);
        var result = _parser.Parse(text);
        var cards = _generator.GenerateCards(result.Nodes);
        var vm = new CardViewerViewModel(cards, result.Nodes);

        var sw = Stopwatch.StartNew();

        // Navigate forward through all cards
        while (vm.CanGoNext)
            vm.NextCardCommand.Execute(null);

        Assert.Equal(cards.Count - 1, vm.CurrentIndex);

        // Navigate backward through all cards
        while (vm.CanGoPrevious)
            vm.PreviousCardCommand.Execute(null);

        sw.Stop();

        Assert.Equal(0, vm.CurrentIndex);
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Full navigation cycle took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void RapidChapterNavigation_SkipsThroughAllChapters()
    {
        var text = GenerateLargeOutline(20, 5, 5);
        var result = _parser.Parse(text);
        var cards = _generator.GenerateCards(result.Nodes);
        var vm = new CardViewerViewModel(cards, result.Nodes);

        var sw = Stopwatch.StartNew();

        // Skip through all chapters forward
        int chapterJumps = 0;
        while (vm.CanGoNextChapter)
        {
            vm.NextChapterCommand.Execute(null);
            chapterJumps++;
            if (chapterJumps > 1000) break; // Safety valve
        }

        // Skip back through all chapters
        while (vm.CanGoPreviousChapter)
        {
            vm.PreviousChapterCommand.Execute(null);
            chapterJumps++;
            if (chapterJumps > 2000) break;
        }

        sw.Stop();

        Assert.True(chapterJumps > 0, "Should have made at least one chapter jump");
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Chapter navigation took {sw.ElapsedMilliseconds}ms");
    }

    // --- Structured Editor Stress Tests ---

    [Fact]
    public void StructuredEditor_BulkAdd200Nodes_ThenRemoveAll()
    {
        var vm = new StructuredEditorViewModel();

        var sw = Stopwatch.StartNew();

        // Add 200 nodes
        for (int i = 0; i < 200; i++)
        {
            vm.AddItemCommand.Execute(null);
            vm.SelectedNode!.Title = $"Node {i + 1}";
        }

        Assert.Equal(200, vm.Nodes.Count);

        // Remove all nodes
        while (vm.Nodes.Count > 0)
        {
            vm.SelectedNode = vm.Nodes[0];
            vm.RemoveItemCommand.Execute(null);
        }

        sw.Stop();

        Assert.Empty(vm.Nodes);
        Assert.True(sw.ElapsedMilliseconds < 3000, $"Bulk add/remove took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void StructuredEditor_BulkAddChildrenThenPromote()
    {
        var vm = new StructuredEditorViewModel();
        vm.AddItemCommand.Execute(null);
        vm.SelectedNode!.Title = "Root";

        // Add 50 children
        for (int i = 0; i < 50; i++)
        {
            vm.SelectedNode = vm.Nodes[0];
            vm.AddChildCommand.Execute(null);
            vm.SelectedNode!.Title = $"Child {i + 1}";
        }

        Assert.Equal(50, vm.Nodes[0].Children.Count);

        var sw = Stopwatch.StartNew();

        // Promote all children back to root level
        while (vm.Nodes[0].Children.Count > 0)
        {
            vm.SelectedNode = vm.Nodes[0].Children[0];
            vm.PromoteCommand.Execute(null);
        }

        sw.Stop();

        Assert.True(vm.Nodes.Count > 1);
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Bulk promote took {sw.ElapsedMilliseconds}ms");
    }

    // --- Mode Switching Stress Tests ---

    [Fact]
    public void ModeSwitching_UnderLoad_500Lines()
    {
        var vm = new OutlineEditorViewModel();
        var text = GenerateLargeOutline(10, 10, 5);
        vm.OutlineText = text;

        var sw = Stopwatch.StartNew();

        // Toggle mode 10 times
        for (int i = 0; i < 10; i++)
        {
            vm.ToggleEditorModeCommand.Execute(null);
        }

        sw.Stop();

        // Should be back in text mode after even number of toggles
        Assert.False(vm.IsStructuredMode);
        Assert.True(sw.ElapsedMilliseconds < 3000, $"Mode switching took {sw.ElapsedMilliseconds}ms");
    }

    // --- Sidebar Stress Tests ---

    [Fact]
    public void ExpandCollapseAll_1000Nodes()
    {
        var text = GenerateLargeOutline(20, 10, 5);
        var result = _parser.Parse(text);
        var cards = _generator.GenerateCards(result.Nodes);
        var vm = new CardViewerViewModel(cards, result.Nodes);

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 20; i++)
        {
            vm.CollapseAllCommand.Execute(null);
            vm.ExpandAllCommand.Execute(null);
        }

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 2000, $"Expand/collapse cycles took {sw.ElapsedMilliseconds}ms");
    }

    // --- Persistence Stress Tests ---

    [Fact]
    public async Task Persistence_RoundTrip_LargeOutline()
    {
        var persistence = new OutlinePersistence();
        var text = GenerateLargeOutline(20, 10, 5);
        var outline = _parser.ParseToOutline(text, "Stress Test");

        var tempFile = Path.Combine(Path.GetTempPath(), $"stress_{Guid.NewGuid()}.cdv");
        try
        {
            var sw = Stopwatch.StartNew();

            await persistence.SaveAsync(outline, tempFile);
            var loaded = await persistence.LoadAsync(tempFile);

            sw.Stop();

            Assert.Equal(outline.RootNodes.Count, loaded.RootNodes.Count);
            Assert.Equal("Stress Test", loaded.Name);
            Assert.True(sw.ElapsedMilliseconds < 3000, $"Save/load took {sw.ElapsedMilliseconds}ms");

            // Verify parent links restored
            foreach (var node in loaded.RootNodes)
            {
                Assert.Null(node.Parent);
                foreach (var child in node.Children)
                    Assert.Same(node, child.Parent);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    // --- Concurrent Parsing Stress Tests ---

    [Fact]
    public async Task ConcurrentParsing_MultipleThreads()
    {
        var texts = Enumerable.Range(0, 10)
            .Select(i => GenerateLargeOutline(5 + i, 5, 5))
            .ToArray();

        var sw = Stopwatch.StartNew();

        var tasks = texts.Select(text => Task.Run(() =>
        {
            var parser = new OutlineParser();
            var result = parser.Parse(text);
            Assert.True(result.TotalNodes > 0);
            return result;
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        sw.Stop();

        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.True(r.TotalNodes > 0));
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Concurrent parsing took {sw.ElapsedMilliseconds}ms");
    }

    // --- Memory Stability ---

    [Fact]
    public void CreateAndDiscard_ManyCardViewerViewModels()
    {
        var text = GenerateLargeOutline(5, 5, 5);
        var result = _parser.Parse(text);
        var cards = _generator.GenerateCards(result.Nodes);

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 100; i++)
        {
            var vm = new CardViewerViewModel(cards, result.Nodes);
            // Exercise the VM
            while (vm.CanGoNext)
                vm.NextCardCommand.Execute(null);
        }

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 5000, $"Creating/discarding 100 VMs took {sw.ElapsedMilliseconds}ms");
    }

    // --- LoadFromText Large Input ---

    [Fact]
    public void StructuredEditor_LoadFromText_LargeInput()
    {
        var vm = new StructuredEditorViewModel();
        var text = GenerateLargeOutline(20, 10, 5);

        var sw = Stopwatch.StartNew();
        vm.LoadFromText(text);
        sw.Stop();

        Assert.True(vm.Nodes.Count > 0);
        Assert.True(sw.ElapsedMilliseconds < 2000, $"LoadFromText took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void StructuredEditor_ToMarkdownText_LargeTree()
    {
        var vm = new StructuredEditorViewModel();
        var text = GenerateLargeOutline(20, 10, 5);
        vm.LoadFromText(text);

        var sw = Stopwatch.StartNew();
        var output = vm.ToMarkdownText();
        sw.Stop();

        Assert.Equal(text, output);
        Assert.True(sw.ElapsedMilliseconds < 1000, $"ToMarkdownText took {sw.ElapsedMilliseconds}ms");
    }

    // --- NavigateToNode Stress ---

    [Fact]
    public void NavigateToNode_AllNodes_InLargeOutline()
    {
        var text = GenerateLargeOutline(10, 5, 5);
        var result = _parser.Parse(text);
        var cards = _generator.GenerateCards(result.Nodes);
        var vm = new CardViewerViewModel(cards, result.Nodes);

        // Flatten all nodes
        var allNodes = new List<OutlineNode>();
        FlattenNodes(result.Nodes, allNodes);

        var sw = Stopwatch.StartNew();

        foreach (var node in allNodes)
        {
            vm.NavigateToNodeCommand.Execute(node);
        }

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 5000,
            $"Navigating to {allNodes.Count} nodes took {sw.ElapsedMilliseconds}ms");
    }

    private static void FlattenNodes(IEnumerable<OutlineNode> nodes, List<OutlineNode> result)
    {
        foreach (var node in nodes)
        {
            result.Add(node);
            FlattenNodes(node.Children, result);
        }
    }
}
