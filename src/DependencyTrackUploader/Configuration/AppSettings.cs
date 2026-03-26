using YamlDotNet.Serialization;

namespace DependencyTrackUploader.Configuration;

public class AppSettings
{
    // Connection settings
    [YamlMember(Alias = "dtrack-url")]
    public string? DTrackUrl { get; set; }

    [YamlMember(Alias = "api-key")]
    public string? ApiKey { get; set; }

    // Project settings
    [YamlMember(Alias = "project-id")]
    public string? ProjectId { get; set; }

    [YamlMember(Alias = "project-name")]
    public string? ProjectName { get; set; }

    [YamlMember(Alias = "project-version")]
    public string? ProjectVersion { get; set; }

    [YamlMember(Alias = "project-description")]
    public string? ProjectDescription { get; set; }

    [YamlMember(Alias = "project-classifier")]
    public string? ProjectClassifier { get; set; }

    [YamlMember(Alias = "project-swid-tag-id")]
    public string? ProjectSwidTagId { get; set; }

    [YamlMember(Alias = "project-group")]
    public string? ProjectGroup { get; set; }

    [YamlMember(Alias = "project-tags")]
    public List<string>? ProjectTags { get; set; }

    [YamlMember(Alias = "auto-create")]
    public bool? AutoCreate { get; set; }

    [YamlMember(Alias = "is-latest")]
    public bool? IsLatest { get; set; }

    // Parent project settings
    [YamlMember(Alias = "parent-project-name")]
    public string? ParentProjectName { get; set; }

    [YamlMember(Alias = "parent-project-version")]
    public string? ParentProjectVersion { get; set; }

    // BOM file
    [YamlMember(Alias = "bom-file-path")]
    public string? BomFilePath { get; set; }

    // SSL settings
    [YamlMember(Alias = "ca-file-path")]
    public string? CaFilePath { get; set; }

    // Threshold settings
    [YamlMember(Alias = "threshold-action")]
    public string? ThresholdAction { get; set; }

    [YamlMember(Alias = "threshold-critical")]
    public int? ThresholdCritical { get; set; }

    [YamlMember(Alias = "threshold-high")]
    public int? ThresholdHigh { get; set; }

    [YamlMember(Alias = "threshold-medium")]
    public int? ThresholdMedium { get; set; }

    [YamlMember(Alias = "threshold-low")]
    public int? ThresholdLow { get; set; }

    [YamlMember(Alias = "threshold-unassigned")]
    public int? ThresholdUnassigned { get; set; }

    [YamlMember(Alias = "threshold-policy-violations-fail")]
    public int? ThresholdPolicyViolationsFail { get; set; }

    [YamlMember(Alias = "threshold-policy-violations-warn")]
    public int? ThresholdPolicyViolationsWarn { get; set; }

    [YamlMember(Alias = "threshold-policy-violations-info")]
    public int? ThresholdPolicyViolationsInfo { get; set; }

    [YamlMember(Alias = "threshold-policy-violations-total")]
    public int? ThresholdPolicyViolationsTotal { get; set; }

    public void MergeFrom(AppSettings other)
    {
        if (other.DTrackUrl != null) DTrackUrl = other.DTrackUrl;
        if (other.ApiKey != null) ApiKey = other.ApiKey;
        if (other.ProjectId != null) ProjectId = other.ProjectId;
        if (other.ProjectName != null) ProjectName = other.ProjectName;
        if (other.ProjectVersion != null) ProjectVersion = other.ProjectVersion;
        if (other.ProjectDescription != null) ProjectDescription = other.ProjectDescription;
        if (other.ProjectClassifier != null) ProjectClassifier = other.ProjectClassifier;
        if (other.ProjectSwidTagId != null) ProjectSwidTagId = other.ProjectSwidTagId;
        if (other.ProjectGroup != null) ProjectGroup = other.ProjectGroup;
        if (other.ProjectTags != null) ProjectTags = other.ProjectTags;
        if (other.AutoCreate != null) AutoCreate = other.AutoCreate;
        if (other.IsLatest != null) IsLatest = other.IsLatest;
        if (other.ParentProjectName != null) ParentProjectName = other.ParentProjectName;
        if (other.ParentProjectVersion != null) ParentProjectVersion = other.ParentProjectVersion;
        if (other.BomFilePath != null) BomFilePath = other.BomFilePath;
        if (other.CaFilePath != null) CaFilePath = other.CaFilePath;
        if (other.ThresholdAction != null) ThresholdAction = other.ThresholdAction;
        if (other.ThresholdCritical != null) ThresholdCritical = other.ThresholdCritical;
        if (other.ThresholdHigh != null) ThresholdHigh = other.ThresholdHigh;
        if (other.ThresholdMedium != null) ThresholdMedium = other.ThresholdMedium;
        if (other.ThresholdLow != null) ThresholdLow = other.ThresholdLow;
        if (other.ThresholdUnassigned != null) ThresholdUnassigned = other.ThresholdUnassigned;
        if (other.ThresholdPolicyViolationsFail != null) ThresholdPolicyViolationsFail = other.ThresholdPolicyViolationsFail;
        if (other.ThresholdPolicyViolationsWarn != null) ThresholdPolicyViolationsWarn = other.ThresholdPolicyViolationsWarn;
        if (other.ThresholdPolicyViolationsInfo != null) ThresholdPolicyViolationsInfo = other.ThresholdPolicyViolationsInfo;
        if (other.ThresholdPolicyViolationsTotal != null) ThresholdPolicyViolationsTotal = other.ThresholdPolicyViolationsTotal;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BomFilePath))
            throw new ArgumentException("BOM file path is required.");

        if (string.IsNullOrWhiteSpace(DTrackUrl))
            throw new ArgumentException("Dependency Track URL is required.");

        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ArgumentException("API key is required.");

        if (AutoCreate == true)
        {
            if (string.IsNullOrWhiteSpace(ProjectName) || string.IsNullOrWhiteSpace(ProjectVersion))
                throw new ArgumentException("Project name and version are required when auto-create is enabled.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ProjectId) &&
                (string.IsNullOrWhiteSpace(ProjectName) || string.IsNullOrWhiteSpace(ProjectVersion)))
                throw new ArgumentException("Project name and version are required when project ID is not specified.");
        }

        if (!File.Exists(BomFilePath))
            throw new FileNotFoundException($"BOM file not found: {BomFilePath}");

        if (!string.IsNullOrWhiteSpace(CaFilePath) && !File.Exists(CaFilePath))
            throw new FileNotFoundException($"CA certificate file not found: {CaFilePath}");

        var validActions = new[] { "none", "warn", "error" };
        var action = ThresholdAction ?? "none";
        if (!validActions.Contains(action))
            throw new ArgumentException($"Invalid threshold action '{action}'. Must be one of: {string.Join(", ", validActions)}");
    }
}
