using DependencyTrackUploader.Configuration;

namespace DependencyTrackUploader.Tests;

public class ConfigurationLoaderTests
{
    private readonly string _tempDir;

    public ConfigurationLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dtrack-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void LoadFromYaml_ValidFile_ReturnsSettings()
    {
        var yamlContent = @"
dtrack-url: https://dtrack.example.com
api-key: test-api-key
project-name: my-project
project-version: '1.0.0'
bom-file-path: /path/to/bom.xml
auto-create: true
is-latest: true
threshold-action: warn
threshold-critical: 0
threshold-high: 5
project-tags:
  - tag1
  - tag2
";
        var filePath = Path.Combine(_tempDir, "config.yaml");
        File.WriteAllText(filePath, yamlContent);

        var settings = ConfigurationLoader.LoadFromYaml(filePath);

        Assert.Equal("https://dtrack.example.com", settings.DTrackUrl);
        Assert.Equal("test-api-key", settings.ApiKey);
        Assert.Equal("my-project", settings.ProjectName);
        Assert.Equal("1.0.0", settings.ProjectVersion);
        Assert.Equal("/path/to/bom.xml", settings.BomFilePath);
        Assert.True(settings.AutoCreate);
        Assert.True(settings.IsLatest);
        Assert.Equal("warn", settings.ThresholdAction);
        Assert.Equal(0, settings.ThresholdCritical);
        Assert.Equal(5, settings.ThresholdHigh);
        Assert.NotNull(settings.ProjectTags);
        Assert.Equal(2, settings.ProjectTags.Count);
        Assert.Contains("tag1", settings.ProjectTags);
        Assert.Contains("tag2", settings.ProjectTags);
    }

