using System.Text.Json;

namespace TecNM.Api.Core.Entities;

public sealed class TutoringDeliverable
{
    public Guid Id { get; init; }
    public required string TeacherKey { get; init; }
    public int PeriodId { get; init; }
    public required string ReportType { get; init; }
    public short ReportingMonth { get; init; }
    public required string Status { get; set; }
    public int DelayDays { get; set; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; set; }
    public string? UpdatedIp { get; set; }
    public JsonElement FormPayload { get; set; }
    public uint Version { get; set; }
    public bool IsDeleted { get; init; }
}
