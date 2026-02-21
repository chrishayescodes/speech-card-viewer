using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CardViewer.Models;

public class OutlineNode : INotifyPropertyChanged
{
    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public ObservableCollection<OutlineNode> Children { get; set; } = new();

    private OutlineNode? _parent;

    [JsonIgnore]
    public OutlineNode? Parent
    {
        get => _parent;
        set
        {
            _parent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Depth));
            OnPropertyChanged(nameof(IsBullet));
        }
    }

    [JsonIgnore]
    public int Depth
    {
        get
        {
            int d = 0;
            var current = _parent;
            while (current != null) { d++; current = current._parent; }
            return d;
        }
    }

    [JsonIgnore]
    public bool IsBullet => Depth >= 3;

    private bool _isHighlighted;

    [JsonIgnore]
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set { _isHighlighted = value; OnPropertyChanged(); }
    }

    private bool _isExpanded = true;

    [JsonIgnore]
    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public bool IsLeaf => Children.Count == 0;

    public List<string> GetBreadcrumb()
    {
        var path = new List<string>();
        var current = this;
        while (current != null)
        {
            path.Insert(0, current.Title);
            current = current.Parent;
        }
        return path;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
