using System.Text.Json.Serialization;

namespace DependencyTrackUploader.Models;

public class ProcessingStatus
{
    [JsonPropertyName("processing")]
    public bool Processing { get; set; }
}
