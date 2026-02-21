using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CardViewer.ViewModels;

namespace CardViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Outline",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Card Viewer Files") { Patterns = new[] { "*.cdv", "*.cardviewer.json" } },
                new FilePickerFileType("Markdown Files") { Patterns = new[] { "*.md", "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0 && ViewModel != null)
        {
            var path = files[0].Path.LocalPath;
            await ViewModel.OpenOutlineAsync(path);
        }
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Markdown Outline",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Markdown Files") { Patterns = new[] { "*.md", "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0 && ViewModel != null)
        {
            var path = files[0].Path.LocalPath;
            await ViewModel.OpenOutlineAsync(path);
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        if (ViewModel.HasFilePath)
        {
            await ViewModel.SaveAsync();
        }
        else
        {
            await SaveAsAsync();
        }
    }

    private async void OnSaveAsClick(object? sender, RoutedEventArgs e)
    {
        await SaveAsAsync();
    }

    private async Task SaveAsAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Outline As",
            DefaultExtension = "cdv",
            SuggestedFileName = "outline",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Card Viewer Files") { Patterns = new[] { "*.cdv" } }
            }
        });

        if (file != null && ViewModel != null)
        {
            await ViewModel.SaveOutlineAsync(file.Path.LocalPath);
        }
    }

    private async void OnExportPdfClick(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Cards as PDF",
            DefaultExtension = "pdf",
            SuggestedFileName = "speech-cards",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } }
            }
        });

        if (file != null && ViewModel != null)
        {
            ViewModel.ExportPdf(file.Path.LocalPath);
        }
    }
}
