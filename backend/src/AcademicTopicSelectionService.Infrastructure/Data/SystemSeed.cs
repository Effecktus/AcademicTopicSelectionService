namespace AcademicTopicSelectionService.Infrastructure.Data;

/// <summary>
/// Фиксированные идентификаторы обязательного наполнения системы: справочников и учётной записи администратора.
/// Одинаковы во всех окружениях; строки создаются миграциями (см. <c>ApplicationDbContext.SeedData.cs</c>).
/// </summary>
/// <remarks>Значения менять нельзя: на них ссылаются данные в уже развёрнутых базах.</remarks>
public static class SystemSeed
{
    /// <summary>
    /// Значение <c>PasswordHash</c> администратора до первой установки пароля.
    /// Не является BCrypt-хешем, поэтому войти с ним невозможно.
    /// </summary>
    public const string AdminPasswordPlaceholder = "!";

    /// <summary>Учётные записи пользователей.</summary>
    public static class Users
    {
        /// <summary>Администратор системы.</summary>
        public static readonly Guid Admin = new("037b31dc-d13a-423e-8b0b-983494f8a7f9");
    }

    /// <summary>Роли пользователей (<c>UserRoles</c>).</summary>
    public static class UserRoles
    {
        public static readonly Guid Student = new("dde522f4-db45-4961-9466-f041aea1b345");
        public static readonly Guid Teacher = new("b7fd4e95-d013-4633-b018-4a971199e6f4");
        public static readonly Guid DepartmentHead = new("385409b2-385c-4d5e-9325-85c73ed4c8ba");
        public static readonly Guid Admin = new("58f7ed6b-fdb7-404e-bdb9-ffa45abbba51");
    }

    /// <summary>Статусы заявок (<c>ApplicationStatuses</c>).</summary>
    public static class ApplicationStatuses
    {
        public static readonly Guid OnEditing = new("9fc52948-532c-45f1-8d1e-444dda62a7f4");
        public static readonly Guid Pending = new("b673307e-6a2d-404d-b52c-5fe54f689ebc");
        public static readonly Guid ApprovedBySupervisor = new("683a9a61-148e-4de2-8c96-9bcbb1727241");
        public static readonly Guid RejectedBySupervisor = new("96191172-82c0-4760-bf02-f1fb8c695638");
        public static readonly Guid PendingDepartmentHead = new("ad95b86a-caf5-4e81-b056-def8ddd3a0f6");
        public static readonly Guid ApprovedByDepartmentHead = new("5fcdc4ad-5c81-403c-95a4-2ab1e9bdb390");
        public static readonly Guid RejectedByDepartmentHead = new("a5f8904b-96b2-4f33-ba23-3f1f0d347a66");
        public static readonly Guid Cancelled = new("a196d568-1e19-4f44-937c-7f0b92b7877f");
    }

    /// <summary>Статусы действий по заявкам (<c>ApplicationActionStatuses</c>).</summary>
    public static class ApplicationActionStatuses
    {
        public static readonly Guid Pending = new("1fb2b19d-271f-4df7-b544-3a6cb97e2398");
        public static readonly Guid Approved = new("4f78fc4b-7116-4b48-bb57-84ca2af36175");
        public static readonly Guid Rejected = new("05a67ee5-eb98-4ff8-a511-abca3abcce10");
        public static readonly Guid ReturnedForEditing = new("b381e910-5967-45cd-a48b-a907c25e385c");
        public static readonly Guid Cancelled = new("459226af-b979-4af6-a598-a1c72f2ba865");
    }

    /// <summary>Статусы тем (<c>TopicStatuses</c>).</summary>
    public static class TopicStatuses
    {
        public static readonly Guid Active = new("0d032d1c-692c-4566-95df-62df8e9ddcd3");
        public static readonly Guid Inactive = new("57d06be7-4dec-4515-945f-815969992f36");
    }

