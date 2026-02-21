using System.Collections.ObjectModel;
using System.Timers;
using CardViewer.Models;
using CardViewer.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CardViewer.ViewModels;

public partial class OutlineEditorViewModel : ViewModelBase
{
    private readonly OutlineParser _parser = new();
    private readonly System.Timers.Timer _debounceTimer;

    [ObservableProperty]
    private string _outlineText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<OutlineNode> _previewNodes = new();

    [ObservableProperty]
    private string _parseStatus = "0 items, 0 cards";

    [ObservableProperty]
    private bool _isStructuredMode;

    [ObservableProperty]
    private string _editorModeLabel = "Structured";

    public event Action<OutlineNode>? NodeDoubleClicked;

    public StructuredEditorViewModel StructuredEditor { get; } = new();

    public void RaiseNodeDoubleClicked(OutlineNode node) => NodeDoubleClicked?.Invoke(node);

    public OutlineEditorViewModel()
    {
        _debounceTimer = new System.Timers.Timer(150);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += OnDebounceElapsed;
    }

    [RelayCommand]
    private void ToggleEditorMode()
    {
        if (IsStructuredMode)
        {
            // Switching to text mode — sync tree → text
            OutlineText = StructuredEditor.ToMarkdownText();
            IsStructuredMode = false;
            EditorModeLabel = "Structured";
        }
        else
        {
            // Switching to structured mode — sync text → tree
            StructuredEditor.LoadFromText(OutlineText);
            IsStructuredMode = true;
            EditorModeLabel = "Text";
        }
    }

    partial void OnOutlineTextChanged(string value)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        var text = OutlineText;
        var result = _parser.Parse(text);
        PreviewNodes = new ObservableCollection<OutlineNode>(result.Nodes);
        ParseStatus = $"{result.TotalNodes} items, {result.LeafCount} cards";
    }

    public List<OutlineNode> GetParsedNodes()
    {
        if (IsStructuredMode)
            return StructuredEditor.GetNodes();
        return _parser.Parse(OutlineText).Nodes;
    }

    /// <summary>
    /// Gets the current text representation regardless of mode.
    /// </summary>
    public string GetOutlineText()
    {
        if (IsStructuredMode)
            return StructuredEditor.ToMarkdownText();
        return OutlineText;
    }

    public void LoadOutlineText(string text)
    {
        OutlineText = text;
        if (IsStructuredMode)
            StructuredEditor.LoadFromText(text);
    }
}
