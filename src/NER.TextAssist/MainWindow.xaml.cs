using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NER.TextAssist.Core.Sources;

namespace NER.TextAssist;

public partial class MainWindow : Window
{
    private const string EmptyPreviewMessage =
        "Import TXT, DOCX, CSV, atau XLSX untuk melihat hasil ekstraksi teks di sini.";

    private readonly ObservableCollection<SourceItem> _sources = new();
    private readonly SourceLibrary _sourceLibrary = new();
    private readonly TextExtractionService _textExtractor = new();

    public MainWindow()
    {
        InitializeComponent();
        SourceListBox.ItemsSource = _sources;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var items = await _sourceLibrary.LoadAsync();
        foreach (var item in items)
        {
            _sources.Add(item);
        }

        UpdateSourceCount();
        if (_sources.Count > 0)
        {
            SourceListBox.SelectedIndex = 0;
            SetStatus($"{_sources.Count} sumber lokal dimuat.");
        }
    }

    private async void ImportSource_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Sumber Teks",
            Filter = "Dokumen didukung (*.txt;*.docx;*.csv;*.xlsx)|*.txt;*.docx;*.csv;*.xlsx|TXT (*.txt)|*.txt|Word (*.docx)|*.docx|CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        ImportButton.IsEnabled = false;
        var imported = 0;
        var failures = new List<string>();
        SourceItem? lastImported = null;

        try
        {
            for (var index = 0; index < dialog.FileNames.Length; index++)
            {
                var file = dialog.FileNames[index];
                SetStatus($"Mengekstrak {index + 1}/{dialog.FileNames.Length}: {Path.GetFileName(file)}");

                try
                {
                    if (!_textExtractor.IsSupported(file))
                    {
                        failures.Add($"{Path.GetFileName(file)} — format belum didukung");
                        continue;
                    }

                    var extractedText = await _textExtractor.ExtractAsync(file);
                    if (string.IsNullOrWhiteSpace(extractedText))
                    {
                        failures.Add($"{Path.GetFileName(file)} — tidak ditemukan teks yang dapat diekstrak");
                        continue;
                    }

                    var existing = _sources.FirstOrDefault(item => SamePath(item.OriginalPath, file));
                    var item = new SourceItem
                    {
                        Id = existing?.Id ?? Guid.NewGuid(),
                        Name = Path.GetFileName(file),
                        OriginalPath = Path.GetFullPath(file),
                        Extension = Path.GetExtension(file),
                        ImportedAtUtc = DateTime.UtcNow,
                        CharacterCount = extractedText.Length,
                        WordCount = CountWords(extractedText),
                        IsEnabled = existing?.IsEnabled ?? true,
                        ExtractedText = extractedText
                    };

                    await _sourceLibrary.SaveAsync(item);

                    if (existing is not null)
                    {
                        _sources.Remove(existing);
                    }

                    _sources.Insert(0, item);
                    lastImported = item;
                    imported++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{Path.GetFileName(file)} — {ex.Message}");
                }
            }
        }
        finally
        {
            ImportButton.IsEnabled = true;
        }

        UpdateSourceCount();
        if (lastImported is not null)
        {
            SourceListBox.SelectedItem = lastImported;
        }

        if (failures.Count == 0)
        {
            SetStatus($"Selesai. {imported} sumber berhasil diimpor dan diekstrak.");
            return;
        }

        SetStatus($"Selesai. {imported} berhasil, {failures.Count} gagal.");
        MessageBox.Show(
            string.Join(Environment.NewLine, failures),
            "Hasil Import",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void SourceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SourceListBox.SelectedItem is not SourceItem item)
        {
            PreviewTitleText.Text = "Preview Teks";
            PreviewMetaText.Text = "Belum ada sumber yang dipilih.";
            PreviewTextBox.Text = EmptyPreviewMessage;
            DeleteSourceButton.IsEnabled = false;
            return;
        }

        PreviewTitleText.Text = item.Name;
        PreviewMetaText.Text = $"{item.Summary} · diimpor {item.ImportedAtLabel}";
        PreviewTextBox.Text = item.ExtractedText;
        DeleteSourceButton.IsEnabled = true;
    }

    private async void DeleteSource_Click(object sender, RoutedEventArgs e)
    {
        if (SourceListBox.SelectedItem is not SourceItem item)
        {
            return;
        }

        var answer = MessageBox.Show(
            $"Hapus sumber '{item.Name}' dari NER Text Assist?\n\nFile asli tidak akan dihapus.",
            "Hapus Sumber",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        await _sourceLibrary.DeleteAsync(item);
        _sources.Remove(item);
        UpdateSourceCount();

        if (_sources.Count > 0)
        {
            SourceListBox.SelectedIndex = 0;
        }
        else
        {
            PreviewTitleText.Text = "Preview Teks";
            PreviewMetaText.Text = "Belum ada sumber yang dipilih.";
            PreviewTextBox.Text = EmptyPreviewMessage;
            DeleteSourceButton.IsEnabled = false;
        }

        SetStatus($"Sumber '{item.Name}' dihapus dari library lokal.");
    }

    private void UpdateSourceCount()
    {
        SourceCountText.Text = $"{_sources.Count} sumber";
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
    }

    private static int CountWords(string text) =>
        Regex.Matches(text, @"[\p{L}\p{N}]+(?:['’\-][\p{L}\p{N}]+)*").Count;

    private static bool SamePath(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
