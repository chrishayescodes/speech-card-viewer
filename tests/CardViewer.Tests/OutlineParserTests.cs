using CardViewer.Services;

namespace CardViewer.Tests;

public class OutlineParserTests
{
    private readonly OutlineParser _parser = new();

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyResult()
    {
        var result = _parser.Parse("");
        Assert.Empty(result.Nodes);
        Assert.Equal(0, result.TotalNodes);
        Assert.Equal(0, result.LeafCount);
    }

    [Fact]
    public void Parse_SingleItem_ReturnsOneRootNode()
    {
        var result = _parser.Parse("How to draw");
        Assert.Single(result.Nodes);
        Assert.Equal("How to draw", result.Nodes[0].Title);
        Assert.True(result.Nodes[0].IsLeaf);
        Assert.Equal(1, result.TotalNodes);
        Assert.Equal(1, result.LeafCount);
    }

    [Fact]
    public void Parse_NestedOutline_BuildsTree()
    {
        var text = """
            How to draw
               Understanding perspective
                  1 point perspective
                  2 point perspective
               Color theory
                  Warm vs cool colors
            """;

        var result = _parser.Parse(text);

        Assert.Single(result.Nodes);
        var root = result.Nodes[0];
        Assert.Equal("How to draw", root.Title);
        Assert.Equal(2, root.Children.Count);

        var perspective = root.Children[0];
        Assert.Equal("Understanding perspective", perspective.Title);
        Assert.Equal(2, perspective.Children.Count);
        Assert.Equal("1 point perspective", perspective.Children[0].Title);
        Assert.Equal("2 point perspective", perspective.Children[1].Title);

        var colorTheory = root.Children[1];
        Assert.Equal("Color theory", colorTheory.Title);
        Assert.Single(colorTheory.Children);
        Assert.Equal("Warm vs cool colors", colorTheory.Children[0].Title);
    }

    [Fact]
    public void Parse_MarkdownDashes_StripsPrefix()
    {
        var text = """
            - How to draw
               - Understanding perspective
                  - 1 point perspective
            """;

        var result = _parser.Parse(text);
        Assert.Equal("How to draw", result.Nodes[0].Title);
        Assert.Equal("Understanding perspective", result.Nodes[0].Children[0].Title);
        Assert.Equal("1 point perspective", result.Nodes[0].Children[0].Children[0].Title);
    }

    [Fact]
    public void Parse_MarkdownAsterisks_StripsPrefix()
    {
        var text = """
            * Topic one
               * Sub topic
            """;

        var result = _parser.Parse(text);
        Assert.Equal("Topic one", result.Nodes[0].Title);
        Assert.Equal("Sub topic", result.Nodes[0].Children[0].Title);
    }

    [Fact]
    public void Parse_MultipleRoots_ReturnsAll()
    {
        var text = """
            How to draw
               Perspective
            Cooking basics
               Knife skills
            """;

        var result = _parser.Parse(text);
        Assert.Equal(2, result.Nodes.Count);
        Assert.Equal("How to draw", result.Nodes[0].Title);
        Assert.Equal("Cooking basics", result.Nodes[1].Title);
    }

    [Fact]
    public void Parse_SetsParentReferences()
    {
        var text = """
            Root
               Child
                  Grandchild
            """;

        var result = _parser.Parse(text);
        var root = result.Nodes[0];
        var child = root.Children[0];
        var grandchild = child.Children[0];

        Assert.Null(root.Parent);
        Assert.Equal(root, child.Parent);
        Assert.Equal(child, grandchild.Parent);
    }

    [Fact]
    public void Parse_BlankLines_AreSkipped()
    {
        var text = """
            Topic one

            Topic two

               Subtopic
            """;

        var result = _parser.Parse(text);
        Assert.Equal(2, result.Nodes.Count);
        Assert.Equal("Topic one", result.Nodes[0].Title);
        Assert.Equal("Topic two", result.Nodes[1].Title);
        Assert.Single(result.Nodes[1].Children);
    }

    [Fact]
    public void Parse_CountsLeavesCorrectly()
    {
        var text = """
            How to draw
               Understanding perspective
                  1 point perspective
                  2 point perspective
               Color theory
                  Warm vs cool colors
            Cooking basics
               Knife skills
            """;

        var result = _parser.Parse(text);
        Assert.Equal(8, result.TotalNodes);
        Assert.Equal(4, result.LeafCount);
    }

