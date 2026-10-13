using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicTopicSelectionService.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class UnifySchemaForProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_Head",
                table: "Departments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Email_NoSpaces",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Email_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Email_Trimmed",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_FirstName_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_LastName_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PasswordHash_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserRoles_CodeName_NotEmpty",
                table: "UserRoles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserRoles_DisplayName_NotEmpty",
                table: "UserRoles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TopicStatuses_CodeName_NotEmpty",
                table: "TopicStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TopicStatuses_DisplayName_NotEmpty",
                table: "TopicStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Topics_Title_NotEmpty",
                table: "Topics");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TopicCreatorTypes_CodeName_NotEmpty",
                table: "TopicCreatorTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TopicCreatorTypes_DisplayName_NotEmpty",
                table: "TopicCreatorTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Positions_CodeName_NotEmpty",
                table: "Positions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Positions_DisplayName_NotEmpty",
                table: "Positions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotificationTypes_CodeName_NotEmpty",
                table: "NotificationTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotificationTypes_DisplayName_NotEmpty",
                table: "NotificationTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Content_NotEmpty",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Title_NotEmpty",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorkStatuses_CodeName_NotEmpty",
                table: "GraduateWorkStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorkStatuses_DisplayName_NotEmpty",
                table: "GraduateWorkStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_CommissionMembers_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_FileName_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_FilePath_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_PresentationFileName_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_PresentationPath_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_Title_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Departments_CodeName_NotEmpty",
                table: "Departments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Departments_DisplayName_NotEmpty",
                table: "Departments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChatMessages_Content_NotEmpty",
                table: "ChatMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationStatuses_CodeName_NotEmpty",
                table: "ApplicationStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationStatuses_DisplayName_NotEmpty",
                table: "ApplicationStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationActionStatuses_CodeName_NotEmpty",
                table: "ApplicationActionStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationActionStatuses_DisplayName_NotEmpty",
                table: "ApplicationActionStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationActions_Comment_NotEmpty",
                table: "ApplicationActions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicTitles_CodeName_NotEmpty",
                table: "AcademicTitles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicTitles_DisplayName_NotEmpty",
                table: "AcademicTitles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicDegrees_CodeName_NotEmpty",
                table: "AcademicDegrees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicDegrees_DisplayName_NotEmpty",
                table: "AcademicDegrees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicDegrees_ShortName_NotEmpty",
                table: "AcademicDegrees");

            migrationBuilder.AlterColumn<string>(
                name: "MiddleName",
                table: "Users",
                type: "citext",
                maxLength: 100,
                nullable: true,
                comment: "Отчество пользователя",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Отчество пользователя");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Фамилия пользователя",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Фамилия пользователя");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Имя пользователя",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Имя пользователя");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "UserRoles",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение роли (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение роли (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "TopicStatuses",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение статуса (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение статуса (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "TopicCreatorTypes",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение типа (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение типа (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Positions",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение должности (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение должности (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "NotificationTypes",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение типа (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение типа (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "GraduateWorkStatuses",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение статуса (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение статуса (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Departments",
                type: "citext",
                maxLength: 255,
                nullable: false,
                comment: "Отображаемое значение кафедры (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldComment: "Отображаемое значение кафедры (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "ApplicationStatuses",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение статуса (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение статуса (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "ApplicationActionStatuses",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение статуса (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение статуса (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AcademicTitles",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение звания (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение звания (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "AcademicDegrees",
                type: "citext",
                maxLength: 50,
                nullable: true,
                comment: "Сокращенное название степени (для отображения в кратких формах)",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "Сокращенное название степени (для отображения в кратких формах)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AcademicDegrees",
                type: "citext",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение степени (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение степени (для пользовательского интерфейса)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Email_NoSpaces",
                table: "Users",
                sql: "\"Email\" NOT LIKE '% %'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Email_NotEmpty",
                table: "Users",
                sql: "TRIM(\"Email\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_FirstName_NotEmpty",
                table: "Users",
                sql: "TRIM(\"FirstName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_LastName_NotEmpty",
                table: "Users",
                sql: "TRIM(\"LastName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PasswordHash_NotEmpty",
                table: "Users",
                sql: "TRIM(\"PasswordHash\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserRoles_CodeName_NotEmpty",
                table: "UserRoles",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserRoles_DisplayName_NotEmpty",
                table: "UserRoles",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TopicStatuses_CodeName_NotEmpty",
                table: "TopicStatuses",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TopicStatuses_DisplayName_NotEmpty",
                table: "TopicStatuses",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Topics_Title_NotEmpty",
                table: "Topics",
                sql: "TRIM(\"Title\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TopicCreatorTypes_CodeName_NotEmpty",
                table: "TopicCreatorTypes",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TopicCreatorTypes_DisplayName_NotEmpty",
                table: "TopicCreatorTypes",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Positions_CodeName_NotEmpty",
                table: "Positions",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Positions_DisplayName_NotEmpty",
                table: "Positions",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotificationTypes_CodeName_NotEmpty",
                table: "NotificationTypes",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotificationTypes_DisplayName_NotEmpty",
                table: "NotificationTypes",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Content_NotEmpty",
                table: "Notifications",
                sql: "TRIM(\"Content\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Title_NotEmpty",
                table: "Notifications",
                sql: "TRIM(\"Title\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorkStatuses_CodeName_NotEmpty",
                table: "GraduateWorkStatuses",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorkStatuses_DisplayName_NotEmpty",
                table: "GraduateWorkStatuses",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_CommissionMembers_NotEmpty",
                table: "GraduateWorks",
                sql: "\"CommissionMembers\" IS NULL OR TRIM(\"CommissionMembers\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_FileName_NotEmpty",
                table: "GraduateWorks",
                sql: "\"FileName\" IS NULL OR TRIM(\"FileName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_FilePath_NotEmpty",
                table: "GraduateWorks",
                sql: "\"FilePath\" IS NULL OR TRIM(\"FilePath\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_PresentationFileName_NotEmpty",
                table: "GraduateWorks",
                sql: "\"PresentationFileName\" IS NULL OR TRIM(\"PresentationFileName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_PresentationPath_NotEmpty",
                table: "GraduateWorks",
                sql: "\"PresentationPath\" IS NULL OR TRIM(\"PresentationPath\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_Title_NotEmpty",
                table: "GraduateWorks",
                sql: "TRIM(\"Title\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Departments_CodeName_NotEmpty",
                table: "Departments",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Departments_DisplayName_NotEmpty",
                table: "Departments",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChatMessages_Content_NotEmpty",
                table: "ChatMessages",
                sql: "TRIM(\"Content\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationStatuses_CodeName_NotEmpty",
                table: "ApplicationStatuses",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationStatuses_DisplayName_NotEmpty",
                table: "ApplicationStatuses",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationActionStatuses_CodeName_NotEmpty",
                table: "ApplicationActionStatuses",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationActionStatuses_DisplayName_NotEmpty",
                table: "ApplicationActionStatuses",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationActions_Comment_NotEmpty",
                table: "ApplicationActions",
                sql: "\"Comment\" IS NULL OR TRIM(\"Comment\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicTitles_CodeName_NotEmpty",
                table: "AcademicTitles",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicTitles_DisplayName_NotEmpty",
                table: "AcademicTitles",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicDegrees_CodeName_NotEmpty",
                table: "AcademicDegrees",
                sql: "TRIM(\"CodeName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicDegrees_DisplayName_NotEmpty",
                table: "AcademicDegrees",
                sql: "TRIM(\"DisplayName\") <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicDegrees_ShortName_NotEmpty",
                table: "AcademicDegrees",
                sql: "\"ShortName\" IS NULL OR TRIM(\"ShortName\") <> ''");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_Head",
                table: "Departments",
                column: "HeadId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_Head",
                table: "Departments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Email_NoSpaces",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Email_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_FirstName_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_LastName_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PasswordHash_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserRoles_CodeName_NotEmpty",
                table: "UserRoles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserRoles_DisplayName_NotEmpty",
                table: "UserRoles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TopicStatuses_CodeName_NotEmpty",
                table: "TopicStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TopicStatuses_DisplayName_NotEmpty",
                table: "TopicStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Topics_Title_NotEmpty",
                table: "Topics");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TopicCreatorTypes_CodeName_NotEmpty",
                table: "TopicCreatorTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TopicCreatorTypes_DisplayName_NotEmpty",
                table: "TopicCreatorTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Positions_CodeName_NotEmpty",
                table: "Positions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Positions_DisplayName_NotEmpty",
                table: "Positions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotificationTypes_CodeName_NotEmpty",
                table: "NotificationTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotificationTypes_DisplayName_NotEmpty",
                table: "NotificationTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Content_NotEmpty",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Title_NotEmpty",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorkStatuses_CodeName_NotEmpty",
                table: "GraduateWorkStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorkStatuses_DisplayName_NotEmpty",
                table: "GraduateWorkStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_CommissionMembers_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_FileName_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_FilePath_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_PresentationFileName_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_PresentationPath_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduateWorks_Title_NotEmpty",
                table: "GraduateWorks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Departments_CodeName_NotEmpty",
                table: "Departments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Departments_DisplayName_NotEmpty",
                table: "Departments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChatMessages_Content_NotEmpty",
                table: "ChatMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationStatuses_CodeName_NotEmpty",
                table: "ApplicationStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationStatuses_DisplayName_NotEmpty",
                table: "ApplicationStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationActionStatuses_CodeName_NotEmpty",
                table: "ApplicationActionStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationActionStatuses_DisplayName_NotEmpty",
                table: "ApplicationActionStatuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApplicationActions_Comment_NotEmpty",
                table: "ApplicationActions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicTitles_CodeName_NotEmpty",
                table: "AcademicTitles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicTitles_DisplayName_NotEmpty",
                table: "AcademicTitles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicDegrees_CodeName_NotEmpty",
                table: "AcademicDegrees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicDegrees_DisplayName_NotEmpty",
                table: "AcademicDegrees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicDegrees_ShortName_NotEmpty",
                table: "AcademicDegrees");

            migrationBuilder.AlterColumn<string>(
                name: "MiddleName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Отчество пользователя",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Отчество пользователя");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Фамилия пользователя",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Фамилия пользователя");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Имя пользователя",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Имя пользователя");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "UserRoles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение роли (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение роли (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "TopicStatuses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение статуса (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение статуса (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "TopicCreatorTypes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение типа (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение типа (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Positions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение должности (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение должности (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "NotificationTypes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение типа (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение типа (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "GraduateWorkStatuses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение статуса (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение статуса (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Departments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                comment: "Отображаемое значение кафедры (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldComment: "Отображаемое значение кафедры (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "ApplicationStatuses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение статуса (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение статуса (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "ApplicationActionStatuses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение статуса (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение статуса (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AcademicTitles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение звания (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение звания (для пользовательского интерфейса)");

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "AcademicDegrees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "Сокращенное название степени (для отображения в кратких формах)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "Сокращенное название степени (для отображения в кратких формах)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AcademicDegrees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Отображаемое значение степени (для пользовательского интерфейса)",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldComment: "Отображаемое значение степени (для пользовательского интерфейса)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Email_NoSpaces",
                table: "Users",
                sql: "position(' ' in \"Email\"::text) = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Email_NotEmpty",
                table: "Users",
                sql: "length(btrim(\"Email\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Email_Trimmed",
                table: "Users",
                sql: "\"Email\"::text = btrim(\"Email\"::text)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_FirstName_NotEmpty",
                table: "Users",
                sql: "length(btrim(\"FirstName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_LastName_NotEmpty",
                table: "Users",
                sql: "length(btrim(\"LastName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PasswordHash_NotEmpty",
                table: "Users",
                sql: "length(btrim(\"PasswordHash\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserRoles_CodeName_NotEmpty",
                table: "UserRoles",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserRoles_DisplayName_NotEmpty",
                table: "UserRoles",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TopicStatuses_CodeName_NotEmpty",
                table: "TopicStatuses",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TopicStatuses_DisplayName_NotEmpty",
                table: "TopicStatuses",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Topics_Title_NotEmpty",
                table: "Topics",
                sql: "length(btrim(\"Title\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TopicCreatorTypes_CodeName_NotEmpty",
                table: "TopicCreatorTypes",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TopicCreatorTypes_DisplayName_NotEmpty",
                table: "TopicCreatorTypes",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Positions_CodeName_NotEmpty",
                table: "Positions",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Positions_DisplayName_NotEmpty",
                table: "Positions",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotificationTypes_CodeName_NotEmpty",
                table: "NotificationTypes",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotificationTypes_DisplayName_NotEmpty",
                table: "NotificationTypes",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Content_NotEmpty",
                table: "Notifications",
                sql: "length(btrim(\"Content\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Title_NotEmpty",
                table: "Notifications",
                sql: "length(btrim(\"Title\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorkStatuses_CodeName_NotEmpty",
                table: "GraduateWorkStatuses",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorkStatuses_DisplayName_NotEmpty",
                table: "GraduateWorkStatuses",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_CommissionMembers_NotEmpty",
                table: "GraduateWorks",
                sql: "\"CommissionMembers\" IS NULL OR length(btrim(\"CommissionMembers\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_FileName_NotEmpty",
                table: "GraduateWorks",
                sql: "\"FileName\" IS NULL OR length(btrim(\"FileName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_FilePath_NotEmpty",
                table: "GraduateWorks",
                sql: "\"FilePath\" IS NULL OR length(btrim(\"FilePath\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_PresentationFileName_NotEmpty",
                table: "GraduateWorks",
                sql: "\"PresentationFileName\" IS NULL OR length(btrim(\"PresentationFileName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_PresentationPath_NotEmpty",
                table: "GraduateWorks",
                sql: "\"PresentationPath\" IS NULL OR length(btrim(\"PresentationPath\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduateWorks_Title_NotEmpty",
                table: "GraduateWorks",
                sql: "length(btrim(\"Title\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Departments_CodeName_NotEmpty",
                table: "Departments",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Departments_DisplayName_NotEmpty",
                table: "Departments",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChatMessages_Content_NotEmpty",
                table: "ChatMessages",
                sql: "length(btrim(\"Content\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationStatuses_CodeName_NotEmpty",
                table: "ApplicationStatuses",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationStatuses_DisplayName_NotEmpty",
                table: "ApplicationStatuses",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationActionStatuses_CodeName_NotEmpty",
                table: "ApplicationActionStatuses",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationActionStatuses_DisplayName_NotEmpty",
                table: "ApplicationActionStatuses",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApplicationActions_Comment_NotEmpty",
                table: "ApplicationActions",
                sql: "\"Comment\" IS NULL OR length(btrim(\"Comment\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicTitles_CodeName_NotEmpty",
                table: "AcademicTitles",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicTitles_DisplayName_NotEmpty",
                table: "AcademicTitles",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicDegrees_CodeName_NotEmpty",
                table: "AcademicDegrees",
                sql: "length(btrim(\"CodeName\"::text)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicDegrees_DisplayName_NotEmpty",
                table: "AcademicDegrees",
                sql: "length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicDegrees_ShortName_NotEmpty",
                table: "AcademicDegrees",
                sql: "\"ShortName\" IS NULL OR length(btrim(\"ShortName\")) > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_Head",
                table: "Departments",
                column: "HeadId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
