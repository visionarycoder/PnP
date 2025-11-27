namespace Aspire.DeploymentStrategies;

public record ComplianceRequirements(
    List<string> Standards, // SOC2, HIPAA, GDPR, etc.
    bool RequiresApprovalGates,
    bool RequiresChangeControl,
    TimeSpan MinimumReviewPeriod);