-- Создание справочника должностей

DROP TABLE IF EXISTS "Positions" CASCADE;

CREATE TABLE "Positions" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" CITEXT NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL,

    CONSTRAINT "CK_Positions_Name_NotEmpty" CHECK (length(btrim("Name"::text)) > 0),
    CONSTRAINT "CK_Positions_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Вставка начальных значений
INSERT INTO "Positions" ("Name", "DisplayName") VALUES
('Assistant', 'Ассистент'),
('SeniorLecturer', 'Старший преподаватель'),
('AssociateProfessor', 'Доцент'),
('Professor', 'Профессор'),
('DepartmentHead', 'Заведующий кафедрой');

-- Комментарии к таблице
COMMENT ON TABLE "Positions" IS 'Справочник должностей преподавателей. Содержит системные и отображаемые названия должностей.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Positions"."Id" IS 'Уникальный идентификатор должности';
COMMENT ON COLUMN "Positions"."Name" IS 'Системное значение должности (для кода), регистронезависимо';
COMMENT ON COLUMN "Positions"."DisplayName" IS 'Отображаемое значение должности (для пользовательского интерфейса)';
COMMENT ON COLUMN "Positions"."CreatedAt" IS 'Дата и время создания записи о должности';
COMMENT ON COLUMN "Positions"."UpdatedAt" IS 'Дата и время последнего обновления записи о должности';
