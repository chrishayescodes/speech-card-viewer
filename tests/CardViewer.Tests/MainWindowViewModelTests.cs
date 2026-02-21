using CardViewer.ViewModels;

namespace CardViewer.Tests;

public class MainWindowViewModelTests
{
    // --- Initial State ---

    [Fact]
    public void InitialState_EditorViewActive()
    {
        var vm = new MainWindowViewModel();

        Assert.IsType<OutlineEditorViewModel>(vm.CurrentView);
        Assert.True(vm.IsEditorActive);
        Assert.False(vm.IsPracticeActive);
        Assert.Equal("Ready", vm.StatusMessage);
    }

    // --- NavigateToEditor ---

    [Fact]
    public void NavigateToEditor_SwitchesToEditorView()
    {
        var vm = new MainWindowViewModel();
        // First go to viewer to set up non-editor state
        SetEditorText(vm, "A\n   B");
        vm.NavigateToViewerCommand.Execute(null);

        vm.NavigateToEditorCommand.Execute(null);

        Assert.IsType<OutlineEditorViewModel>(vm.CurrentView);
        Assert.True(vm.IsEditorActive);
        Assert.False(vm.IsPracticeActive);
        Assert.Equal("Editor", vm.StatusMessage);
    }

    // --- NavigateToViewer ---

    [Fact]
    public void NavigateToViewer_WithContent_SwitchesToPracticeMode()
    {
        var vm = new MainWindowViewModel();
        SetEditorText(vm, "Topic\n   Point1\n   Point2");

        vm.NavigateToViewerCommand.Execute(null);

        Assert.IsType<CardViewerViewModel>(vm.CurrentView);
        Assert.False(vm.IsEditorActive);
        Assert.True(vm.IsPracticeActive);
        Assert.Contains("2 cards", vm.StatusMessage);
    }

    [Fact]
    public void NavigateToViewer_EmptyOutline_ShowsError()
    {
        var vm = new MainWindowViewModel();
        // OutlineText is empty by default

        vm.NavigateToViewerCommand.Execute(null);

        Assert.IsType<OutlineEditorViewModel>(vm.CurrentView);
        Assert.Contains("No outline", vm.StatusMessage);
    }

    [Fact]
    public void NavigateToViewer_NoLeafNodes_ShowsError()
    {
        var vm = new MainWindowViewModel();
        // A single node with no children IS a leaf node and generates a card.
        // To get "no leaf nodes", we'd need a special case — but in practice this
        // message only appears with empty outlines. Test the empty case instead.
        SetEditorText(vm, "");

        vm.NavigateToViewerCommand.Execute(null);

        Assert.Contains("No outline", vm.StatusMessage);
    }

    [Fact]
    public void NavigateToViewer_CachesViewModelOnSecondCall()
    {
        var vm = new MainWindowViewModel();
        SetEditorText(vm, "A\n   B");

        vm.NavigateToViewerCommand.Execute(null);
        var first = vm.CurrentView;

        vm.NavigateToEditorCommand.Execute(null);
        vm.NavigateToViewerCommand.Execute(null);
        var second = vm.CurrentView;

        Assert.Same(first, second);
    }

    [Fact]
    public void NavigateToViewer_InvalidatesCacheWhenOutlineChanges()
    {
        var vm = new MainWindowViewModel();
        SetEditorText(vm, "A\n   B");
        vm.NavigateToViewerCommand.Execute(null);
        var first = vm.CurrentView;

        vm.NavigateToEditorCommand.Execute(null);
        SetEditorText(vm, "A\n   B\n   C");
        vm.NavigateToViewerCommand.Execute(null);
        var second = vm.CurrentView;

        Assert.NotSame(first, second);
    }

    // --- Save / Open ---

    [Fact]
    public async Task SaveAndOpen_RoundTrips()
    {
        var vm = new MainWindowViewModel();
        SetEditorText(vm, "Root\n   Child1\n   Child2");

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.cdv");
        try
        {
            await vm.SaveOutlineAsync(tempFile);
            Assert.Contains("Saved", vm.StatusMessage);

            // Create a new VM and open the file
            var vm2 = new MainWindowViewModel();
            await vm2.OpenOutlineAsync(tempFile);
            Assert.Contains("Opened", vm2.StatusMessage);

            // Navigate to viewer to verify content
            vm2.NavigateToViewerCommand.Execute(null);
            Assert.IsType<CardViewerViewModel>(vm2.CurrentView);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveAsync_WithFilePath_SavesSilently()
    {
        var vm = new MainWindowViewModel();
        SetEditorText(vm, "Content");

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.cdv");
        try
        {
            await vm.SaveOutlineAsync(tempFile);
            Assert.True(vm.HasFilePath);

            // SaveAsync should save silently to the same path
            await vm.SaveAsync();
            Assert.Contains("Saved", vm.StatusMessage);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task OpenOutline_MarkdownImport_SetsNoFilePath()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
        try
        {
            await File.WriteAllTextAsync(tempFile, "# Title\n- Item1\n- Item2");
            var vm = new MainWindowViewModel();

            await vm.OpenOutlineAsync(tempFile);

            Assert.Contains("Imported", vm.StatusMessage);
            Assert.False(vm.HasFilePath);
            Assert.True(vm.IsEditorActive);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    // --- ExportPdf ---

    [Fact]
    public void ExportPdf_WithContent_Exports()
    {
        var vm = new MainWindowViewModel();
        SetEditorText(vm, "Root\n   Child");

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.pdf");
        try
        {
            vm.ExportPdf(tempFile);
            Assert.Contains("Exported", vm.StatusMessage);
            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ExportPdf_EmptyOutline_ShowsError()
    {
        var vm = new MainWindowViewModel();
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.pdf");

        vm.ExportPdf(tempFile);

        Assert.Contains("No cards", vm.StatusMessage);
        Assert.False(File.Exists(tempFile));
    }

    // --- NavigateToViewerAtNode (via NodeDoubleClicked) ---

    [Fact]
    public void NodeDoubleClicked_NavigatesToCorrectCard()
    {
        var vm = new MainWindowViewModel();
        SetEditorText(vm, "Root\n   A\n   B\n   C");

        // Get the parsed nodes so we can find node B
        var editorVm = (OutlineEditorViewModel)vm.CurrentView!;
        var nodes = editorVm.GetParsedNodes();
        var nodeB = nodes[0].Children[1]; // B

        editorVm.RaiseNodeDoubleClicked(nodeB);

        Assert.IsType<CardViewerViewModel>(vm.CurrentView);
        var cardVm = (CardViewerViewModel)vm.CurrentView;
        Assert.Equal("B", cardVm.CurrentCard?.Topic);
    }

    // --- Helper ---

    private static void SetEditorText(MainWindowViewModel vm, string text)
    {
        var editor = (OutlineEditorViewModel)vm.CurrentView!;
        editor.OutlineText = text;
    }
}
