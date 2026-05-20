-- Создание справочника статусов ВКР.

DROP TABLE IF EXISTS "GraduateWorkStatuses" CASCADE;

CREATE TABLE "GraduateWorkStatuses" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CodeName" CITEXT NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_GraduateWorkStatuses_CodeName_NotEmpty" CHECK (length(btrim("CodeName"::text)) > 0),
    CONSTRAINT "CK_GraduateWorkStatuses_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Вставка начальных данных.
INSERT INTO "GraduateWorkStatuses" ("CodeName", "DisplayName") VALUES
('Draft',     'Черновик'),
('Completed', 'Заполнено');

-- Комментарии к таблице
COMMENT ON TABLE "GraduateWorkStatuses" IS 'Справочник статусов выпускных квалификационных работ.';

-- Комментарии к столбцам
COMMENT ON COLUMN "GraduateWorkStatuses"."Id"          IS 'Уникальный идентификатор статуса ВКР';
COMMENT ON COLUMN "GraduateWorkStatuses"."CodeName"    IS 'Системное значение статуса (для кода), регистронезависимо';
COMMENT ON COLUMN "GraduateWorkStatuses"."DisplayName" IS 'Отображаемое значение статуса (для пользовательского интерфейса)';
COMMENT ON COLUMN "GraduateWorkStatuses"."CreatedAt"   IS 'Дата и время создания записи о статусе';
COMMENT ON COLUMN "GraduateWorkStatuses"."UpdatedAt"   IS 'Дата и время последнего обновления записи о статусе';
