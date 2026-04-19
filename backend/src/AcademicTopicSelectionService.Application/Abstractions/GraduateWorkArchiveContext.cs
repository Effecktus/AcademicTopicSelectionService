namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Данные заявки, необходимые для создания записи ВКР в архиве.
/// </summary>
public sealed record GraduateWorkArchiveContext(Guid StudentId, Guid TeacherId);
