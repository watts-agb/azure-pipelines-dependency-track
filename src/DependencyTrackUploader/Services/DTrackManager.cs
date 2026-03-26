using System.Text.Json;
using DependencyTrackUploader.Client;
using DependencyTrackUploader.Models;

namespace DependencyTrackUploader.Services;

public class DTrackManager
{
    private readonly IDTrackClient _client;

    public DTrackManager(IDTrackClient client)
    {
        _client = client;
    }

    public async Task<string> GetProjectUuidAsync(string name, string? version)
    {
        try
        {
            return await _client.GetProjectUuidAsync(name, version);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unable to find Dependency Track project with name '{name}' and version '{version}'.", ex);
        }
    }

    public async Task<ProjectInfo> GetProjectInfoAsync(string projectId)
    {
        return await _client.GetProjectInfoAsync(projectId);
    }

    public async Task UpdateProjectAsync(string projectId, string? description, string? classifier,
        string? swidTagId, string? group, List<string>? tags, bool? isLatest)
    {
        var hasUpdateParams = description != null ||
                              classifier != null ||
                              swidTagId != null ||
                              group != null ||
                              (tags != null && tags.Count > 0) ||
                              isLatest != null;

        if (!hasUpdateParams)
        {
            Console.WriteLine("Project settings don't need to be updated.");
            return;
        }

        var projectInfo = await GetProjectInfoAsync(projectId);

        var updatedDescription = (string?)null;
        var updatedClassifier = (string?)null;
        var updatedSwidTagId = (string?)null;
        var updatedGroup = (string?)null;
        List<ProjectTag>? updatedTags = null;
        bool? updatedIsLatest = null;

        if (description != null && projectInfo.Description != description)
            updatedDescription = description;

        if (classifier != null && projectInfo.Classifier != classifier)
            updatedClassifier = classifier;

        if (swidTagId != null && projectInfo.SwidTagId != swidTagId)
            updatedSwidTagId = swidTagId;

        if (group != null && projectInfo.Group != group)
            updatedGroup = group;

        if (tags != null && tags.Count > 0)
        {
            var existingTagNames = projectInfo.Tags?.Select(t => t.Name.ToLowerInvariant()).OrderBy(t => t).ToList() ?? new List<string>();
            var newTagNames = tags.Select(t => t.ToLowerInvariant()).OrderBy(t => t).ToList();

            if (!existingTagNames.SequenceEqual(newTagNames))
                updatedTags = tags.Select(t => new ProjectTag { Name = t }).ToList();
        }

        if (isLatest != null && projectInfo.IsLatest != isLatest.Value)
            updatedIsLatest = isLatest;

        Console.WriteLine("Current project settings:");
        Console.WriteLine($"  Id: {projectId}");
        Console.WriteLine($"  Name: {projectInfo.Name}");
        Console.WriteLine($"  Version: {projectInfo.Version}");
        Console.WriteLine($"  Description: {projectInfo.Description}");
        Console.WriteLine($"  Classifier: {projectInfo.Classifier}");
        Console.WriteLine($"  SWID Tag ID: {projectInfo.SwidTagId}");
        Console.WriteLine($"  Group: {projectInfo.Group}");
        Console.WriteLine($"  Tags: {JsonSerializer.Serialize(projectInfo.Tags)}");
        Console.WriteLine($"  Is Latest: {projectInfo.IsLatest}");

        var hasChanges = updatedDescription != null || updatedClassifier != null ||
                         updatedSwidTagId != null || updatedGroup != null ||
                         updatedTags != null || updatedIsLatest != null;

        if (!hasChanges)
        {
            Console.WriteLine("Project settings don't need to be updated.");
            return;
        }

        // Force isLatest handling to work around DependencyTrack issue #5279
        var effectiveIsLatest = isLatest ?? projectInfo.IsLatest;

        Console.WriteLine("Updating project...");
        var newSettings = await _client.UpdateProjectAsync(
            projectId, updatedDescription, updatedClassifier,
            updatedSwidTagId, updatedGroup, updatedTags, effectiveIsLatest);

        Console.WriteLine("Update succeeded. New project settings:");
        Console.WriteLine($"  Id: {projectId}");
        Console.WriteLine($"  Name: {newSettings.Name}");
        Console.WriteLine($"  Version: {newSettings.Version}");
        Console.WriteLine($"  Description: {newSettings.Description}");
        Console.WriteLine($"  Classifier: {newSettings.Classifier}");
        Console.WriteLine($"  SWID Tag ID: {newSettings.SwidTagId}");
        Console.WriteLine($"  Group: {newSettings.Group}");
        Console.WriteLine($"  Tags: {JsonSerializer.Serialize(newSettings.Tags)}");
        Console.WriteLine($"  Is Latest: {newSettings.IsLatest}");
    }

    public async Task<string> UploadBomAsync(string projectId, string bom)
    {
        try
        {
            return await _client.UploadBomAsync(projectId, bom);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Uploading the BOM to Dependency Track failed.", ex);
        }
    }

    public async Task<string> UploadBomAndCreateProjectAsync(string name, string version, bool isLatest, string bom)
    {
        try
        {
            return await _client.UploadBomAndCreateProjectAsync(name, version, isLatest, bom);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Uploading the BOM to Dependency Track failed.", ex);
        }
    }

    public async Task<string> UploadBomAndCreateChildProjectAsync(string name, string version,
        string parentName, string? parentVersion, bool isLatest, string bom)
    {
        // Ensure parent project exists, create it if it does not
        string parentUuid;
        try
        {
            parentUuid = await GetProjectUuidAsync(parentName, parentVersion);

            if (string.IsNullOrEmpty(parentUuid))
                throw new InvalidOperationException("Parent project UUID was empty.");
        }
        catch
        {
            Console.WriteLine($"Parent project '{parentName}' (version: '{parentVersion ?? "any"}') not found. Creating it...");
            parentUuid = await _client.CreateProjectAsync(parentName, parentVersion ?? "default");
            Console.WriteLine($"Parent project created with UUID: {parentUuid}");
        }

        try
        {
            return await _client.UploadBomAndCreateChildProjectAsync(name, version, parentUuid, isLatest, bom);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Uploading the BOM to Dependency Track failed.", ex);
        }
    }

    public async Task WaitBomProcessingAsync(string token)
    {
        var processing = true;
        while (processing)
        {
            await Task.Delay(2000);
            Console.WriteLine("Polling Dependency Track for update...");
            try
            {
                processing = await _client.PullProcessingStatusAsync(token);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Polling Dependency Track for update failed.", ex);
            }
        }
    }

    public async Task WaitMetricsRefreshAsync(string projectId)
    {
        var projectInfo = await GetProjectInfoAsync(projectId);
        var lastBomImport = projectInfo.LastBomImport ?? DateTime.MinValue;
        DateTime lastOccurrence;

        do
        {
            await Task.Delay(2000);
            Console.WriteLine("Polling Dependency Track for update...");
            try
            {
                lastOccurrence = await _client.GetLastMetricCalculationDateAsync(projectId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Polling Dependency Track for update failed.", ex);
            }
        } while (lastOccurrence < lastBomImport);

        Console.WriteLine($"Last date of BOM import: {lastBomImport}");
        Console.WriteLine($"Last date of Metrics update: {lastOccurrence}");
    }

    public async Task<ProjectMetrics> GetProjectMetricsAsync(string projectId)
    {
        try
        {
            return await _client.GetProjectMetricsAsync(projectId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve project metrics.", ex);
        }
    }
}