    /// <summary>Типы уведомлений (<c>NotificationTypes</c>).</summary>
    public static class NotificationTypes
    {
        public static readonly Guid ApplicationStatusChanged = new("c0e778fd-2499-47e1-bad1-7ec7fe99aa74");
        public static readonly Guid NewMessage = new("397e63a0-5d1e-4330-9812-940fe8660344");
        public static readonly Guid TopicApproved = new("2ada1cff-7051-4923-9db2-fbc5cc32881f");
        public static readonly Guid TopicRejected = new("11a3ad63-8018-4318-aa61-dc3cbb476922");
        public static readonly Guid SupervisorRequestStatusChanged = new("1dbba1a7-1e14-4094-9b87-6ca0af52264d");
        public static readonly Guid SupervisorRequestCreated = new("846d97d2-1872-47c4-91e0-78f452a18eb9");
        public static readonly Guid ApplicationSubmittedToSupervisor = new("db554618-92d5-4479-ba4f-4c1f3260cc05");
        public static readonly Guid ApplicationSubmittedToDepartmentHead = new("ae653734-653a-4c1f-8d75-ead52887fce1");
        public static readonly Guid SupervisorDecisionByDepartmentHead = new("1352b13f-8f9f-4f3b-802d-274a4fbc631c");
        public static readonly Guid GraduateWorkUploaded = new("1f1a210b-271d-4964-86e6-00f1013a5332");
    }

    /// <summary>Учёные степени (<c>AcademicDegrees</c>).</summary>
    public static class AcademicDegrees
    {
        public static readonly Guid None = new("576dc6b1-1dc5-4a28-a023-07b1f3783817");
        public static readonly Guid DoctorOfBiologicalSciences = new("ec876ca2-a2ed-4b5c-9241-eccc01931c83");
        public static readonly Guid DoctorOfVeterinarySciences = new("b9171e4b-51b6-4397-8fa7-4621c63f557e");
        public static readonly Guid DoctorOfMilitarySciences = new("4569d55c-f052-4493-914a-11498d8119db");
        public static readonly Guid DoctorOfGeographicalSciences = new("e11b58ea-ee79-4124-864f-66d66c937947");
        public static readonly Guid DoctorOfArtHistory = new("759075f2-bda2-4c85-9b47-294f9166ea27");
        public static readonly Guid DoctorOfHistoricalSciences = new("c4242756-5170-4c92-a4f0-7d5c646c386a");
        public static readonly Guid DoctorOfCulturalStudies = new("fe1b8f5c-8bb2-45cb-834c-9178bdf37814");
        public static readonly Guid DoctorOfMedicalSciences = new("c020767c-038f-4187-af35-78681f3e53f1");
        public static readonly Guid DoctorOfPedagogicalSciences = new("8bf652a0-58ff-44b3-adc1-c6628e200808");
        public static readonly Guid DoctorOfPoliticalSciences = new("56ef0a07-6318-4baa-af41-2f7bb97904e8");
        public static readonly Guid DoctorOfPsychologicalSciences = new("44fe22e5-3025-4f00-b7db-49035f7c6289");
        public static readonly Guid DoctorOfAgriculturalSciences = new("e214d642-2b3e-4e10-b6b8-8dd96a796ce2");
        public static readonly Guid DoctorOfSociologicalSciences = new("13c2a1b7-2b3b-46d4-a057-9c7b35eb5cf9");
        public static readonly Guid DoctorOfTechnicalSciences = new("b112a3b6-c56a-49b6-8c14-b9f8cb30e3b8");
        public static readonly Guid DoctorOfPhysicsAndMathematics = new("8898b736-c62d-45c3-82e6-20e556ff51d3");
        public static readonly Guid DoctorOfPhilologicalSciences = new("082f63c9-6d9e-4462-973d-51e56f9c6580");
        public static readonly Guid DoctorOfPhilosophicalSciences = new("f3669689-9263-4ac5-a33d-b6cb00bbbfd2");
        public static readonly Guid DoctorOfChemicalSciences = new("2d88d4a1-a3c4-4f62-8263-075e10008600");
        public static readonly Guid DoctorOfEconomicSciences = new("b5f1d21a-921f-4117-9873-7026651df706");
        public static readonly Guid DoctorOfLegalSciences = new("6aab8f60-bfc5-4256-ad9b-a36332bbad44");
        public static readonly Guid CandidateOfBiologicalSciences = new("e7c44f14-4964-47b4-9b45-f2ee58884794");
        public static readonly Guid CandidateOfVeterinarySciences = new("32335bdf-0ea2-4cce-a263-d2d6a5f0f082");
        public static readonly Guid CandidateOfMilitarySciences = new("ed402408-da72-4adc-9861-5ac9d835288d");
        public static readonly Guid CandidateOfGeographicalSciences = new("fef7b9bd-eff4-4105-823a-7f7d8590d80e");
        public static readonly Guid CandidateOfArtHistory = new("cac1b0e2-860e-416d-a970-c17616dc34d4");
        public static readonly Guid CandidateOfHistoricalSciences = new("6acde269-0f6d-4987-ab84-b7fc4a17092e");
        public static readonly Guid CandidateOfCulturalStudies = new("1e293e01-8b5d-480d-9408-77c684fd37bc");
        public static readonly Guid CandidateOfMedicalSciences = new("25abae5b-8ef7-48ab-88f2-076f85fd3de7");
        public static readonly Guid CandidateOfPedagogicalSciences = new("82eb2516-61bd-4eb0-b185-82d45f21e50e");
        public static readonly Guid CandidateOfPoliticalSciences = new("9370c85e-0ea8-4820-9a8d-cc47ef173ea0");
        public static readonly Guid CandidateOfPsychologicalSciences = new("65db8961-b214-4f13-a52a-d77efc6ae4fb");
        public static readonly Guid CandidateOfAgriculturalSciences = new("151b1b4e-3dac-469a-a282-ed3092255c07");
        public static readonly Guid CandidateOfSociologicalSciences = new("f466844e-2b3d-4706-921b-a3782f3f71fe");
        public static readonly Guid CandidateOfTechnicalSciences = new("5205b00e-1af4-42c0-b8a8-8270b7fbf6fc");
        public static readonly Guid CandidateOfPhysicsAndMathematics = new("89ca22b2-bf66-4d9e-b047-ee93cf3729c4");
        public static readonly Guid CandidateOfPhilologicalSciences = new("deac597f-0048-45cd-a7c3-388b71a642e4");
        public static readonly Guid CandidateOfPhilosophicalSciences = new("12855269-4d2f-4725-8dfd-972dedc893e6");
        public static readonly Guid CandidateOfChemicalSciences = new("c6ddc379-46ef-456f-9363-43e584bd3112");
        public static readonly Guid CandidateOfEconomicSciences = new("39d26f17-2ec9-42bc-bc64-9fc821d622ff");
        public static readonly Guid CandidateOfLegalSciences = new("b06e6242-9b43-40ad-aa6d-32b4458f0a8f");
    }

