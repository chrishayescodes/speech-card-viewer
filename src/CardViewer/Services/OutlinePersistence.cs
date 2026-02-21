using System.Text.Json;
using CardViewer.Models;

namespace CardViewer.Services;

public class OutlinePersistence
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SaveAsync(Outline outline, string filePath)
    {
        outline.ModifiedAt = DateTime.UtcNow;
        outline.FilePath = filePath;
        var json = JsonSerializer.Serialize(outline, Options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<Outline> LoadAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var outline = JsonSerializer.Deserialize<Outline>(json, Options)!;
        outline.FilePath = filePath;
        RelinkParents(outline.RootNodes, null);
        return outline;
    }

    private static void RelinkParents(IEnumerable<OutlineNode> nodes, OutlineNode? parent)
    {
        foreach (var node in nodes)
        {
            node.Parent = parent;
            RelinkParents(node.Children, node);
        }
    }
}
