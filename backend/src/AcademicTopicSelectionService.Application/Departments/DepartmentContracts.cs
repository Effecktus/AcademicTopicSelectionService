namespace AcademicTopicSelectionService.Application.Departments;

/// <summary>
/// Краткое представление кафедры для выпадающих списков и справочников.
/// </summary>
public sealed record DepartmentDto(Guid Id, string CodeName, string DisplayName);
