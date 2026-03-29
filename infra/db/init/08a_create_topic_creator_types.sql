-- Создание справочника типов создателей тем ВКР

DROP TABLE IF EXISTS "TopicCreatorTypes" CASCADE;

CREATE TABLE "TopicCreatorTypes" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CodeName" CITEXT NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_TopicCreatorTypes_CodeName_NotEmpty" CHECK (length(btrim("CodeName"::text)) > 0),
    CONSTRAINT "CK_TopicCreatorTypes_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Вставка начальных данных.
INSERT INTO "TopicCreatorTypes" ("CodeName", "DisplayName") VALUES
('Teacher', 'Научный руководитель'),
('Student', 'Студент');

-- Комментарии к таблице
COMMENT ON TABLE "TopicCreatorTypes" IS 'Справочник типов пользователей, создающих темы ВКР. Определяет, кем была предложена тема: научным руководителем или студентом.';

-- Комментарии к столбцам
COMMENT ON COLUMN "TopicCreatorTypes"."Id" IS 'Уникальный идентификатор типа создателя темы';
COMMENT ON COLUMN "TopicCreatorTypes"."CodeName" IS 'Системное значение типа (для кода), регистронезависимо';
COMMENT ON COLUMN "TopicCreatorTypes"."DisplayName" IS 'Отображаемое значение типа (для пользовательского интерфейса)';
COMMENT ON COLUMN "TopicCreatorTypes"."CreatedAt" IS 'Дата и время создания записи';
COMMENT ON COLUMN "TopicCreatorTypes"."UpdatedAt" IS 'Дата и время последнего обновления записи';
