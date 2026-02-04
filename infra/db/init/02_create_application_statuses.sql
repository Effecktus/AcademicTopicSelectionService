-- Создание справочника статусов заявки.

DROP TABLE IF EXISTS "ApplicationStatuses" CASCADE;

CREATE TABLE "ApplicationStatuses" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" CITEXT NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_ApplicationStatuses_Name_NotEmpty" CHECK (length(btrim("Name"::text)) > 0),
    CONSTRAINT "CK_ApplicationStatuses_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Вставка начальных данных.
INSERT INTO "ApplicationStatuses" ("Name", "DisplayName") VALUES
('Pending', 'Ожидает ответа преподавателя'),
('ApprovedBySupervisor', 'Одобрено преподавателем'),
('RejectedBySupervisor', 'Отклонено преподавателем'),
('PendingDepartmentHead', 'Отправлено заведующему кафедрой'),
('ApprovedByDepartmentHead', 'Утверждено заведующим кафедрой'),
('RejectedByDepartmentHead', 'Отклонено заведующим кафедрой'),
('Cancelled', 'Отменено студентом');

-- Комментарии к таблице
COMMENT ON TABLE "ApplicationStatuses" IS 'Справочник статусов заявок на темы ВКР. Содержит системные и отображаемые названия статусов.';

-- Комментарии к столбцам
COMMENT ON COLUMN "ApplicationStatuses"."Id" IS 'Уникальный идентификатор статуса заявки';
COMMENT ON COLUMN "ApplicationStatuses"."Name" IS 'Системное значение статуса (для кода), регистронезависимо';
COMMENT ON COLUMN "ApplicationStatuses"."DisplayName" IS 'Отображаемое значение статуса (для пользовательского интерфейса)';
COMMENT ON COLUMN "ApplicationStatuses"."CreatedAt" IS 'Дата и время создания записи о статусе';
COMMENT ON COLUMN "ApplicationStatuses"."UpdatedAt" IS 'Дата и время последнего обновления записи о статусе';