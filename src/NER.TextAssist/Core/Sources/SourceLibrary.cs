using System.IO;
using System.Text.Json;

namespace NER.TextAssist.Core.Sources;

public sealed class SourceLibrary
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<SourceItem>> LoadAsync()
    {
        AppPaths.EnsureCreated();

        var items = new List<SourceItem>();
        foreach (var file in Directory.EnumerateFiles(AppPaths.SourcesDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                await using var stream = File.OpenRead(file);
                var item = await JsonSerializer.DeserializeAsync<SourceItem>(stream, JsonOptions);
                if (item is not null)
                {
                    items.Add(item);
                }
            }
            catch
            {
                // File sumber yang rusak tidak boleh membuat aplikasi gagal dibuka.
            }
        }

        return items
            .OrderByDescending(item => item.ImportedAtUtc)
            .ToList();
    }

    public async Task SaveAsync(SourceItem item)
    {
        AppPaths.EnsureCreated();
        var targetPath = GetMetadataPath(item.Id);
        var tempPath = targetPath + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, item, JsonOptions);
        }

        File.Move(tempPath, targetPath, true);
    }

    public Task DeleteAsync(SourceItem item)
    {
        var path = GetMetadataPath(item.Id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private static string GetMetadataPath(Guid id) =>
        Path.Combine(AppPaths.SourcesDirectory, $"{id:N}.json");
}
