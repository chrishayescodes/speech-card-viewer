using CardViewer.Models;
using CardViewer.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CardViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly OutlineParser _parser = new();
    private readonly CardGenerator _cardGenerator = new();
    private readonly OutlinePersistence _persistence = new();
    private readonly PdfExporter _pdfExporter = new();

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isEditorActive = true;

    [ObservableProperty]
    private bool _isPracticeActive;

    private readonly OutlineEditorViewModel _editorViewModel;
    private CardViewerViewModel? _cardViewerViewModel;
    private string _lastOutlineText = "";
    private string _lastShowTitle = "";
    private string? _currentFilePath;

    public MainWindowViewModel()
    {
        _editorViewModel = new OutlineEditorViewModel();
        _editorViewModel.NodeDoubleClicked += NavigateToViewerAtNode;
        CurrentView = _editorViewModel;
    }

    [RelayCommand]
    private void NavigateToEditor()
    {
        CurrentView = _editorViewModel;
        IsEditorActive = true;
        IsPracticeActive = false;
        StatusMessage = "Editor";
    }

    [RelayCommand]
    private void NavigateToViewer()
    {
        var currentText = _editorViewModel.GetOutlineText();

        // Reuse cached viewer if outline and title haven't changed
        if (_cardViewerViewModel != null && currentText == _lastOutlineText && _editorViewModel.ShowTitle == _lastShowTitle)
        {
            CurrentView = _cardViewerViewModel;
            IsEditorActive = false;
            IsPracticeActive = true;
            StatusMessage = $"Practice mode — {_cardViewerViewModel.TotalCards} cards";
            return;
        }

        var nodes = _editorViewModel.GetParsedNodes();
        if (nodes.Count == 0)
        {
            StatusMessage = "No outline to practice — add some content first";
            return;
        }

        var cards = _cardGenerator.GenerateCards(nodes);
        if (cards.Count == 0)
        {
            StatusMessage = "No leaf nodes found — add detail items to your outline";
            return;
        }

        _lastOutlineText = currentText;
        _lastShowTitle = _editorViewModel.ShowTitle;
        _cardViewerViewModel = new CardViewerViewModel(cards, nodes, showTitle: _editorViewModel.ShowTitle);
        CurrentView = _cardViewerViewModel;
        IsEditorActive = false;
        IsPracticeActive = true;
        StatusMessage = $"Practice mode — {cards.Count} cards";
    }

    public bool HasFilePath => _currentFilePath != null;

    public async Task SaveOutlineAsync(string filePath)
    {
        var text = _editorViewModel.GetOutlineText();
        var nodes = _editorViewModel.GetParsedNodes();
        var title = _editorViewModel.ShowTitle;
        var outline = _parser.ParseToOutline(text, string.IsNullOrEmpty(title) ? "Untitled Outline" : title);
        await _persistence.SaveAsync(outline, filePath);
        _currentFilePath = filePath;
        StatusMessage = $"Saved to {Path.GetFileName(filePath)}";
    }

    public async Task SaveAsync()
    {
        if (_currentFilePath != null)
            await SaveOutlineAsync(_currentFilePath);
    }

    public async Task OpenOutlineAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext is ".md" or ".txt")
        {
            var text = await File.ReadAllTextAsync(filePath);
            _editorViewModel.LoadOutlineText(text);
            _currentFilePath = null;
            StatusMessage = $"Imported {Path.GetFileName(filePath)}";
        }
        else
        {
            var outline = await _persistence.LoadAsync(filePath);
            _editorViewModel.LoadOutlineText(OutlineToText(outline), outline.Name);
            _currentFilePath = filePath;
            StatusMessage = $"Opened {Path.GetFileName(filePath)}";
        }

        _cardViewerViewModel = null;
        CurrentView = _editorViewModel;
        IsEditorActive = true;
        IsPracticeActive = false;
    }

    public void ExportPdf(string filePath)
    {
        var nodes = _editorViewModel.GetParsedNodes();
        var cards = _cardGenerator.GenerateCards(nodes);
        if (cards.Count == 0)
        {
            StatusMessage = "No cards to export";
            return;
        }
        _pdfExporter.ExportCards(cards, filePath, _editorViewModel.ShowTitle);
        StatusMessage = $"Exported {cards.Count} cards to {Path.GetFileName(filePath)}";
    }

    private void NavigateToViewerAtNode(OutlineNode node)
    {
        var nodes = _editorViewModel.GetParsedNodes();
        var cards = _cardGenerator.GenerateCards(nodes);
        if (cards.Count == 0)
        {
            StatusMessage = "No cards to practice";
            return;
        }

        var nodePath = node.GetBreadcrumb();
        int bestIndex = 0;

        for (int i = 0; i < cards.Count; i++)
        {
            var cardFullPath = new List<string>(cards[i].BreadcrumbPath) { cards[i].Topic };

            // Exact match — node is a card topic
            if (cardFullPath.SequenceEqual(nodePath))
            {
                bestIndex = i;
                break;
            }

            // Node is a parent — first card whose path starts with this node's path
            if (cardFullPath.Count >= nodePath.Count &&
                cardFullPath.Take(nodePath.Count).SequenceEqual(nodePath))
            {
                bestIndex = i;
                break;
            }

            // Node is a bullet — find card whose full path is a prefix of node's path
            if (nodePath.Count > cardFullPath.Count &&
                nodePath.Take(cardFullPath.Count).SequenceEqual(cardFullPath))
            {
                bestIndex = i;
                break;
            }
        }

        _lastOutlineText = _editorViewModel.GetOutlineText();
        _lastShowTitle = _editorViewModel.ShowTitle;
        _cardViewerViewModel = new CardViewerViewModel(cards, nodes, bestIndex, _editorViewModel.ShowTitle);
        CurrentView = _cardViewerViewModel;
        IsEditorActive = false;
        IsPracticeActive = true;
        StatusMessage = $"Practice mode — {cards.Count} cards";
    }

    private static string OutlineToText(Outline outline)
    {
        var lines = new List<string>();
        foreach (var node in outline.RootNodes)
            AppendNodeText(node, 0, lines);
        return string.Join("\n", lines);
    }

    private static void AppendNodeText(OutlineNode node, int depth, List<string> lines)
    {
        lines.Add(new string(' ', depth * 3) + node.Title);
        foreach (var child in node.Children)
            AppendNodeText(child, depth + 1, lines);
    }
}
