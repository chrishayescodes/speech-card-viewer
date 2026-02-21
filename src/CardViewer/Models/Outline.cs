using System.Collections.ObjectModel;

namespace CardViewer.Models;

public class Outline
{
    public string Name { get; set; } = "Untitled Outline";
    public string? FilePath { get; set; }
    public ObservableCollection<OutlineNode> RootNodes { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
