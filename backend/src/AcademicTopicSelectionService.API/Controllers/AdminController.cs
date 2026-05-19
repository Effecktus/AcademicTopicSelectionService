using System.Text;
using Asp.Versioning;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Admin;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Эндпоинты панели администратора: аналитика и экспорт данных.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminController(IAdminRepository repo) : ControllerBase
{
    /// <summary>
    /// Сводная аналитика: заявки по статусам, ВКР по годам, заявки по кафедрам.
    /// </summary>
    [ProducesResponseType(typeof(AdminAnalyticsDto), StatusCodes.Status200OK)]
    [HttpGet("analytics")]
    public async Task<ActionResult<AdminAnalyticsDto>> GetAnalyticsAsync(CancellationToken ct = default)
    {
        var result = await repo.GetAnalyticsAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Экспорт данных. <c>format</c>: <c>excel</c> (xlsx, 3 листа) или <c>csv</c> (один датасет).
    /// <c>dataset</c> для csv: <c>graduate-works</c> | <c>applications</c> | <c>users</c>.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportAsync(
        [FromQuery] string format = "excel",
        [FromQuery] string dataset = "graduate-works",
        CancellationToken ct = default)
    {
        if (format.Equals("excel", StringComparison.OrdinalIgnoreCase))
            return await ExportExcelAsync(ct);

        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            return await ExportCsvAsync(dataset, ct);

        return BadRequest("format must be 'excel' or 'csv'");
    }

    private async Task<IActionResult> ExportExcelAsync(CancellationToken ct)
    {
        var gw = await repo.GetGwExportAsync(ct);
        var apps = await repo.GetApplicationsExportAsync(ct);
        var users = await repo.GetUsersExportAsync(ct);

        using var wb = new XLWorkbook();

        var wsGw = wb.AddWorksheet("Архив ВКР");
        wsGw.Cell(1, 1).InsertData(new[]
        {
            new[] { "Тема", "Студент", "Преподаватель", "Год", "Оценка", "Комиссия", "Есть ВКР", "Есть презентация", "Дата создания" }
        });
        for (var i = 0; i < gw.Count; i++)
        {
            var r = gw[i];
            var row = i + 2;
            wsGw.Cell(row, 1).Value = r.Title;
            wsGw.Cell(row, 2).Value = r.StudentFullName;
            wsGw.Cell(row, 3).Value = r.TeacherFullName;
            wsGw.Cell(row, 4).Value = r.Year;
            wsGw.Cell(row, 5).Value = r.Grade;
            wsGw.Cell(row, 6).Value = r.CommissionMembers;
            wsGw.Cell(row, 7).Value = r.HasThesis ? "Да" : "Нет";
            wsGw.Cell(row, 8).Value = r.HasPresentation ? "Да" : "Нет";
            wsGw.Cell(row, 9).Value = r.CreatedAt.ToString("yyyy-MM-dd");
        }
        wsGw.Row(1).Style.Font.Bold = true;
        wsGw.Columns().AdjustToContents();

        var wsApps = wb.AddWorksheet("Заявки");
        wsApps.Cell(1, 1).InsertData(new[]
        {
            new[] { "Тема", "Студент", "Группа", "Научрук", "Статус", "Дата создания" }
        });
        for (var i = 0; i < apps.Count; i++)
        {
            var r = apps[i];
            var row = i + 2;
            wsApps.Cell(row, 1).Value = r.TopicTitle;
            wsApps.Cell(row, 2).Value = r.StudentFullName;
            wsApps.Cell(row, 3).Value = r.StudentGroup;
            wsApps.Cell(row, 4).Value = r.SupervisorFullName;
            wsApps.Cell(row, 5).Value = r.StatusDisplayName;
            wsApps.Cell(row, 6).Value = r.CreatedAt.ToString("yyyy-MM-dd");
        }
        wsApps.Row(1).Style.Font.Bold = true;
        wsApps.Columns().AdjustToContents();

        var wsUsers = wb.AddWorksheet("Пользователи");
        wsUsers.Cell(1, 1).InsertData(new[]
        {
            new[] { "Email", "Фамилия", "Имя", "Отчество", "Роль", "Кафедра", "Активен", "Дата создания" }
        });
        for (var i = 0; i < users.Count; i++)
        {
            var r = users[i];
            var row = i + 2;
            wsUsers.Cell(row, 1).Value = r.Email;
            wsUsers.Cell(row, 2).Value = r.LastName;
            wsUsers.Cell(row, 3).Value = r.FirstName;
            wsUsers.Cell(row, 4).Value = r.MiddleName ?? "";
            wsUsers.Cell(row, 5).Value = r.RoleDisplayName;
            wsUsers.Cell(row, 6).Value = r.DepartmentDisplayName ?? "";
            wsUsers.Cell(row, 7).Value = r.IsActive ? "Да" : "Нет";
            wsUsers.Cell(row, 8).Value = r.CreatedAt.ToString("yyyy-MM-dd");
        }
        wsUsers.Row(1).Style.Font.Bold = true;
        wsUsers.Columns().AdjustToContents();

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var fileName = $"export_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private async Task<IActionResult> ExportCsvAsync(string dataset, CancellationToken ct)
    {
        var sb = new StringBuilder();
        string fileName;

        switch (dataset.ToLowerInvariant())
        {
            case "graduate-works":
            {
                var rows = await repo.GetGwExportAsync(ct);
                sb.AppendLine("Тема;Студент;Преподаватель;Год;Оценка;Комиссия;Есть ВКР;Есть презентация;Дата создания");
                foreach (var r in rows)
                    sb.AppendLine($"{Esc(r.Title)};{Esc(r.StudentFullName)};{Esc(r.TeacherFullName)};{r.Year};{r.Grade};{Esc(r.CommissionMembers)};{(r.HasThesis ? "Да" : "Нет")};{(r.HasPresentation ? "Да" : "Нет")};{r.CreatedAt:yyyy-MM-dd}");
                fileName = $"graduate-works_{DateTime.UtcNow:yyyyMMdd}.csv";
                break;
            }
            case "applications":
            {
                var rows = await repo.GetApplicationsExportAsync(ct);
                sb.AppendLine("Тема;Студент;Группа;Научрук;Статус;Дата создания");
                foreach (var r in rows)
                    sb.AppendLine($"{Esc(r.TopicTitle)};{Esc(r.StudentFullName)};{Esc(r.StudentGroup)};{Esc(r.SupervisorFullName)};{Esc(r.StatusDisplayName)};{r.CreatedAt:yyyy-MM-dd}");
                fileName = $"applications_{DateTime.UtcNow:yyyyMMdd}.csv";
                break;
            }
            case "users":
            {
                var rows = await repo.GetUsersExportAsync(ct);
                sb.AppendLine("Email;Фамилия;Имя;Отчество;Роль;Кафедра;Активен;Дата создания");
                foreach (var r in rows)
                    sb.AppendLine($"{Esc(r.Email)};{Esc(r.LastName)};{Esc(r.FirstName)};{Esc(r.MiddleName)};{Esc(r.RoleDisplayName)};{Esc(r.DepartmentDisplayName)};{(r.IsActive ? "Да" : "Нет")};{r.CreatedAt:yyyy-MM-dd}");
                fileName = $"users_{DateTime.UtcNow:yyyyMMdd}.csv";
                break;
            }
            default:
                return BadRequest("dataset must be 'graduate-works', 'applications' or 'users'");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static string Esc(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var escaped = value.Replace("\"", "\"\"");
        return escaped.Contains(';') || escaped.Contains('"') || escaped.Contains('\n')
            ? $"\"{escaped}\""
            : escaped;
    }
}
