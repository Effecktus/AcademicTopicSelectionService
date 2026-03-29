-- Создание справочника типов уведомлений

DROP TABLE IF EXISTS "NotificationTypes" CASCADE;

CREATE TABLE "NotificationTypes" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CodeName" CITEXT NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "CK_NotificationTypes_CodeName_NotEmpty" CHECK (length(btrim("CodeName"::text)) > 0),
    CONSTRAINT "CK_NotificationTypes_DisplayName_NotEmpty" CHECK (length(btrim("DisplayName")) > 0)
);

-- Вставка начальных данных
INSERT INTO "NotificationTypes" ("CodeName", "DisplayName") VALUES
('ApplicationStatusChanged', 'Статус заявки изменен'),
('NewMessage', 'Новое сообщение'),
('TopicApproved', 'Тема утверждена'),
('TopicRejected', 'Тема отклонена');

-- Комментарии к таблице
COMMENT ON TABLE "NotificationTypes" IS 'Справочник типов уведомлений системы. Содержит системные и отображаемые названия типов.';

-- Комментарии к столбцам
COMMENT ON COLUMN "NotificationTypes"."Id" IS 'Уникальный идентификатор типа уведомления';
COMMENT ON COLUMN "NotificationTypes"."CodeName" IS 'Системное значение типа (для кода), регистронезависимо';
COMMENT ON COLUMN "NotificationTypes"."DisplayName" IS 'Отображаемое значение типа (для пользовательского интерфейса)';
COMMENT ON COLUMN "NotificationTypes"."CreatedAt" IS 'Дата и время создания записи о типе уведомления';
COMMENT ON COLUMN "NotificationTypes"."UpdatedAt" IS 'Дата и время последнего обновления записи о типе уведомления';