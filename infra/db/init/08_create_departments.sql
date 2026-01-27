-- Создание таблицы Departments (Кафедры)
-- Независимая таблица, создаётся первой из таблиц

DROP TABLE IF EXISTS "Departments" CASCADE;

CREATE TABLE "Departments" (
	"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
	"Name" VARCHAR(255) NOT NULL,
	"HeadId" UUID NULL,
	"CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"UpdatedAt" TIMESTAMP NULL,

	CONSTRAINT "UQ_Departments_Name" UNIQUE ("Name"),
	CONSTRAINT "CK_Departments_Name_NotEmpty" CHECK (length(btrim("Name")) > 0)
);

-- Комментарии к таблице
COMMENT ON TABLE "Departments" IS 'Таблица кафедр. Содержит информацию о кафедрах и их заведующих.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Departments"."Id" IS 'Уникальный идентификатор кафедры';
COMMENT ON COLUMN "Departments"."Name" IS 'Название кафедры';
COMMENT ON COLUMN "Departments"."HeadId" IS 'Идентификатор заведующего кафедрой (внешний ключ к таблице Users)';
COMMENT ON COLUMN "Departments"."CreatedAt" IS 'Дата и время создания записи о кафедре';
COMMENT ON COLUMN "Departments"."UpdatedAt" IS 'Дата и время последнего обновления записи о кафедре';