    [Fact]
    public void Parse_DeepNesting_Works()
    {
        var text = """
            L1
               L2
                  L3
                     L4
                        L5
            """;

        var result = _parser.Parse(text);
        var node = result.Nodes[0];
        for (int i = 2; i <= 5; i++)
        {
            Assert.Single(node.Children);
            node = node.Children[0];
            Assert.Equal($"L{i}", node.Title);
        }
        Assert.True(node.IsLeaf);
    }

    [Fact]
    public void GetBreadcrumb_ReturnsFullPath()
    {
        var text = """
            How to draw
               Understanding perspective
                  1 point perspective
            """;

        var result = _parser.Parse(text);
        var leaf = result.Nodes[0].Children[0].Children[0];
        var breadcrumb = leaf.GetBreadcrumb();

        Assert.Equal(3, breadcrumb.Count);
        Assert.Equal("How to draw", breadcrumb[0]);
        Assert.Equal("Understanding perspective", breadcrumb[1]);
        Assert.Equal("1 point perspective", breadcrumb[2]);
    }

    // --- Markdown header tests ---

    [Fact]
    public void Parse_MarkdownHeaders_BuildsHierarchy()
    {
        var text = "# How to draw\n## Understanding perspective\n### 1 point perspective\n### 2 point perspective";

        var result = _parser.Parse(text);

        Assert.Single(result.Nodes);
        var root = result.Nodes[0];
        Assert.Equal("How to draw", root.Title);
        Assert.Single(root.Children);

        var perspective = root.Children[0];
        Assert.Equal("Understanding perspective", perspective.Title);
        Assert.Equal(2, perspective.Children.Count);
        Assert.Equal("1 point perspective", perspective.Children[0].Title);
        Assert.Equal("2 point perspective", perspective.Children[1].Title);
    }

    [Fact]
    public void Parse_MarkdownHeaders_MultipleTopLevel()
    {
        var text = "# Drawing\n## Perspective\n# Cooking\n## Knife skills";

        var result = _parser.Parse(text);

        Assert.Equal(2, result.Nodes.Count);
        Assert.Equal("Drawing", result.Nodes[0].Title);
        Assert.Single(result.Nodes[0].Children);
        Assert.Equal("Cooking", result.Nodes[1].Title);
        Assert.Single(result.Nodes[1].Children);
    }

    [Fact]
    public void Parse_MarkdownHeaders_WithListItemsBelow()
    {
        var text = "# Speech Topic\n## Main Point\n- Detail A\n- Detail B";

        var result = _parser.Parse(text);

        Assert.Single(result.Nodes);
        var root = result.Nodes[0];
        Assert.Equal("Speech Topic", root.Title);

        var mainPoint = root.Children[0];
        Assert.Equal("Main Point", mainPoint.Title);
        Assert.Equal(2, mainPoint.Children.Count);
        Assert.Equal("Detail A", mainPoint.Children[0].Title);
        Assert.Equal("Detail B", mainPoint.Children[1].Title);
    }

    [Fact]
    public void Parse_MarkdownHeaders_IndentedListsNestUnderHeaders()
    {
        var text = "# Topic\n## Subtopic\n- Point one\n   - Sub point";

        var result = _parser.Parse(text);

        var subtopic = result.Nodes[0].Children[0];
        Assert.Equal("Subtopic", subtopic.Title);

        var pointOne = subtopic.Children[0];
        Assert.Equal("Point one", pointOne.Title);
        Assert.Single(pointOne.Children);
        Assert.Equal("Sub point", pointOne.Children[0].Title);
    }

    [Fact]
    public void Parse_MarkdownHeaders_LeafCountCorrect()
    {
        // # Title (not leaf)
        //   ## Section A (not leaf)
        //     ### Detail 1 (leaf)
        //     ### Detail 2 (leaf)
        //   ## Section B (leaf — no children)
        var text = "# Title\n## Section A\n### Detail 1\n### Detail 2\n## Section B";

        var result = _parser.Parse(text);
        Assert.Equal(5, result.TotalNodes);
        Assert.Equal(3, result.LeafCount); // Detail 1, Detail 2, Section B
    }

    [Fact]
    public void Parse_MarkdownHeaders_SkipsHeaderOnly()
    {
        // A line that is just "###" with no title text should be skipped
        var text = "# Real Title\n###\n## Subtitle";

        var result = _parser.Parse(text);
        Assert.Single(result.Nodes);
        Assert.Equal("Real Title", result.Nodes[0].Title);
        Assert.Single(result.Nodes[0].Children);
        Assert.Equal("Subtitle", result.Nodes[0].Children[0].Title);
    }
}
