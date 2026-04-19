using Asp.Versioning;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.GraduateWorks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Архив выпускных квалификационных работ (метаданные и ссылки на файлы в объектном хранилище).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/graduate-works")]
[Produces("application/json")]
[Authorize]
public sealed class GraduateWorksController(IGraduateWorksService service) : ControllerBase
{
    /// <summary>
    /// Список записей архива ВКР с пагинацией.
    /// </summary>
    [ProducesResponseType(typeof(PagedResult<GraduateWorkDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<GraduateWorkDto>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? year = null,
        CancellationToken ct = default)
    {
        var result = await service.GetAllAsync(new ListGraduateWorksQuery(page, pageSize, year), ct);
        return Ok(result);
    }

    /// <summary>
    /// Запись архива по идентификатору.
    /// </summary>
    [ProducesResponseType(typeof(GraduateWorkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GraduateWorkDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await service.GetByIdAsync(id, ct);
        return dto is null
            ? Problem(title: "Not Found", detail: "Graduate work not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString())
            : Ok(dto);
    }

    /// <summary>
    /// Создать запись ВКР по заявке (студент и научрук из заявки).
    /// </summary>
    [ProducesResponseType(typeof(GraduateWorkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<GraduateWorkDto>> CreateAsync(
        [FromBody] CreateGraduateWorkCommand command,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(command, ct);
        return MapCreateResult(result);
    }

    /// <summary>
    /// Обновить метаданные записи ВКР.
    /// </summary>
    [ProducesResponseType(typeof(GraduateWorkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GraduateWorkDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateGraduateWorkBody body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(
            new UpdateGraduateWorkCommand(id, body.Title, body.Year, body.Grade, body.CommissionMembers), ct);
        return MapMutationResult(result);
    }

    /// <summary>
    /// Удалить запись ВКР и связанные объекты в хранилище (если были загружены).
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var result = await service.DeleteAsync(id, ct);
        if (result.Error is not null)
        {
            return result.Error.Value switch
            {
                GraduateWorksError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Получить временную ссылку для загрузки файла (presigned PUT; в dev — заглушка).
    /// </summary>
    [ProducesResponseType(typeof(FileUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/upload-url/{fileType}")]
    public async Task<ActionResult<FileUrlDto>> GetUploadUrlAsync(
        Guid id,
        string fileType,
        CancellationToken ct = default)
    {
        var result = await service.GetUploadUrlAsync(id, fileType, ct);
        return MapUrlResult(result);
    }

    /// <summary>
    /// Подтвердить успешную загрузку файла в хранилище (проверка существования объекта).
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/confirm-upload/{fileType}")]
    public async Task<IActionResult> ConfirmUploadAsync(
        Guid id,
        string fileType,
        [FromBody] ConfirmUploadBody body,
        CancellationToken ct = default)
    {
        var result = await service.ConfirmUploadAsync(id, fileType, body.FileName, ct);
        if (result.Error is not null)
        {
            return result.Error.Value switch
            {
                GraduateWorksError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                GraduateWorksError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Получить временную ссылку на скачивание файла.
    /// </summary>
    [ProducesResponseType(typeof(FileUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}/download-url/{fileType}")]
    public async Task<ActionResult<FileUrlDto>> GetDownloadUrlAsync(
        Guid id,
        string fileType,
        CancellationToken ct = default)
    {
        var result = await service.GetDownloadUrlAsync(id, fileType, ct);
        return MapUrlResult(result);
    }

    // ======================== Helpers ========================

    private ActionResult<GraduateWorkDto> MapCreateResult(Result<GraduateWorkDto, GraduateWorksError> result)
    {
        if (result.Error is not null)
        {
            return result.Error.Value switch
            {
                GraduateWorksError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        var dto = result.Value!;
        return CreatedAtAction(nameof(GetAsync), new { id = dto.Id, version = "1.0" }, dto);
    }

    private ActionResult<GraduateWorkDto> MapMutationResult(Result<GraduateWorkDto, GraduateWorksError> result)
    {
        if (result.Error is not null)
        {
            return result.Error.Value switch
            {
                GraduateWorksError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                GraduateWorksError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value!);
    }

    private ActionResult<FileUrlDto> MapUrlResult(Result<FileUrlDto, GraduateWorksError> result)
    {
        if (result.Error is not null)
        {
            return result.Error.Value switch
            {
                GraduateWorksError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                GraduateWorksError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value!);
    }
}

/// <summary>
/// Тело PUT для обновления ВКР (идентификатор в URL).
/// </summary>
public sealed record UpdateGraduateWorkBody(string Title, int Year, int Grade, string CommissionMembers);

/// <summary>
/// Тело POST confirm-upload: оригинальное имя файла с расширением (например, "Диплом_Иванов.docx").
/// </summary>
public sealed record ConfirmUploadBody(string FileName);
