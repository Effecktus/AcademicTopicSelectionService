-- Создание таблицы Departments (Кафедры)
-- Независимая таблица, создаётся первой из таблиц

DROP TABLE IF EXISTS "Departments" CASCADE;

CREATE TABLE "Departments" (
	"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
	"CodeName" CITEXT NOT NULL,
	"DisplayName" VARCHAR(255) NOT NULL,
	"HeadId" UUID NULL,
	"CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"UpdatedAt" TIMESTAMPTZ NULL,

	CONSTRAINT "UQ_Departments_CodeName" UNIQUE ("CodeName"),
	CONSTRAINT "CK_Departments_CodeName_NotEmpty" CHECK (length(btrim("CodeName"::text)) > 0),
	CONSTRAINT "CK_Departments_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Комментарии к таблице
COMMENT ON TABLE "Departments" IS 'Таблица кафедр. Содержит информацию о кафедрах и их заведующих.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Departments"."Id" IS 'Уникальный идентификатор кафедры';
COMMENT ON COLUMN "Departments"."CodeName" IS 'Системное значение кафедры (для кода), регистронезависимо';
COMMENT ON COLUMN "Departments"."DisplayName" IS 'Отображаемое значение кафедры (для пользовательского интерфейса)';
COMMENT ON COLUMN "Departments"."HeadId" IS 'Идентификатор заведующего кафедрой (внешний ключ к таблице Users)';
COMMENT ON COLUMN "Departments"."CreatedAt" IS 'Дата и время создания записи о кафедре';
COMMENT ON COLUMN "Departments"."UpdatedAt" IS 'Дата и время последнего обновления записи о кафедре';