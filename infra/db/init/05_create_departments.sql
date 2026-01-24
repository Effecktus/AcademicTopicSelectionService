-- Создание таблицы Departments (Кафедры)
-- Независимая таблица, создаётся первой из таблиц

DROP TABLE IF EXISTS "Departments" CASCADE;

CREATE TABLE "Departments" (
	"Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
	"Name" VARCHAR(255) NOT NULL,
	"HeadId" UUID NULL,
	"CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"UpdatedAt" TIMESTAMP NULL
);

-- Комментарии для документации
COMMENT ON TABLE "Departments" IS 'Таблица кафедр';
COMMENT ON COLUMN "Departments"."Id" IS 'Уникальный идентификатор кафедры';
COMMENT ON COLUMN "Departments"."Name" IS 'Название кафедры';
COMMENT ON COLUMN "Departments"."HeadId" IS 'Ссылка на пользователя - заведующего кафедрой (добавится после создания таблицы Users)';
COMMENT ON COLUMN "Departments"."CreatedAt" IS 'Дата и время создания записи';
COMMENT ON COLUMN "Departments"."UpdatedAt" IS 'Дата и время последнего обновления записи (обновляется автоматически триггером)';