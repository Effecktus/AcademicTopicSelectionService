-- Создание справочника статусов действий по заявке.
-- Зависит от: ничего (базовый справочник)

DROP TABLE IF EXISTS "ApplicationActionStatuses" CASCADE;

CREATE TABLE "ApplicationActionStatuses" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CodeName" CITEXT NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_ApplicationActionStatuses_CodeName_NotEmpty" CHECK (length(btrim("CodeName"::text)) > 0),
    CONSTRAINT "CK_ApplicationActionStatuses_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Вставка начальных данных.
INSERT INTO "ApplicationActionStatuses" ("CodeName", "DisplayName") VALUES
('Pending',   'На согласовании'),
('Approved',  'Согласовано'),
('Rejected',  'Отклонено'),
('Cancelled', 'Отменено');

-- Комментарии к таблице
COMMENT ON TABLE "ApplicationActionStatuses" IS 'Справочник статусов действий по заявкам на темы ВКР. Определяет результат рассмотрения: на согласовании, согласовано, отклонено или отменено.';

-- Комментарии к столбцам
COMMENT ON COLUMN "ApplicationActionStatuses"."Id"          IS 'Уникальный идентификатор статуса действия';
COMMENT ON COLUMN "ApplicationActionStatuses"."CodeName"    IS 'Системное значение статуса (для кода), регистронезависимо';
COMMENT ON COLUMN "ApplicationActionStatuses"."DisplayName" IS 'Отображаемое значение статуса (для пользовательского интерфейса)';
COMMENT ON COLUMN "ApplicationActionStatuses"."CreatedAt"   IS 'Дата и время создания записи о статусе';
COMMENT ON COLUMN "ApplicationActionStatuses"."UpdatedAt"   IS 'Дата и время последнего обновления записи о статусе';
