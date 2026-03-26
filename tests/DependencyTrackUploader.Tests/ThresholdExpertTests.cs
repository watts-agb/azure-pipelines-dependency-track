using DependencyTrackUploader.Models;
using DependencyTrackUploader.Services;

namespace DependencyTrackUploader.Tests;

public class ThresholdExpertTests
{
    [Fact]
    public void AreThresholdsConfigured_AllDisabled_ReturnsFalse()
    {
        var expert = new ThresholdExpert();
        Assert.False(expert.AreThresholdsConfigured());
    }

    [Fact]
    public void AreThresholdsConfigured_CriticalSet_ReturnsTrue()
    {
        var expert = new ThresholdExpert(criticalCount: 0);
        Assert.True(expert.AreThresholdsConfigured());
    }

    [Fact]
    public void AreThresholdsConfigured_HighSet_ReturnsTrue()
    {
        var expert = new ThresholdExpert(highCount: 5);
        Assert.True(expert.AreThresholdsConfigured());
    }

    [Fact]
    public void AreThresholdsConfigured_PolicyViolationsTotalSet_ReturnsTrue()
    {
        var expert = new ThresholdExpert(policyViolationsTotal: 10);
        Assert.True(expert.AreThresholdsConfigured());
    }

    [Fact]
    public void ValidateThresholds_AllBelowThreshold_DoesNotThrow()
    {
        var expert = new ThresholdExpert(criticalCount: 5, highCount: 10, mediumCount: 20, lowCount: 30, unassignedCount: 5);
        var metrics = new ProjectMetrics
        {
            Critical = 0, High = 5, Medium = 10, Low = 15, Unassigned = 2
        };

        expert.ValidateThresholds(metrics);
    }

    [Fact]
    public void ValidateThresholds_CriticalExceeded_Throws()
    {
        var expert = new ThresholdExpert(criticalCount: 0);
        var metrics = new ProjectMetrics { Critical = 1 };

        var ex = Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
        Assert.Contains("Critical", ex.Message);
    }

    [Fact]
    public void ValidateThresholds_HighExceeded_Throws()
    {
        var expert = new ThresholdExpert(highCount: 5);
        var metrics = new ProjectMetrics { High = 10 };

        var ex = Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
        Assert.Contains("High", ex.Message);
    }

    [Fact]
    public void ValidateThresholds_MediumExceeded_Throws()
    {
        var expert = new ThresholdExpert(mediumCount: 3);
        var metrics = new ProjectMetrics { Medium = 5 };

        var ex = Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
        Assert.Contains("Medium", ex.Message);
    }

    [Fact]
    public void ValidateThresholds_LowExceeded_Throws()
    {
        var expert = new ThresholdExpert(lowCount: 2);
        var metrics = new ProjectMetrics { Low = 4 };

        var ex = Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
        Assert.Contains("Low", ex.Message);
    }

    [Fact]
    public void ValidateThresholds_UnassignedExceeded_Throws()
    {
        var expert = new ThresholdExpert(unassignedCount: 0);
        var metrics = new ProjectMetrics { Unassigned = 1 };

        var ex = Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
        Assert.Contains("Unassigned", ex.Message);
    }

    [Fact]
    public void ValidateThresholds_PolicyViolationsFailExceeded_Throws()
    {
        var expert = new ThresholdExpert(policyViolationsFail: 0);
        var metrics = new ProjectMetrics { PolicyViolationsFail = 1 };

        var ex = Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
        Assert.Contains("Fail", ex.Message);
    }

    [Fact]
    public void ValidateThresholds_PolicyViolationsWarnExceeded_Throws()
    {
        var expert = new ThresholdExpert(policyViolationsWarn: 2);
        var metrics = new ProjectMetrics { PolicyViolationsWarn = 5 };

        var ex = Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
        Assert.Contains("Warn", ex.Message);
    }

    [Fact]
    public void ValidateThresholds_PolicyViolationsInfoExceeded_Throws()
    {
        var expert = new ThresholdExpert(policyViolationsInfo: 1);
        var metrics = new ProjectMetrics { PolicyViolationsInfo = 3 };

        var ex = Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
        Assert.Contains("Info", ex.Message);
    }

    [Fact]
    public void ValidateThresholds_PolicyViolationsTotalExceeded_Throws()
    {
        var expert = new ThresholdExpert(policyViolationsTotal: 5);
        var metrics = new ProjectMetrics { PolicyViolationsTotal = 10 };

        var ex = Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
        Assert.Contains("Total", ex.Message);
    }

    [Fact]
    public void ValidateThresholds_DisabledThreshold_DoesNotThrow()
    {
        // -1 means disabled, so even if the metric exceeds, it should not throw
        var expert = new ThresholdExpert(criticalCount: -1);
        var metrics = new ProjectMetrics { Critical = 100 };

        expert.ValidateThresholds(metrics);
    }

    [Fact]
    public void ValidateThresholds_ExactlyAtThreshold_DoesNotThrow()
    {
        var expert = new ThresholdExpert(criticalCount: 5);
        var metrics = new ProjectMetrics { Critical = 5 };

        expert.ValidateThresholds(metrics);
    }

    [Fact]
    public void ValidateThresholds_OneAboveThreshold_Throws()
    {
        var expert = new ThresholdExpert(criticalCount: 5);
        var metrics = new ProjectMetrics { Critical = 6 };

        Assert.Throws<ThresholdExceededException>(() => expert.ValidateThresholds(metrics));
    }
}
