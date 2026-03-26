using DependencyTrackUploader.Models;

namespace DependencyTrackUploader.Client;

public interface IDTrackClient
{
    Task<string> UploadBomAsync(string projectId, string bom);
    Task<string> UploadBomAndCreateProjectAsync(string name, string version, bool isLatest, string bom);
    Task<string> UploadBomAndCreateChildProjectAsync(string name, string version, string parentUuid, bool isLatest, string bom);
    Task<string> CreateProjectAsync(string projectName, string projectVersion);
    Task<string> GetProjectUuidAsync(string projectName, string? projectVersion);
    Task<bool> PullProcessingStatusAsync(string token);
    Task<ProjectMetrics> GetProjectMetricsAsync(string projectId);
    Task<DateTime> GetLastMetricCalculationDateAsync(string projectId);
    Task<ProjectInfo> GetProjectInfoAsync(string projectId);
    Task<ProjectInfo> UpdateProjectAsync(string projectId, string? description, string? classifier,
        string? swidTagId, string? group, List<ProjectTag>? tags, bool? isLatest);
}
