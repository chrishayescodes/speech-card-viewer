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

        // Reuse cached viewer if outline hasn't changed
        if (_cardViewerViewModel != null && currentText == _lastOutlineText)
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
        _cardViewerViewModel = new CardViewerViewModel(cards, nodes);
        CurrentView = _cardViewerViewModel;
        IsEditorActive = false;
        IsPracticeActive = true;
        StatusMessage = $"Practice mode — {cards.Count} cards";
    }

    public async Task SaveOutlineAsync(string filePath)
    {
        var outline = _parser.ParseToOutline(_editorViewModel.GetOutlineText());
        await _persistence.SaveAsync(outline, filePath);
        StatusMessage = $"Saved to {Path.GetFileName(filePath)}";
    }

    public async Task OpenOutlineAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext is ".md" or ".txt")
        {
            var text = await File.ReadAllTextAsync(filePath);
            _editorViewModel.LoadOutlineText(text);
            StatusMessage = $"Imported {Path.GetFileName(filePath)}";
        }
        else
        {
            var outline = await _persistence.LoadAsync(filePath);
            _editorViewModel.LoadOutlineText(OutlineToText(outline));
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
        _pdfExporter.ExportCards(cards, filePath);
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
        _cardViewerViewModel = new CardViewerViewModel(cards, nodes, bestIndex);
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
