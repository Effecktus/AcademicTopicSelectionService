-- Создание справочника ученых званий

DROP TABLE IF EXISTS "AcademicTitles" CASCADE;

CREATE TABLE "AcademicTitles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CodeName" CITEXT NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_AcademicTitles_CodeName_NotEmpty" CHECK (length(btrim("CodeName"::text)) > 0),
    CONSTRAINT "CK_AcademicTitles_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Вставка начальных значений
INSERT INTO "AcademicTitles" ("CodeName", "DisplayName") VALUES
('None', 'Без звания'),
('AssociateProfessor', 'Доцент'),
('Professor', 'Профессор');

-- Комментарии к таблице
COMMENT ON TABLE "AcademicTitles" IS 'Справочник ученых званий. Содержит системные и отображаемые названия званий.';

-- Комментарии к столбцам
COMMENT ON COLUMN "AcademicTitles"."Id" IS 'Уникальный идентификатор ученого звания';
COMMENT ON COLUMN "AcademicTitles"."CodeName" IS 'Системное значение звания (для кода), регистронезависимо';
COMMENT ON COLUMN "AcademicTitles"."DisplayName" IS 'Отображаемое значение звания (для пользовательского интерфейса)';
COMMENT ON COLUMN "AcademicTitles"."CreatedAt" IS 'Дата и время создания записи о звании';
COMMENT ON COLUMN "AcademicTitles"."UpdatedAt" IS 'Дата и время последнего обновления записи о звании';
