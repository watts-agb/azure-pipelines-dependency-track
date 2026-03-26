using DependencyTrackUploader.Models;

namespace DependencyTrackUploader.Services;

public class ThresholdExpert
{
    public int CriticalCount { get; }
    public int HighCount { get; }
    public int MediumCount { get; }
    public int LowCount { get; }
    public int UnassignedCount { get; }
    public int PolicyViolationsFail { get; }
    public int PolicyViolationsWarn { get; }
    public int PolicyViolationsInfo { get; }
    public int PolicyViolationsTotal { get; }

    public ThresholdExpert(
        int criticalCount = -1,
        int highCount = -1,
        int mediumCount = -1,
        int lowCount = -1,
        int unassignedCount = -1,
        int policyViolationsFail = -1,
        int policyViolationsWarn = -1,
        int policyViolationsInfo = -1,
        int policyViolationsTotal = -1)
    {
        CriticalCount = criticalCount;
        HighCount = highCount;
        MediumCount = mediumCount;
        LowCount = lowCount;
        UnassignedCount = unassignedCount;
        PolicyViolationsFail = policyViolationsFail;
        PolicyViolationsWarn = policyViolationsWarn;
        PolicyViolationsInfo = policyViolationsInfo;
        PolicyViolationsTotal = policyViolationsTotal;
    }

    public bool AreThresholdsConfigured()
    {
        return CriticalCount >= 0 ||
               HighCount >= 0 ||
               MediumCount >= 0 ||
               LowCount >= 0 ||
               UnassignedCount >= 0 ||
               PolicyViolationsFail >= 0 ||
               PolicyViolationsWarn >= 0 ||
               PolicyViolationsInfo >= 0 ||
               PolicyViolationsTotal >= 0;
    }

    public void ValidateThresholds(ProjectMetrics metrics)
    {
        if (CriticalCount >= 0 && metrics.Critical > CriticalCount)
            throw new ThresholdExceededException("Critical vulnerability count threshold surpassed.");

        if (HighCount >= 0 && metrics.High > HighCount)
            throw new ThresholdExceededException("High vulnerability count threshold surpassed.");

        if (MediumCount >= 0 && metrics.Medium > MediumCount)
            throw new ThresholdExceededException("Medium vulnerability count threshold surpassed.");

        if (LowCount >= 0 && metrics.Low > LowCount)
            throw new ThresholdExceededException("Low vulnerability count threshold surpassed.");

        if (UnassignedCount >= 0 && metrics.Unassigned > UnassignedCount)
            throw new ThresholdExceededException("Unassigned vulnerability count threshold surpassed.");

        if (PolicyViolationsFail >= 0 && metrics.PolicyViolationsFail > PolicyViolationsFail)
            throw new ThresholdExceededException("Fail policy violation count threshold surpassed.");

        if (PolicyViolationsWarn >= 0 && metrics.PolicyViolationsWarn > PolicyViolationsWarn)
            throw new ThresholdExceededException("Warn policy violation count threshold surpassed.");

        if (PolicyViolationsInfo >= 0 && metrics.PolicyViolationsInfo > PolicyViolationsInfo)
            throw new ThresholdExceededException("Info policy violation count threshold surpassed.");

        if (PolicyViolationsTotal >= 0 && metrics.PolicyViolationsTotal > PolicyViolationsTotal)
            throw new ThresholdExceededException("Total policy violation count threshold surpassed.");
    }
}

public class ThresholdExceededException : Exception
{
    public ThresholdExceededException(string message) : base(message) { }
}
