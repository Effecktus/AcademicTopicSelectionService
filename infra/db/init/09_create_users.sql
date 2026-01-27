-- Создание таблицы Users (Пользователи)
-- Зависит от: Departments и UserRole

DROP TABLE IF EXISTS "Users" CASCADE;

CREATE TABLE "Users" (
	"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
	"Email" CITEXT NOT NULL UNIQUE,
	"PasswordHash" VARCHAR(255) NOT NULL,
	"FirstName" VARCHAR(100) NOT NULL,
	"LastName" VARCHAR(100) NOT NULL,
	"MiddleName" VARCHAR(100) NULL,
	"RoleId" UUID NOT NULL,
	"DepartmentId" UUID NULL,
	"CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"UpdatedAt" TIMESTAMP NULL,
	"IsActive" BOOLEAN NOT NULL DEFAULT true,
	
	CONSTRAINT "CK_Users_Email_NotEmpty" CHECK (length(btrim("Email"::text)) > 0),
	CONSTRAINT "CK_Users_Email_NoSpaces" CHECK (position(' ' in "Email"::text) = 0),
	CONSTRAINT "CK_Users_Email_Trimmed" CHECK ("Email"::text = btrim("Email"::text)),
	CONSTRAINT "CK_Users_PasswordHash_NotEmpty" CHECK (length(btrim("PasswordHash")) > 0),
	CONSTRAINT "CK_Users_FirstName_NotEmpty" CHECK (length(btrim("FirstName")) > 0),
	CONSTRAINT "CK_Users_LastName_NotEmpty" CHECK (length(btrim("LastName")) > 0),

	CONSTRAINT "FK_Users_Departments"
		FOREIGN KEY ("DepartmentId")
		REFERENCES "Departments"("Id")
		ON DELETE SET NULL
		ON UPDATE CASCADE,
		
	CONSTRAINT "FK_Users_UserRoles"
	   FOREIGN KEY ("RoleId")
	   REFERENCES "UserRoles"("Id")
	   ON DELETE RESTRICT
	   ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "Users" IS 'Таблица пользователей системы. Содержит основную информацию о пользователях: учетные данные, персональные данные, роль и принадлежность к кафедре.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Users"."Id" IS 'Уникальный идентификатор пользователя';
COMMENT ON COLUMN "Users"."Email" IS 'Email пользователя (уникальный, регистронезависимый)';
COMMENT ON COLUMN "Users"."PasswordHash" IS 'Хеш пароля пользователя';
COMMENT ON COLUMN "Users"."FirstName" IS 'Имя пользователя';
COMMENT ON COLUMN "Users"."LastName" IS 'Фамилия пользователя';
COMMENT ON COLUMN "Users"."MiddleName" IS 'Отчество пользователя';
COMMENT ON COLUMN "Users"."RoleId" IS 'Идентификатор роли пользователя (внешний ключ к таблице UserRoles)';
COMMENT ON COLUMN "Users"."DepartmentId" IS 'Идентификатор кафедры пользователя (внешний ключ к таблице Departments)';
COMMENT ON COLUMN "Users"."CreatedAt" IS 'Дата и время создания записи о пользователе';
COMMENT ON COLUMN "Users"."UpdatedAt" IS 'Дата и время последнего обновления записи о пользователе';
COMMENT ON COLUMN "Users"."IsActive" IS 'Флаг активности пользователя (true - активен, false - деактивирован)';
