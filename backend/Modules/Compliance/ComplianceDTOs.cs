using System.Text.Json;

namespace TecNM.Api.Modules.Compliance;

public sealed record ComplianceSummaryQuery(
    int PeriodoId, int Page = 1, int Limit = 20, short? Month = null,
    string? FormatKey = null, string? Status = null, string? Search = null);

public sealed record TeacherComplianceSummary(
    string DocenteClave, string DocenteName, int TotalDeliverables,
    int Pending, int OnTime, int Late, int TotalDelayDays);

public sealed record ComplianceSummaryPage(
    IReadOnlyList<TeacherComplianceSummary> Data, int Page, int Limit, int Total);

public sealed record DeliverableListItem(
    Guid Id, string DocenteClave, int PeriodoId, string FormatKey, short ReportingMonth,
    string Status, int DelayDays, DateTimeOffset? UpdatedAt);

public sealed record DeliverableListPage(
    IReadOnlyList<DeliverableListItem> Data, int Page, int Limit, int Total);

public sealed record SavePlanRequest(JsonElement FormPayload, bool Submit, string? TabKey);

public sealed record PlanTask(
    Guid Id, string DocenteClave, int PeriodoId, short ReportingMonth, string Status,
    int DelayDays, DateTimeOffset? UpdatedAt, IReadOnlyDictionary<string, string> PrefilledData,
    JsonElement FormPayload)
{
    public string FormatKey => "PlanTrabajo";
    public string Title => "Plan de trabajo del tutor";
    public bool HasTabs => false;
    public bool RequiresPhotoEvidence => false;
}

public enum PlanSaveOutcome { Saved, NotFound, Invalid, Conflict }

public sealed record PlanSaveResult(PlanSaveOutcome Outcome, PlanTask? Task = null);
