namespace AcademicTopicSelectionService.Infrastructure.Email;

public sealed class EmailDispatchOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Smtp";
}
