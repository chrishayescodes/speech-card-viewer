using CardViewer.Models;

namespace CardViewer.Services;

public class CardGenerator
{
    public List<SpeechCard> GenerateCards(Outline outline)
    {
        var cards = new List<SpeechCard>();
        foreach (var root in outline.RootNodes)
        {
            CollectLeafCards(root, new List<string>(), cards);
        }
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].CardNumber = i + 1;
            cards[i].TotalCards = cards.Count;
        }
        return cards;
    }

    public List<SpeechCard> GenerateCards(IEnumerable<OutlineNode> rootNodes)
    {
        var outline = new Outline();
        foreach (var node in rootNodes)
            outline.RootNodes.Add(node);
        return GenerateCards(outline);
    }

    private const int MaxStructuralDepth = 3;

    private void CollectLeafCards(OutlineNode node, List<string> breadcrumb, List<SpeechCard> cards, int depth = 0)
    {
        // At max structural depth with children: this node is the card topic, children become bullets
        if (depth >= MaxStructuralDepth - 1 && !node.IsLeaf)
        {
            var card = new SpeechCard
            {
                BreadcrumbPath = new List<string>(breadcrumb),
                Topic = node.Title
            };
            CollectBullets(node, card.Bullets, 0);
            cards.Add(card);
        }
        else if (node.IsLeaf)
        {
            cards.Add(new SpeechCard
            {
                BreadcrumbPath = new List<string>(breadcrumb),
                Topic = node.Title
            });
        }
        else
        {
            breadcrumb.Add(node.Title);
            foreach (var child in node.Children)
            {
                CollectLeafCards(child, breadcrumb, cards, depth + 1);
            }
            breadcrumb.RemoveAt(breadcrumb.Count - 1);
        }
    }

    private static void CollectBullets(OutlineNode node, List<BulletItem> bullets, int indent)
    {
        foreach (var child in node.Children)
        {
            bullets.Add(new BulletItem { Text = child.Title, IndentLevel = indent });
            CollectBullets(child, bullets, indent + 1);
        }
    }
}
