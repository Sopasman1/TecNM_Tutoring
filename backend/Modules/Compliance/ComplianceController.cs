using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Compliance;

[ApiController]
[Authorize(Roles = "Admin,User")]
[Route("api/compliance")]
public sealed class ComplianceController(ComplianceService service) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet("summary")]
    public async Task<ActionResult<ComplianceSummaryPage>> GetSummary(
        [FromQuery] ComplianceSummaryQuery query, CancellationToken cancellationToken)
    {
        var result = await service.GetSummaryAsync(query, cancellationToken);
        return result is null
            ? BadRequest(new { message = "Los filtros de cumplimiento no son válidos." })
            : Ok(result);
    }

    [HttpGet("{docenteClave}/{periodoId:int}")]
    public async Task<ActionResult<DeliverableListPage>> GetDeliverables(
        string docenteClave, int periodoId, [FromQuery] ComplianceSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var currentTeacher = User.FindFirst("docente_clave")?.Value;
        var teacherKey = docenteClave == "me" ? currentTeacher : docenteClave.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(teacherKey)
            || (!User.IsInRole("Admin") && teacherKey != currentTeacher))
        {
            return Forbid();
        }

        var result = await service.GetDeliverablesAsync(
            teacherKey, query with { PeriodoId = periodoId }, cancellationToken);
        return result is null
            ? BadRequest(new { message = "Los filtros de cumplimiento no son válidos." })
            : Ok(result);
    }

    [HttpGet("tasks/{id:guid}")]
    public async Task<ActionResult<PlanTask>> GetTask(Guid id, CancellationToken cancellationToken)
    {
        var teacherKey = User.FindFirst("docente_clave")?.Value;
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && string.IsNullOrWhiteSpace(teacherKey))
            return Forbid();

        var result = await service.GetPlanAsync(id, isAdmin ? null : teacherKey, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = "User")]
    [HttpPut("tasks/{id:guid}/form")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<PlanTask>> SaveTask(
        Guid id, SavePlanRequest request, CancellationToken cancellationToken)
    {
        var teacherKey = User.FindFirst("docente_clave")?.Value;
        var email = User.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(teacherKey) || string.IsNullOrWhiteSpace(email))
            return Forbid();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (ip is null)
            return BadRequest(new { message = "No se pudo identificar el origen de la solicitud." });

        var result = await service.SavePlanAsync(id, teacherKey, email, ip, request, cancellationToken);
        return result.Outcome switch
        {
            PlanSaveOutcome.Saved => Ok(result.Task),
            PlanSaveOutcome.Invalid => UnprocessableEntity(new { message = "Revisa los campos del Plan de Trabajo antes de guardar o entregar." }),
            PlanSaveOutcome.Conflict => Conflict(new { message = "El Plan de Trabajo ya fue entregado o cambió. Recarga el formulario." }),
            _ => NotFound()
        };
    }
}
