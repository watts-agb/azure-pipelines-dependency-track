using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DependencyTrackUploader.Models;

namespace DependencyTrackUploader.Client;

public class DTrackClient : IDTrackClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;

    public DTrackClient(string baseUrl, string apiKey, string? caFilePath = null)
    {
        var handler = new HttpClientHandler();

        if (!string.IsNullOrWhiteSpace(caFilePath))
        {
            var caCert = new X509Certificate2(caFilePath);
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                    return true;

                if (chain == null || cert == null)
                    return false;

                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(caCert);
                return chain.Build(cert);
            };
        }

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/'))
        };
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        _ownsClient = true;
    }

    internal DTrackClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _ownsClient = false;
    }

    public async Task<string> UploadBomAsync(string projectId, string bom)
    {
        var formData = new MultipartFormDataContent
        {
            { new StringContent(projectId), "project" },
            { new StringContent(bom), "bom" }
        };

        return await PostBomAsync(formData);
    }

    public async Task<string> UploadBomAndCreateProjectAsync(string name, string version, bool isLatest, string bom)
    {
        var formData = new MultipartFormDataContent
        {
            { new StringContent("true"), "autoCreate" },
            { new StringContent(name), "projectName" },
            { new StringContent(version), "projectVersion" },
            { new StringContent(isLatest.ToString().ToLowerInvariant()), "isLatest" },
            { new StringContent(bom), "bom" }
        };

        return await PostBomAsync(formData);
    }

    public async Task<string> UploadBomAndCreateChildProjectAsync(string name, string version, string parentUuid, bool isLatest, string bom)
    {
        var formData = new MultipartFormDataContent
        {
            { new StringContent("true"), "autoCreate" },
            { new StringContent(name), "projectName" },
            { new StringContent(version), "projectVersion" },
            { new StringContent(parentUuid), "parentUUID" },
            { new StringContent(isLatest.ToString().ToLowerInvariant()), "isLatest" },
            { new StringContent(bom), "bom" }
        };

        return await PostBomAsync(formData);
    }

    public async Task<string> CreateProjectAsync(string projectName, string projectVersion)
    {
        var payload = new { name = projectName, version = projectVersion };
        var response = await _httpClient.PostAsJsonAsync("/api/v1/project", payload);

        if (response.StatusCode == System.Net.HttpStatusCode.Created)
        {
            var project = await response.Content.ReadFromJsonAsync<ProjectInfo>()
                ?? throw new InvalidOperationException("Failed to deserialize project response.");
            return project.Uuid;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Failed to create project. Status: {(int)response.StatusCode}. Body: {body}");
    }

    public async Task<string> GetProjectUuidAsync(string projectName, string? projectVersion)
    {
        if (string.IsNullOrWhiteSpace(projectVersion))
            return await GetProjectUuidByNameAsync(projectName);

        var encodedName = Uri.EscapeDataString(projectName);
        var encodedVersion = Uri.EscapeDataString(projectVersion);
        var response = await _httpClient.GetAsync($"/api/v1/project/lookup?name={encodedName}&version={encodedVersion}");
        response.EnsureSuccessStatusCode();

        var project = await response.Content.ReadFromJsonAsync<ProjectInfo>();
        return project?.Uuid ?? string.Empty;
    }

    public async Task<bool> PullProcessingStatusAsync(string token)
    {
        var response = await _httpClient.GetAsync($"/api/v1/event/token/{token}");
        response.EnsureSuccessStatusCode();

        var status = await response.Content.ReadFromJsonAsync<ProcessingStatus>()
            ?? throw new InvalidOperationException("Failed to deserialize processing status.");
        return status.Processing;
    }

    public async Task<ProjectMetrics> GetProjectMetricsAsync(string projectId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/metrics/project/{projectId}/current");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProjectMetrics>()
            ?? throw new InvalidOperationException("Failed to deserialize project metrics.");
    }

    public async Task<DateTime> GetLastMetricCalculationDateAsync(string projectId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/metrics/project/{projectId}/current");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content) || content == "{}")
            return DateTime.MinValue;

        var metrics = JsonSerializer.Deserialize<ProjectMetrics>(content);
        if (metrics == null)
            return DateTime.MinValue;

        return DateTimeOffset.FromUnixTimeMilliseconds(metrics.LastOccurrence).UtcDateTime;
    }

    public async Task<ProjectInfo> GetProjectInfoAsync(string projectId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/project/{projectId}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProjectInfo>()
            ?? throw new InvalidOperationException("Failed to deserialize project info.");
    }

    public async Task<ProjectInfo> UpdateProjectAsync(string projectId, string? description, string? classifier,
        string? swidTagId, string? group, List<ProjectTag>? tags, bool? isLatest)
    {
        var data = new Dictionary<string, object?>();

        if (description != null) data["description"] = description;
        if (classifier != null) data["classifier"] = classifier;
        if (swidTagId != null) data["swidTagId"] = swidTagId;
        if (group != null) data["group"] = group;
        if (tags != null) data["tags"] = tags;
        if (isLatest != null) data["isLatest"] = isLatest;

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/project/{projectId}")
        {
            Content = JsonContent.Create(data)
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProjectInfo>()
            ?? throw new InvalidOperationException("Failed to deserialize project info.");
    }

    private async Task<string> PostBomAsync(MultipartFormDataContent formData)
    {
        var response = await _httpClient.PostAsync("/api/v1/bom", formData);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BomUploadResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize BOM upload response.");
        return result.Token;
    }

    private async Task<string> GetProjectUuidByNameAsync(string projectName)
    {
        var encodedName = Uri.EscapeDataString(projectName);
        var response = await _httpClient.GetAsync($"/api/v1/project?name={encodedName}");
        response.EnsureSuccessStatusCode();

        if (response.Headers.TryGetValues("X-Total-Count", out var totalValues))
        {
            if (int.TryParse(totalValues.FirstOrDefault(), out var count) && count > 1)
                throw new InvalidOperationException("Multiple projects found with the same name. Please specify a version.");
        }

        var projects = await response.Content.ReadFromJsonAsync<List<ProjectInfo>>();
        return projects?.FirstOrDefault()?.Uuid ?? string.Empty;
    }

    public void Dispose()
    {
        if (_ownsClient)
            _httpClient.Dispose();
    }
}