    [Fact]
    public void LoadFromYaml_FileNotFound_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ConfigurationLoader.LoadFromYaml("/nonexistent/config.yaml"));
    }

    [Fact]
    public void BuildConfiguration_CliOverridesYaml()
    {
        var yamlContent = @"
dtrack-url: https://yaml-url.example.com
api-key: yaml-key
project-name: yaml-project
project-version: '1.0.0'
bom-file-path: /yaml/bom.xml
auto-create: false
threshold-action: none
threshold-critical: 10
";
        var filePath = Path.Combine(_tempDir, "config.yaml");
        File.WriteAllText(filePath, yamlContent);

        var cliSettings = new AppSettings
        {
            DTrackUrl = "https://cli-url.example.com",
            ProjectName = "cli-project",
            ThresholdCritical = 0
        };

        var result = ConfigurationLoader.BuildConfiguration(filePath, cliSettings);

        // CLI overrides
        Assert.Equal("https://cli-url.example.com", result.DTrackUrl);
        Assert.Equal("cli-project", result.ProjectName);
        Assert.Equal(0, result.ThresholdCritical);

        // YAML values kept where CLI not specified
        Assert.Equal("yaml-key", result.ApiKey);
        Assert.Equal("1.0.0", result.ProjectVersion);
        Assert.Equal("/yaml/bom.xml", result.BomFilePath);
        Assert.False(result.AutoCreate);
        Assert.Equal("none", result.ThresholdAction);
    }

    [Fact]
    public void BuildConfiguration_NoConfigFile_UsesCliOnly()
    {
        var cliSettings = new AppSettings
        {
            DTrackUrl = "https://cli-url.example.com",
            ApiKey = "cli-key",
            ProjectName = "cli-project",
            ProjectVersion = "2.0.0",
            BomFilePath = "/cli/bom.xml"
        };

        var result = ConfigurationLoader.BuildConfiguration(null, cliSettings);

        Assert.Equal("https://cli-url.example.com", result.DTrackUrl);
        Assert.Equal("cli-key", result.ApiKey);
        Assert.Equal("cli-project", result.ProjectName);
        Assert.Equal("2.0.0", result.ProjectVersion);
        Assert.Equal("/cli/bom.xml", result.BomFilePath);
    }

    [Fact]
    public void MergeFrom_NullValuesDoNotOverride()
    {
        var baseSettings = new AppSettings
        {
            DTrackUrl = "https://base-url.example.com",
            ApiKey = "base-key",
            ProjectName = "base-project"
        };

        var overrideSettings = new AppSettings
        {
            DTrackUrl = "https://override-url.example.com"
            // ApiKey and ProjectName are null
        };

        baseSettings.MergeFrom(overrideSettings);

        Assert.Equal("https://override-url.example.com", baseSettings.DTrackUrl);
        Assert.Equal("base-key", baseSettings.ApiKey);
        Assert.Equal("base-project", baseSettings.ProjectName);
    }

    [Fact]
    public void Validate_MissingBomFilePath_Throws()
    {
        var settings = new AppSettings
        {
            DTrackUrl = "https://dtrack.example.com",
            ApiKey = "key",
            ProjectName = "proj",
            ProjectVersion = "1.0"
        };

        var ex = Assert.Throws<ArgumentException>(() => settings.Validate());
        Assert.Contains("BOM file path", ex.Message);
    }

    [Fact]
    public void Validate_MissingDTrackUrl_Throws()
    {
        var bomPath = Path.Combine(_tempDir, "bom.xml");
        File.WriteAllText(bomPath, "<bom/>");

        var settings = new AppSettings
        {
            ApiKey = "key",
            ProjectName = "proj",
            ProjectVersion = "1.0",
            BomFilePath = bomPath
        };

        var ex = Assert.Throws<ArgumentException>(() => settings.Validate());
        Assert.Contains("URL", ex.Message);
    }

    [Fact]
    public void Validate_MissingApiKey_Throws()
    {
        var bomPath = Path.Combine(_tempDir, "bom.xml");
        File.WriteAllText(bomPath, "<bom/>");

        var settings = new AppSettings
        {
            DTrackUrl = "https://dtrack.example.com",
            ProjectName = "proj",
            ProjectVersion = "1.0",
            BomFilePath = bomPath
        };

        var ex = Assert.Throws<ArgumentException>(() => settings.Validate());
        Assert.Contains("API key", ex.Message);
    }

    [Fact]
    public void Validate_AutoCreateWithoutProjectInfo_Throws()
    {
        var bomPath = Path.Combine(_tempDir, "bom.xml");
        File.WriteAllText(bomPath, "<bom/>");

        var settings = new AppSettings
        {
            DTrackUrl = "https://dtrack.example.com",
            ApiKey = "key",
            AutoCreate = true,
            BomFilePath = bomPath
        };

        var ex = Assert.Throws<ArgumentException>(() => settings.Validate());
        Assert.Contains("auto-create", ex.Message);
    }

    [Fact]
    public void Validate_NoProjectIdAndNoProjectInfo_Throws()
    {
        var bomPath = Path.Combine(_tempDir, "bom.xml");
        File.WriteAllText(bomPath, "<bom/>");

        var settings = new AppSettings
        {
            DTrackUrl = "https://dtrack.example.com",
            ApiKey = "key",
            BomFilePath = bomPath
        };

        var ex = Assert.Throws<ArgumentException>(() => settings.Validate());
        Assert.Contains("project name and version are required", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Validate_InvalidThresholdAction_Throws()
    {
        var bomPath = Path.Combine(_tempDir, "bom.xml");
        File.WriteAllText(bomPath, "<bom/>");

        var settings = new AppSettings
        {
            DTrackUrl = "https://dtrack.example.com",
            ApiKey = "key",
            ProjectName = "proj",
            ProjectVersion = "1.0",
            BomFilePath = bomPath,
            ThresholdAction = "invalid"
        };

        var ex = Assert.Throws<ArgumentException>(() => settings.Validate());
        Assert.Contains("threshold action", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Validate_ValidSettings_DoesNotThrow()
    {
        var bomPath = Path.Combine(_tempDir, "bom.xml");
        File.WriteAllText(bomPath, "<bom/>");

        var settings = new AppSettings
        {
            DTrackUrl = "https://dtrack.example.com",
            ApiKey = "key",
            ProjectName = "proj",
            ProjectVersion = "1.0",
            BomFilePath = bomPath,
            ThresholdAction = "warn"
        };

        settings.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_ProjectIdWithoutNameVersion_DoesNotThrow()
    {
        var bomPath = Path.Combine(_tempDir, "bom.xml");
        File.WriteAllText(bomPath, "<bom/>");

        var settings = new AppSettings
        {
            DTrackUrl = "https://dtrack.example.com",
            ApiKey = "key",
            ProjectId = "some-uuid",
            BomFilePath = bomPath
        };

        settings.Validate(); // Should not throw
    }

    [Fact]
    public void LoadFromYaml_WithParentProjectSettings_ReturnsSettings()
    {
        var yamlContent = @"
dtrack-url: https://dtrack.example.com
api-key: test-key
project-name: child-project
project-version: '1.0.0'
bom-file-path: /path/to/bom.xml
auto-create: true
parent-project-name: parent-project
parent-project-version: '2.0.0'
";
        var filePath = Path.Combine(_tempDir, "config.yaml");
        File.WriteAllText(filePath, yamlContent);

        var settings = ConfigurationLoader.LoadFromYaml(filePath);

        Assert.Equal("parent-project", settings.ParentProjectName);
        Assert.Equal("2.0.0", settings.ParentProjectVersion);
    }

    [Fact]
    public void LoadFromYaml_AllThresholdSettings_ReturnsCorrectValues()
    {
        var yamlContent = @"
dtrack-url: https://dtrack.example.com
api-key: test-key
project-name: proj
project-version: '1.0.0'
bom-file-path: /path/to/bom.xml
threshold-action: error
threshold-critical: 0
threshold-high: 5
threshold-medium: 10
threshold-low: 20
threshold-unassigned: 3
threshold-policy-violations-fail: 0
threshold-policy-violations-warn: 2
threshold-policy-violations-info: 5
threshold-policy-violations-total: 10
";
        var filePath = Path.Combine(_tempDir, "config.yaml");
        File.WriteAllText(filePath, yamlContent);

        var settings = ConfigurationLoader.LoadFromYaml(filePath);

        Assert.Equal("error", settings.ThresholdAction);
        Assert.Equal(0, settings.ThresholdCritical);
        Assert.Equal(5, settings.ThresholdHigh);
        Assert.Equal(10, settings.ThresholdMedium);
        Assert.Equal(20, settings.ThresholdLow);
        Assert.Equal(3, settings.ThresholdUnassigned);
        Assert.Equal(0, settings.ThresholdPolicyViolationsFail);
        Assert.Equal(2, settings.ThresholdPolicyViolationsWarn);
        Assert.Equal(5, settings.ThresholdPolicyViolationsInfo);
        Assert.Equal(10, settings.ThresholdPolicyViolationsTotal);
    }
}
