-- Создание таблицы Users (Пользователи)
-- Зависит от: Departments (02_create_departments.sql) и user_role (01_create_enums.sql)

DROP TABLE IF EXISTS "Users" CASCADE;

CREATE TABLE "Users" (
	"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
	"Email" VARCHAR(255) NOT NULL UNIQUE,
	"PasswordHash" VARCHAR(255) NOT NULL,
	"FirstName" VARCHAR(100) NOT NULL,
	"LastName" VARCHAR(100) NOT NULL,
	"MiddleName" VARCHAR(100) NULL,
	"Role" user_role NOT NULL,
	"DepartmentId" UUID NULL,
	"CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"UpdatedAt" TIMESTAMP NULL,
	"IsActive" BOOLEAN NOT NULL DEFAULT true,
	
	CONSTRAINT "FK_Users_Departments"
		FOREIGN KEY ("DepartmentId")
		REFERENCES "Departments"("Id")
		ON DELETE SET NULL
		ON UPDATE CASCADE
);

-- Комментарии
COMMENT ON TABLE "Users" IS 'Таблица пользователей системы';
COMMENT ON COLUMN "Users"."Email" IS 'Email пользователя (уникальный)';
COMMENT ON COLUMN "Users"."Role" IS 'Роль пользователя: Student, Teacher, DepartmentHead, Admin';
COMMENT ON COLUMN "Users"."DepartmentId" IS 'Ссылка на кафедру пользователя';
COMMENT ON COLUMN "Users"."CreatedAt" IS 'Дата и время создания записи';
COMMENT ON COLUMN "Users"."UpdatedAt" IS 'Дата и время последнего обновления записи (обновляется автоматически триггером)';
COMMENT ON COLUMN "Users"."IsActive" IS 'Флаг активности пользователя';