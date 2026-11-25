using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core.Entities;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Compliance;

public sealed class ComplianceService(MainDbContext main, TecNMDbContext tutoring)
{
    public async Task<ComplianceSummaryPage?> GetSummaryAsync(
        ComplianceSummaryQuery query, CancellationToken cancellationToken)
    {
        if (!IsValid(query))
            return null;

        var teachers = main.Teachers.Where(teacher => teacher.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            teachers = teachers.Where(teacher => teacher.Key.ToLower().Contains(term)
                || (teacher.FirstName + " " + teacher.LastNames).ToLower().Contains(term));
        }

        var total = await teachers.CountAsync(cancellationToken);
        var page = await teachers.OrderBy(teacher => teacher.Key)
            .Skip((int)Offset(query)).Take(query.Limit)
            .Select(teacher => new { teacher.Key, Name = teacher.FirstName + " " + teacher.LastNames })
            .ToListAsync(cancellationToken);

        if (page.Count == 0)
            return new ComplianceSummaryPage([], query.Page, query.Limit, total);

        var keys = page.Select(teacher => teacher.Key).ToArray();
        var counts = await FilterDeliverables(query).Where(item => keys.Contains(item.TeacherKey))
            .GroupBy(item => item.TeacherKey)
            .Select(group => new
            {
                Key = group.Key,
                Total = group.Count(),
                Pending = group.Count(item => item.Status == "Pending"),
                OnTime = group.Count(item => item.Status == "OnTime"),
                Late = group.Count(item => item.Status == "Late"),
                Delay = group.Sum(item => item.DelayDays)
            })
            .ToDictionaryAsync(item => item.Key, cancellationToken);

        var rows = page.Select(teacher =>
        {
            var count = counts.GetValueOrDefault(teacher.Key);
            return new TeacherComplianceSummary(teacher.Key, teacher.Name, count?.Total ?? 0,
                count?.Pending ?? 0, count?.OnTime ?? 0, count?.Late ?? 0, count?.Delay ?? 0);
        }).ToArray();

        return new ComplianceSummaryPage(rows, query.Page, query.Limit, total);
    }

