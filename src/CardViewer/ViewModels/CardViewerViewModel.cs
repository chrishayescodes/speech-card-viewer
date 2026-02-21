using System.Collections.ObjectModel;
using CardViewer.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CardViewer.ViewModels;

public partial class CardViewerViewModel : ViewModelBase
{
    private readonly List<SpeechCard> _cards;
    private readonly List<OutlineNode> _allNodes = new();

    [ObservableProperty]
    private SpeechCard? _currentCard;

    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _canGoPrevious;

    [ObservableProperty]
    private string _cardPosition = "";

    [ObservableProperty]
    private bool _canGoNextChapter;

    [ObservableProperty]
    private bool _canGoPreviousChapter;

    [ObservableProperty]
    private bool _isSidebarOpen = true;

    public ObservableCollection<OutlineNode> OutlineNodes { get; } = new();

    public int TotalCards => _cards.Count;

    public CardViewerViewModel(List<SpeechCard> cards, List<OutlineNode>? outlineNodes = null, int startIndex = 0)
    {
        _cards = cards;

        if (outlineNodes != null)
        {
            foreach (var node in outlineNodes)
                OutlineNodes.Add(node);
            FlattenNodes(outlineNodes, _allNodes);
        }

        if (_cards.Count > 0)
        {
            CurrentIndex = Math.Clamp(startIndex, 0, _cards.Count - 1);
            CurrentCard = _cards[CurrentIndex];
            UpdateNavigation();
            HighlightCurrentNode();
        }
    }

    [RelayCommand]
    private void NextCard()
    {
        if (CurrentIndex < _cards.Count - 1)
            GoToCard(CurrentIndex + 1);
    }

    [RelayCommand]
    private void PreviousCard()
    {
        if (CurrentIndex > 0)
            GoToCard(CurrentIndex - 1);
    }

    [RelayCommand]
    private void NextChapter()
    {
        if (CurrentCard == null) return;

        var currentChapter = GetChapter(CurrentIndex);

        for (int i = CurrentIndex + 1; i < _cards.Count; i++)
        {
            if (GetChapter(i) != currentChapter)
            {
                GoToCard(i);
                return;
            }
        }
    }

    [RelayCommand]
    private void PreviousChapter()
    {
        if (CurrentCard == null) return;

        var currentChapter = GetChapter(CurrentIndex);

        // Find the start of the current chapter
        int chapterStart = CurrentIndex;
        while (chapterStart > 0 && GetChapter(chapterStart - 1) == currentChapter)
            chapterStart--;

        if (CurrentIndex > chapterStart)
        {
            // Not at the start of current chapter — go to its start
            GoToCard(chapterStart);
        }
        else if (chapterStart > 0)
        {
            // At the start of current chapter — find start of previous chapter
            var prevChapter = GetChapter(chapterStart - 1);
            int prevStart = chapterStart - 1;
            while (prevStart > 0 && GetChapter(prevStart - 1) == prevChapter)
                prevStart--;
            GoToCard(prevStart);
        }
    }

    private int _chapterLevel = -1;

    private string GetChapter(int cardIndex)
    {
        if (_chapterLevel < 0)
            _chapterLevel = FindChapterLevel();

        var card = _cards[cardIndex];
        if (_chapterLevel < card.BreadcrumbPath.Count)
            return card.BreadcrumbPath[_chapterLevel];
        return card.Topic;
    }

    private int FindChapterLevel()
    {
        // Find the shallowest breadcrumb level with more than one distinct value
        int maxDepth = _cards.Max(c => c.BreadcrumbPath.Count);
        for (int level = 0; level < maxDepth; level++)
        {
            var l = level;
            var values = _cards
                .Where(c => c.BreadcrumbPath.Count > l)
                .Select(c => c.BreadcrumbPath[l])
                .Distinct()
                .Count();
            if (values > 1)
                return level;
        }
        // All breadcrumbs are identical — fall back to topic
        return maxDepth;
    }

    private void GoToCard(int index)
    {
        CurrentIndex = index;
        CurrentCard = _cards[CurrentIndex];
        UpdateNavigation();
        HighlightCurrentNode();
    }

    [RelayCommand]
    private void NavigateToNode(OutlineNode node)
    {
        var nodePath = node.GetBreadcrumb();

        for (int i = 0; i < _cards.Count; i++)
        {
            var cardFullPath = new List<string>(_cards[i].BreadcrumbPath) { _cards[i].Topic };

            if (cardFullPath.SequenceEqual(nodePath) ||
                (cardFullPath.Count >= nodePath.Count &&
                 cardFullPath.Take(nodePath.Count).SequenceEqual(nodePath)) ||
                (nodePath.Count > cardFullPath.Count &&
                 nodePath.Take(cardFullPath.Count).SequenceEqual(cardFullPath)))
            {
                CurrentIndex = i;
                CurrentCard = _cards[CurrentIndex];
                UpdateNavigation();
                HighlightCurrentNode();
                return;
            }
        }
    }

    [RelayCommand]
    private void ExpandAll()
    {
        foreach (var node in _allNodes)
            node.IsExpanded = true;
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var node in _allNodes)
            node.IsExpanded = false;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    private void UpdateNavigation()
    {
        CanGoNext = CurrentIndex < _cards.Count - 1;
        CanGoPrevious = CurrentIndex > 0;
        CardPosition = _cards.Count > 0
            ? $"Card {CurrentIndex + 1} of {_cards.Count}"
            : "No cards";

        if (_cards.Count > 0)
        {
            var currentChapter = GetChapter(CurrentIndex);
            CanGoNextChapter = Enumerable.Range(CurrentIndex + 1, _cards.Count - CurrentIndex - 1)
                .Any(i => GetChapter(i) != currentChapter);
            CanGoPreviousChapter = CurrentIndex > 0;
        }
    }

    private void HighlightCurrentNode()
    {
        foreach (var node in _allNodes)
            node.IsHighlighted = false;

        if (CurrentCard == null) return;

        var targetPath = new List<string>(CurrentCard.BreadcrumbPath) { CurrentCard.Topic };

        foreach (var node in _allNodes)
        {
            if (node.GetBreadcrumb().SequenceEqual(targetPath))
            {
                node.IsHighlighted = true;

                // Expand all ancestors so the node is visible
                var ancestor = node.Parent;
                while (ancestor != null)
                {
                    ancestor.IsExpanded = true;
                    ancestor = ancestor.Parent;
                }

                return;
            }
        }
    }

    private static void FlattenNodes(IEnumerable<OutlineNode> nodes, List<OutlineNode> result)
    {
        foreach (var node in nodes)
        {
            result.Add(node);
            FlattenNodes(node.Children, result);
        }
    }
}
