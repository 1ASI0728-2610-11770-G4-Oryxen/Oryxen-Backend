using Oryxen.Domain.Common;

namespace Oryxen.Domain.Entities;

/// <summary>
/// Represents a generated analytical report for a plant owned by a user.
/// Belongs to the Analysis &amp; Reporting bounded context.
/// </summary>
public class AnalysisReport : AuditableEntity
{
    public Guid UserAccountId { get; set; }

    public Guid PlantId { get; set; }

    public ReportType Type { get; set; } = ReportType.HealthSummary;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTime RangeStart { get; set; }

    public DateTime RangeEnd { get; set; }

    public ExportFormat Format { get; set; } = ExportFormat.Csv;

    public string? FileContent { get; set; }

    public DateTime? GeneratedAt { get; set; }
}

public enum ReportType
{
    HealthSummary = 1,
    TelemetryDetail = 2,
    PredictiveAlert = 3
}

public enum ReportStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

public enum ExportFormat
{
    Csv = 1,
    Json = 2
}
