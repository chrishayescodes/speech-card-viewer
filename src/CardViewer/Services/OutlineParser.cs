using System.Collections.ObjectModel;
using CardViewer.Models;

namespace CardViewer.Services;

public class ParseResult
{
    public List<OutlineNode> Nodes { get; }
    public int TotalNodes { get; }
    public int LeafCount { get; }

    public ParseResult(List<OutlineNode> nodes)
    {
        Nodes = nodes;
        TotalNodes = CountAll(nodes);
        LeafCount = CountLeaves(nodes);
    }

    private static int CountAll(IEnumerable<OutlineNode> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            count++;
            count += CountAll(node.Children);
        }
        return count;
    }

    private static int CountLeaves(IEnumerable<OutlineNode> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (node.IsLeaf)
                count++;
            else
                count += CountLeaves(node.Children);
        }
        return count;
    }
}

public class OutlineParser
{
    // Sentinel value so header depths and indent depths don't collide.
    // Headers use negative depths (-1 for #, -2 for ##, etc.) so they
    // always sit above indented list items in the hierarchy.
    private const int HeaderDepthBase = -1000;

    public ParseResult Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ParseResult(new List<OutlineNode>());

        var lines = text.Split('\n');
        var rootNodes = new List<OutlineNode>();
        var stack = new Stack<(int Depth, OutlineNode Node)>();

        // Track whether we've seen any markdown headers — if so,
        // indented list items nest under the most recent header.
        bool hasHeaders = false;
        int lastHeaderDepth = 0;

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var trimmed = rawLine.TrimStart();

            int depth;
            string title;

            if (trimmed.StartsWith('#'))
            {
                // Markdown header: # = depth 0, ## = depth 1, ### = depth 2, etc.
                int hashCount = 0;
                while (hashCount < trimmed.Length && trimmed[hashCount] == '#')
                    hashCount++;

                title = trimmed[hashCount..].Trim();
                if (string.IsNullOrEmpty(title))
                    continue;

                depth = HeaderDepthBase + hashCount; // -999 for #, -998 for ##, etc.
                hasHeaders = true;
                lastHeaderDepth = depth;
            }
            else
            {
                // Indented list item or plain text
                int indent = rawLine.Length - trimmed.Length;
                title = trimmed.TrimStart('-', '*').Trim();

                if (string.IsNullOrEmpty(title))
                    continue;

                if (hasHeaders)
                {
                    // List items under headers get depth relative to last header
                    // so they nest as children of the header above them.
                    depth = lastHeaderDepth + 1 + indent;
                }
                else
                {
                    // Pure indentation mode (no headers in document)
                    depth = indent;
                }
            }

            var node = new OutlineNode { Title = title };

            // Pop stack until we find a parent with less depth
            while (stack.Count > 0 && stack.Peek().Depth >= depth)
                stack.Pop();

            if (stack.Count > 0)
            {
                var parent = stack.Peek().Node;
                node.Parent = parent;
                parent.Children.Add(node);
            }
            else
            {
                rootNodes.Add(node);
            }

            stack.Push((depth, node));
        }

        return new ParseResult(rootNodes);
    }

    public static string ExtractTitle(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("# ") && !trimmed.StartsWith("## "))
                return trimmed[2..].Trim();
        }
        return "";
    }

    public Outline ParseToOutline(string text, string name = "Untitled Outline")
    {
        var result = Parse(text);
        return new Outline
        {
            Name = name,
            RootNodes = new ObservableCollection<OutlineNode>(result.Nodes)
        };
    }
}
