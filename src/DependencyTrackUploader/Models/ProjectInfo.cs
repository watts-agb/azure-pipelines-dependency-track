using System.Text.Json.Serialization;

namespace DependencyTrackUploader.Models;

public class ProjectInfo
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("classifier")]
    public string? Classifier { get; set; }

    [JsonPropertyName("swidTagId")]
    public string? SwidTagId { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("tags")]
    public List<ProjectTag>? Tags { get; set; }

    [JsonPropertyName("isLatest")]
    public bool IsLatest { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("lastBomImport")]
    public DateTime? LastBomImport { get; set; }
}

public class ProjectTag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
