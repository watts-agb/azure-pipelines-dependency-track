using System.Text.Json.Serialization;

namespace DependencyTrackUploader.Models;

public class ProjectMetrics
{
    [JsonPropertyName("critical")]
    public int Critical { get; set; }

    [JsonPropertyName("high")]
    public int High { get; set; }

    [JsonPropertyName("medium")]
    public int Medium { get; set; }

    [JsonPropertyName("low")]
    public int Low { get; set; }

    [JsonPropertyName("unassigned")]
    public int Unassigned { get; set; }

    [JsonPropertyName("suppressed")]
    public int Suppressed { get; set; }

    [JsonPropertyName("policyViolationsFail")]
    public int PolicyViolationsFail { get; set; }

    [JsonPropertyName("policyViolationsWarn")]
    public int PolicyViolationsWarn { get; set; }

    [JsonPropertyName("policyViolationsInfo")]
    public int PolicyViolationsInfo { get; set; }

    [JsonPropertyName("policyViolationsTotal")]
    public int PolicyViolationsTotal { get; set; }

    [JsonPropertyName("lastOccurrence")]
    public long LastOccurrence { get; set; }
}