    /// <summary>Учёные звания (<c>AcademicTitles</c>).</summary>
    public static class AcademicTitles
    {
        public static readonly Guid None = new("ef488515-0904-448f-88eb-86a0a7c36cc6");
        public static readonly Guid AssociateProfessor = new("03f6f767-6ed5-41d4-a26a-0745b89143ee");
        public static readonly Guid Professor = new("b9715e4d-9eaf-4fcd-af4f-fd4edaaca4fe");
    }

    /// <summary>Должности преподавателей (<c>Positions</c>).</summary>
    public static class Positions
    {
        public static readonly Guid Assistant = new("d7f907c1-8a09-441b-8cd5-3f5ffb44095c");
        public static readonly Guid SeniorLecturer = new("547ae28e-dcc6-4072-8e49-1a253b939546");
        public static readonly Guid AssociateProfessor = new("3fb73bf0-f882-46f3-9e0f-e241c4f22c12");
        public static readonly Guid Professor = new("1d90d84c-d463-4399-bf1e-1a461a4c7cdd");
        public static readonly Guid DepartmentHead = new("189e2454-9e62-4a43-8cfd-dfd806fbf32e");
    }

    /// <summary>Типы создателей тем (<c>TopicCreatorTypes</c>).</summary>
    public static class TopicCreatorTypes
    {
        public static readonly Guid Teacher = new("c3bde87f-6409-47a8-a527-9d9708296bb4");
        public static readonly Guid Student = new("208d385e-62a4-498d-9c5e-d67aeb96482b");
    }

    /// <summary>Статусы ВКР (<c>GraduateWorkStatuses</c>).</summary>
    public static class GraduateWorkStatuses
    {
        public static readonly Guid Draft = new("965079ef-80ab-4b6b-ae7c-86d21f1abfc7");
        public static readonly Guid Completed = new("18812e15-4341-4bee-a254-93f8f2ea5fc8");
    }
}
