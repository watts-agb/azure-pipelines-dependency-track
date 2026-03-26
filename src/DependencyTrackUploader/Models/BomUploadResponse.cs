using System.Text.Json.Serialization;

namespace DependencyTrackUploader.Models;

public class BomUploadResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
