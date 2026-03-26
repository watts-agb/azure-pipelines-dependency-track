using DependencyTrackUploader.Client;
using DependencyTrackUploader.Models;
using DependencyTrackUploader.Services;
using Moq;

namespace DependencyTrackUploader.Tests;

public class DTrackManagerTests
{
    private readonly Mock<IDTrackClient> _mockClient;
    private readonly DTrackManager _manager;

    public DTrackManagerTests()
    {
        _mockClient = new Mock<IDTrackClient>();
        _manager = new DTrackManager(_mockClient.Object);
    }

    [Fact]
    public async Task GetProjectUuidAsync_ReturnsUuid()
    {
        _mockClient.Setup(c => c.GetProjectUuidAsync("proj", "1.0"))
            .ReturnsAsync("test-uuid");

        var result = await _manager.GetProjectUuidAsync("proj", "1.0");

        Assert.Equal("test-uuid", result);
    }

    [Fact]
    public async Task GetProjectUuidAsync_ClientThrows_ThrowsInvalidOperationException()
    {
        _mockClient.Setup(c => c.GetProjectUuidAsync("proj", "1.0"))
            .ThrowsAsync(new HttpRequestException("Not found"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.GetProjectUuidAsync("proj", "1.0"));

        Assert.Contains("proj", ex.Message);
        Assert.Contains("1.0", ex.Message);
    }

    [Fact]
    public async Task UploadBomAsync_ReturnsToken()
    {
        _mockClient.Setup(c => c.UploadBomAsync("proj-id", "bom-content"))
            .ReturnsAsync("upload-token");

        var result = await _manager.UploadBomAsync("proj-id", "bom-content");

        Assert.Equal("upload-token", result);
    }

    [Fact]
    public async Task UploadBomAsync_ClientThrows_ThrowsInvalidOperationException()
    {
        _mockClient.Setup(c => c.UploadBomAsync("proj-id", "bom-content"))
            .ThrowsAsync(new HttpRequestException("Failed"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.UploadBomAsync("proj-id", "bom-content"));

        Assert.Contains("Uploading the BOM", ex.Message);
    }

    [Fact]
    public async Task UploadBomAndCreateProjectAsync_ReturnsToken()
    {
        _mockClient.Setup(c => c.UploadBomAndCreateProjectAsync("proj", "1.0", false, "bom"))
            .ReturnsAsync("create-token");

        var result = await _manager.UploadBomAndCreateProjectAsync("proj", "1.0", false, "bom");

        Assert.Equal("create-token", result);
    }

    [Fact]
    public async Task UploadBomAndCreateChildProjectAsync_ParentExists_ReturnsToken()
    {
        _mockClient.Setup(c => c.GetProjectUuidAsync("parent", "1.0"))
            .ReturnsAsync("parent-uuid");
        _mockClient.Setup(c => c.UploadBomAndCreateChildProjectAsync("child", "1.0", "parent-uuid", false, "bom"))
            .ReturnsAsync("child-token");

        var result = await _manager.UploadBomAndCreateChildProjectAsync("child", "1.0", "parent", "1.0", false, "bom");

        Assert.Equal("child-token", result);
    }

    [Fact]
    public async Task UploadBomAndCreateChildProjectAsync_ParentDoesNotExist_CreatesParentAndReturnsToken()
    {
        // Parent lookup fails
        _mockClient.Setup(c => c.GetProjectUuidAsync("parent", "1.0"))
            .ThrowsAsync(new HttpRequestException("Not found"));

        // Parent creation succeeds
        _mockClient.Setup(c => c.CreateProjectAsync("parent", "1.0"))
            .ReturnsAsync("new-parent-uuid");

        // Child upload succeeds
        _mockClient.Setup(c => c.UploadBomAndCreateChildProjectAsync("child", "1.0", "new-parent-uuid", false, "bom"))
            .ReturnsAsync("child-token");

        var result = await _manager.UploadBomAndCreateChildProjectAsync("child", "1.0", "parent", "1.0", false, "bom");

        Assert.Equal("child-token", result);
        _mockClient.Verify(c => c.CreateProjectAsync("parent", "1.0"), Times.Once);
    }

    [Fact]
    public async Task UploadBomAndCreateChildProjectAsync_ParentReturnsEmptyUuid_CreatesParent()
    {
        // Parent lookup returns empty UUID
        _mockClient.Setup(c => c.GetProjectUuidAsync("parent", "2.0"))
            .ReturnsAsync(string.Empty);

        // Parent creation succeeds
        _mockClient.Setup(c => c.CreateProjectAsync("parent", "2.0"))
            .ReturnsAsync("new-parent-uuid");

        // Child upload succeeds
        _mockClient.Setup(c => c.UploadBomAndCreateChildProjectAsync("child", "1.0", "new-parent-uuid", true, "bom"))
            .ReturnsAsync("child-token");

        var result = await _manager.UploadBomAndCreateChildProjectAsync("child", "1.0", "parent", "2.0", true, "bom");

        Assert.Equal("child-token", result);
        _mockClient.Verify(c => c.CreateProjectAsync("parent", "2.0"), Times.Once);
    }

    [Fact]
    public async Task UploadBomAndCreateChildProjectAsync_ParentVersionNull_UsesDefault()
    {
        // Parent lookup fails
        _mockClient.Setup(c => c.GetProjectUuidAsync("parent", null))
            .ThrowsAsync(new HttpRequestException("Not found"));

        // Parent creation with "default" version
        _mockClient.Setup(c => c.CreateProjectAsync("parent", "default"))
            .ReturnsAsync("new-parent-uuid");

        // Child upload succeeds
        _mockClient.Setup(c => c.UploadBomAndCreateChildProjectAsync("child", "1.0", "new-parent-uuid", false, "bom"))
            .ReturnsAsync("child-token");

        var result = await _manager.UploadBomAndCreateChildProjectAsync("child", "1.0", "parent", null, false, "bom");

        Assert.Equal("child-token", result);
        _mockClient.Verify(c => c.CreateProjectAsync("parent", "default"), Times.Once);
    }

    [Fact]
    public async Task GetProjectMetricsAsync_ReturnsMetrics()
    {
        var metrics = new ProjectMetrics { Critical = 1, High = 2, Medium = 3 };
        _mockClient.Setup(c => c.GetProjectMetricsAsync("proj-id"))
            .ReturnsAsync(metrics);

        var result = await _manager.GetProjectMetricsAsync("proj-id");

        Assert.Equal(1, result.Critical);
        Assert.Equal(2, result.High);
        Assert.Equal(3, result.Medium);
    }

    [Fact]
    public async Task UpdateProjectAsync_NoChanges_DoesNotCallClient()
    {
        await _manager.UpdateProjectAsync("proj-id", null, null, null, null, null, null);

        _mockClient.Verify(c => c.UpdateProjectAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<ProjectTag>?>(), It.IsAny<bool?>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateProjectAsync_WithChanges_CallsClientUpdate()
    {
        var projectInfo = new ProjectInfo
        {
            Uuid = "proj-id",
            Name = "proj",
            Version = "1.0",
            Description = "old description",
            IsLatest = false
        };

        _mockClient.Setup(c => c.GetProjectInfoAsync("proj-id"))
            .ReturnsAsync(projectInfo);

        _mockClient.Setup(c => c.UpdateProjectAsync("proj-id", "new description", null, null, null, null, false))
            .ReturnsAsync(new ProjectInfo
            {
                Uuid = "proj-id",
                Name = "proj",
                Version = "1.0",
                Description = "new description",
                IsLatest = false
            });

        await _manager.UpdateProjectAsync("proj-id", "new description", null, null, null, null, null);

        _mockClient.Verify(c => c.UpdateProjectAsync("proj-id", "new description", null, null, null, null, false), Times.Once);
    }
}
