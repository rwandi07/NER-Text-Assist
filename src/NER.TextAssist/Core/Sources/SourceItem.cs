using System.Text.Json.Serialization;

namespace NER.TextAssist.Core.Sources;

public sealed class SourceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public int CharacterCount { get; set; }
    public int WordCount { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string ExtractedText { get; set; } = string.Empty;

    [JsonIgnore]
    public string TypeLabel => Extension.TrimStart('.').ToUpperInvariant();

    [JsonIgnore]
    public string Summary => $"{TypeLabel} · {WordCount:N0} kata · {CharacterCount:N0} karakter";

    [JsonIgnore]
    public string ImportedAtLabel => ImportedAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm");
}
