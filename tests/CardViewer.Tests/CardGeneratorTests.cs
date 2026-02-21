using CardViewer.Models;
using CardViewer.Services;

namespace CardViewer.Tests;

public class CardGeneratorTests
{
    private readonly OutlineParser _parser = new();
    private readonly CardGenerator _generator = new();

    [Fact]
    public void GenerateCards_EmptyOutline_ReturnsNoCards()
    {
        var outline = new Outline();
        var cards = _generator.GenerateCards(outline);
        Assert.Empty(cards);
    }

    [Fact]
    public void GenerateCards_SingleLeafRoot_ReturnsOneCard()
    {
        var outline = _parser.ParseToOutline("Topic");
        var cards = _generator.GenerateCards(outline);

        Assert.Single(cards);
        Assert.Equal("Topic", cards[0].Topic);
        Assert.Empty(cards[0].BreadcrumbPath);
        Assert.Equal(1, cards[0].CardNumber);
        Assert.Equal(1, cards[0].TotalCards);
    }

    [Fact]
    public void GenerateCards_OnlyLeafNodesGetCards()
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

        var outline = _parser.ParseToOutline(text);
        var cards = _generator.GenerateCards(outline);

        Assert.Equal(4, cards.Count);
        Assert.Equal("1 point perspective", cards[0].Topic);
        Assert.Equal("2 point perspective", cards[1].Topic);
        Assert.Equal("Warm vs cool colors", cards[2].Topic);
        Assert.Equal("Knife skills", cards[3].Topic);
    }

    [Fact]
    public void GenerateCards_BreadcrumbPathIsCorrect()
    {
        var text = """
            How to draw
               Understanding perspective
                  1 point perspective
            """;

        var outline = _parser.ParseToOutline(text);
        var cards = _generator.GenerateCards(outline);

        Assert.Single(cards);
        var card = cards[0];
        Assert.Equal(new List<string> { "How to draw", "Understanding perspective" }, card.BreadcrumbPath);
        Assert.Equal("1 point perspective", card.Topic);
        Assert.Equal("How to draw > Understanding perspective > 1 point perspective", card.FullPath);
    }

    [Fact]
    public void GenerateCards_CardNumbersAreSequential()
    {
        var text = """
            A
               B
               C
               D
            """;

        var outline = _parser.ParseToOutline(text);
        var cards = _generator.GenerateCards(outline);

        Assert.Equal(3, cards.Count);
        for (int i = 0; i < cards.Count; i++)
        {
            Assert.Equal(i + 1, cards[i].CardNumber);
            Assert.Equal(3, cards[i].TotalCards);
        }
    }

    [Fact]
    public void GenerateCards_FullPath_FormatsCorrectly()
    {
        var text = """
            Root
               Mid
                  Leaf
            """;

        var outline = _parser.ParseToOutline(text);
        var cards = _generator.GenerateCards(outline);

        Assert.Equal("Root > Mid > Leaf", cards[0].FullPath);
    }

    [Fact]
    public void GenerateCards_TopLevelLeaf_HasEmptyBreadcrumb()
    {
        var text = """
            Standalone topic
            Another standalone
            """;

        var outline = _parser.ParseToOutline(text);
        var cards = _generator.GenerateCards(outline);

        Assert.Equal(2, cards.Count);
        Assert.Empty(cards[0].BreadcrumbPath);
        Assert.Equal("Standalone topic", cards[0].Topic);
        Assert.Equal("Standalone topic", cards[0].FullPath);
    }

    [Fact]
    public void GenerateCards_Depth3Items_BecomeBullets()
    {
        // Depth 0: Speech, Depth 1: Intro, Depth 2: Hook, Depth 3+: bullets
        var text = """
            Speech
               Intro
                  Hook
                     Start with a question
                     Surprising fact
            """;

        var outline = _parser.ParseToOutline(text);
        var cards = _generator.GenerateCards(outline);

        Assert.Single(cards);
        Assert.Equal("Hook", cards[0].Topic);
        Assert.Equal(2, cards[0].Bullets.Count);
        Assert.Equal("Start with a question", cards[0].Bullets[0].Text);
        Assert.Equal("Surprising fact", cards[0].Bullets[1].Text);
    }

    [Fact]
    public void GenerateCards_NestedBullets_HaveIndentLevels()
    {
        var text = """
            A
               B
                  C
                     D1
                        E1
                     D2
            """;

        var outline = _parser.ParseToOutline(text);
        var cards = _generator.GenerateCards(outline);

        Assert.Single(cards);
        Assert.Equal("C", cards[0].Topic);
        Assert.Equal(3, cards[0].Bullets.Count);
        Assert.Equal(0, cards[0].Bullets[0].IndentLevel); // D1
        Assert.Equal(1, cards[0].Bullets[1].IndentLevel); // E1 (child of D1)
        Assert.Equal(0, cards[0].Bullets[2].IndentLevel); // D2
    }

    [Fact]
    public void GenerateCards_LeafAtDepth2_NoBullets()
    {
        var text = """
            A
               B
                  C
            """;

        var outline = _parser.ParseToOutline(text);
        var cards = _generator.GenerateCards(outline);

        Assert.Single(cards);
        Assert.Equal("C", cards[0].Topic);
        Assert.Empty(cards[0].Bullets);
    }
}