    public async Task<DeliverableListPage?> GetDeliverablesAsync(
        string teacherKey, ComplianceSummaryQuery query, CancellationToken cancellationToken)
    {
        if (!IsValid(query))
            return null;

        var deliverables = FilterDeliverables(query).Where(item => item.TeacherKey == teacherKey);
        var total = await deliverables.CountAsync(cancellationToken);
        var page = await deliverables.OrderBy(item => item.ReportingMonth)
            .ThenBy(item => item.ReportType).ThenBy(item => item.Id)
            .Skip((int)Offset(query)).Take(query.Limit)
            .Select(item => new DeliverableListItem(item.Id, item.TeacherKey, item.PeriodId,
                item.ReportType == "Asistencia" ? "SeguimientoMensual"
                    : item.ReportType == "Anexo19Final" ? "SeguimientoFinal" : item.ReportType,
                item.ReportingMonth, item.Status, item.DelayDays, item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new DeliverableListPage(page, query.Page, query.Limit, total);
    }

    public async Task<PlanTask?> GetPlanAsync(Guid id, string? teacherKey, CancellationToken cancellationToken)
    {
        var item = await FindPlan(id, teacherKey).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return item is null ? null : await ToPlanTaskAsync(item, cancellationToken);
    }

    public async Task<PlanSaveResult> SavePlanAsync(Guid id, string teacherKey, string email,
        string ip, SavePlanRequest request, CancellationToken cancellationToken)
    {
        var item = await FindPlan(id, teacherKey).SingleOrDefaultAsync(cancellationToken);
        if (item is null)
            return new(PlanSaveOutcome.NotFound);

        if (request.TabKey is not null || !IsValidPlan(request.FormPayload, request.Submit))
            return new(PlanSaveOutcome.Invalid);

        if (item.Status != "Pending")
        {
            if (request.Submit && JsonElement.DeepEquals(item.FormPayload, request.FormPayload))
                return new(PlanSaveOutcome.Saved, await ToPlanTaskAsync(item, cancellationToken));
            return new(PlanSaveOutcome.Conflict);
        }

        if (!request.Submit && JsonElement.DeepEquals(item.FormPayload, request.FormPayload))
            return new(PlanSaveOutcome.Saved, await ToPlanTaskAsync(item, cancellationToken));

        item.FormPayload = request.FormPayload;
        item.UpdatedBy = email;
        item.UpdatedIp = ip;
        if (request.Submit)
        {
            item.Status = "OnTime";
            item.DelayDays = 0;
        }

        try
        {
            await tutoring.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var current = await FindPlan(id, teacherKey).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (current is null)
                return new(PlanSaveOutcome.NotFound);

            var expectedStatus = request.Submit ? "OnTime" : "Pending";
            if (current.Status != expectedStatus || !JsonElement.DeepEquals(current.FormPayload, request.FormPayload))
                return new(PlanSaveOutcome.Conflict);
            item = current;
        }

        return new(PlanSaveOutcome.Saved, await ToPlanTaskAsync(item, cancellationToken));
    }

    private IQueryable<TutoringDeliverable> FindPlan(Guid id, string? teacherKey) =>
        tutoring.Deliverables.Where(item => item.Id == id && !item.IsDeleted
            && item.ReportType == "PlanTrabajo" && (teacherKey == null || item.TeacherKey == teacherKey));

    private async Task<PlanTask> ToPlanTaskAsync(TutoringDeliverable item, CancellationToken cancellationToken)
    {
        var teacherName = await main.Teachers.Where(teacher => teacher.Key == item.TeacherKey)
            .Select(teacher => teacher.FirstName + " " + teacher.LastNames)
            .SingleOrDefaultAsync(cancellationToken);
        var prefilledData = new Dictionary<string, string>();
        if (teacherName is not null)
            prefilledData["docente_nombre"] = teacherName;

        return new PlanTask(item.Id, item.TeacherKey, item.PeriodId, item.ReportingMonth, item.Status,
            item.DelayDays, item.UpdatedAt, prefilledData, item.FormPayload);
    }

    private static bool IsValidPlan(JsonElement payload, bool submit)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return false;

        string[] textFields = ["semesterLabel", "groupSemester", "career", "shift", "generalObjective",
            "monthlyActivitiesNotes", "tutorName", "signatureMode", "signatureDataUrl"];
        string[] confirmationFields = ["monthlyActivitiesConfirmed", "tutorSignatureConfirmed"];
        if (textFields.Any(key => payload.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.String)
            || confirmationFields.Any(key => payload.TryGetProperty(key, out var value)
                && value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)))
        {
            return false;
        }

        if (!payload.TryGetProperty("sessions", out var sessions))
            return !submit;
        if (sessions.ValueKind != JsonValueKind.Array || sessions.GetArrayLength() != 16)
            return false;

        string[] sessionFields = ["title", "objective", "strategicActivities"];
        foreach (var session in sessions.EnumerateArray())
        {
            if (session.ValueKind != JsonValueKind.Object
                || (session.TryGetProperty("dateRange", out var date) && date.ValueKind != JsonValueKind.String)
                || sessionFields.Any(key => session.TryGetProperty(key, out var value)
                    ? value.ValueKind != JsonValueKind.String || (submit && string.IsNullOrWhiteSpace(value.GetString()))
                    : submit))
                return false;
        }

        string[] requiredFields = ["semesterLabel", "groupSemester", "career", "generalObjective"];
        return !submit || (requiredFields.All(key => payload.TryGetProperty(key, out var value)
            && !string.IsNullOrWhiteSpace(value.GetString()))
            && confirmationFields.All(key => payload.TryGetProperty(key, out var value)
                && value.ValueKind == JsonValueKind.True));
    }

    private IQueryable<TutoringDeliverable> FilterDeliverables(ComplianceSummaryQuery query)
    {
        var deliverables = tutoring.Deliverables.AsNoTracking().Where(item =>
            !item.IsDeleted && item.PeriodId == query.PeriodoId);

        if (query.Month is { } month)
            deliverables = deliverables.Where(item => item.ReportingMonth == month);

        if (ToReportType(query.FormatKey) is { } reportType)
            deliverables = deliverables.Where(item => item.ReportType == reportType);

        if (query.Status is { } status)
            deliverables = deliverables.Where(item => item.Status == status);

        return deliverables;
    }

    private static long Offset(ComplianceSummaryQuery query) => (query.Page - 1L) * query.Limit;

    private static bool IsValid(ComplianceSummaryQuery query) =>
        query.PeriodoId > 0 && query.Page > 0 && query.Limit is >= 1 and <= 100
        && Offset(query) <= int.MaxValue && query.Month is null or (>= 1 and <= 12)
        && (query.FormatKey is null || ToReportType(query.FormatKey) is not null)
        && query.Status is null or "Pending" or "OnTime" or "Late";

    private static string? ToReportType(string? formatKey) => formatKey switch
    {
        "SeguimientoMensual" => "Asistencia",
        "SeguimientoFinal" => "Anexo19Final",
        "PlanTrabajo" or "DiagnosticoInicial" or "Anexo14" or "Anexo15" or "Anexo19AMensual"
            => formatKey,
        _ => null
    };
}
