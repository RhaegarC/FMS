using System.Globalization;
using System.Text;
using System.Text.Json;
using Fms.Api.Contracts;
using Fms.Interface.Repository;
using Fms.Interface.Service;
using Fms.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fms.Api.Controllers;

/// <summary>
/// Submission APIs (feature 07). Submissions are schema-validated against the form's
/// definition before storage. Listing and export are scoped by role: admins see every
/// submission; non-admins see only their own on forms they can access (feature-05
/// evaluator, default deny). Keyword search matches the jsonb text representation;
/// form/date filters narrow the set. Excel export flattens nested data.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class SubmissionController(
    ISubmissionService submissions, ISubmissionExcelExporter exporter) : FmsApiControllerBase
{
    [HttpPost("forms/{formId:int}/submissions")]
    public async Task<ActionResult<SubmissionDto>> Submit(
        int formId, [FromBody] SubmitSubmissionRequest request)
    {
        // A missing/empty `data` body (JsonElement Undefined/Null) is rejected here — the
        // shape only exists at the HTTP boundary; the service works with raw JSON text.
        if (request.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest(new ApiError("Submission data is required."));
        }

        var user = CurrentUser;
        var submission = await submissions.SubmitAsync(formId, user, request.Data.GetRawText());
        // The submit response carries the caller's email (the submitting user).
        var dto = new SubmissionDto(submission.Id, submission.FormId, submission.UserId,
            user.Email, submission.Data, submission.CreatedOn);
        return Created($"/api/forms/{formId}/submissions/{submission.Id}", dto);
    }

    [HttpGet("me/submissions")]
    public async Task<ActionResult<IEnumerable<SubmissionDto>>> ListMy(
        [FromQuery] int? formId, [FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? q)
    {
        var list = await submissions.ListMyAsync(CurrentUser, ParseSubmissionQuery(formId, from, to, q));
        return Ok(list.Select(DtoMapper.ToDto));
    }

    [HttpGet("submissions")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<SubmissionDto>>> ListAll(
        [FromQuery] int? formId, [FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? q)
    {
        var list = await submissions.ListAllAsync(ParseSubmissionQuery(formId, from, to, q));
        return Ok(list.Select(DtoMapper.ToDto));
    }

    [HttpGet("submissions/export")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportAll(
        [FromQuery] string? format, [FromQuery] int? formId,
        [FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? q)
    {
        var list = await submissions.ListAllAsync(ParseSubmissionQuery(formId, from, to, q));
        return Export(list, format);
    }

    [HttpGet("me/submissions/export")]
    public async Task<IActionResult> ExportMy(
        [FromQuery] string? format, [FromQuery] int? formId,
        [FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? q)
    {
        var list = await submissions.ListMyAsync(CurrentUser, ParseSubmissionQuery(formId, from, to, q));
        return Export(list, format);
    }

    // --- Local helpers ------------------------------------------------------

    // Builds the submission query contract. Date filters are parsed here (HTTP concern);
    // the parsed UTC bounds go to the repository via the query object.
    private static SubmissionQuery ParseSubmissionQuery(
        int? formId, string? from, string? to, string? keyword) =>
        new(formId,
            ParseDateFilter(from, inclusiveEndOfDay: false),
            ParseDateFilter(to, inclusiveEndOfDay: true),
            keyword);

    // Parses a from/to filter as UTC. A date-only `to` (yyyy-MM-dd) means "through
    // the end of that day", so it becomes an exclusive bound at the next midnight.
    private static DateTimeOffset? ParseDateFilter(string? value, bool inclusiveEndOfDay)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return null;
        }

        if (inclusiveEndOfDay && DateTime.TryParseExact(value, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            parsed = parsed.AddDays(1);
        }

        return parsed;
    }

    // Formats an export response: JSON array download, Excel workbook download, or a
    // 400 for any other format value.
    private IActionResult Export(List<Submission> submissionList, string? format)
    {
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            var json = JsonSerializer.Serialize(submissionList.Select(DtoMapper.ToDto));
            return File(Encoding.UTF8.GetBytes(json), "application/json", "submissions.json");
        }

        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return File(exporter.BuildWorkbook(submissionList),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "submissions.xlsx");
        }

        return BadRequest(new ApiError("Export format must be 'xlsx' or 'json'."));
    }
}
