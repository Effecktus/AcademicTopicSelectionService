-- Создание справочника статусов тем

DROP TABLE IF EXISTS "TopicStatuses" CASCADE;

CREATE TABLE "TopicStatuses" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" CITEXT NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL,

    CONSTRAINT "CK_TopicStatuses_Name_NotEmpty" CHECK (length(btrim("Name"::text)) > 0),
    CONSTRAINT "CK_TopicStatuses_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Вставка начальных данных.
INSERT INTO "TopicStatuses" ("Name", "DisplayName") VALUES
('Active', 'Активна'),
('Inactive', 'Неактивна');

-- Комментарии к таблице
COMMENT ON TABLE "TopicStatuses" IS 'Справочник статусов тем ВКР. Содержит системные и отображаемые названия статусов.';

-- Комментарии к столбцам
COMMENT ON COLUMN "TopicStatuses"."Id" IS 'Уникальный идентификатор статуса темы';
COMMENT ON COLUMN "TopicStatuses"."Name" IS 'Системное значение статуса (для кода), регистронезависимо';
COMMENT ON COLUMN "TopicStatuses"."DisplayName" IS 'Отображаемое значение статуса (для пользовательского интерфейса)';
COMMENT ON COLUMN "TopicStatuses"."CreatedAt" IS 'Дата и время создания записи о статусе';
COMMENT ON COLUMN "TopicStatuses"."UpdatedAt" IS 'Дата и время последнего обновления записи о статусе';