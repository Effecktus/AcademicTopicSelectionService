-- Создание справочника статусов заяки.

DROP TABLE IF EXISTS"ApplicationStatuses" CASCADE;

CREATE TABLE "ApplicationStatuses" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "DisplayName" varchar(100) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL
);

-- Вставка начальных данных.
INSERT INTO "ApplicationStatuses" ("Name", "DisplayName") VALUES
('ApplicationStatusChanged', 'Статус заявки изменен'),
('NewMessage', 'Новое сообщение'),
('TopicApproved', 'Тема утверждена'),
('TopicRejected', 'Тема отклонена');

-- Комментарии.
COMMENT ON TABLE "ApplicationStatuses" IS 'Справочник статусов заяки';
COMMENT ON COLUMN "ApplicationStatuses"."Name" IS 'Системное значение';
COMMENT ON COLUMN "ApplicationStatuses"."DisplayName" IS 'Отображаемое значение';
COMMENT ON COLUMN "ApplicationStatuses"."CreatedAt" IS 'Дата создания записи';
COMMENT ON COLUMN "ApplicationStatuses"."UpdatedAt" IS 'Дата последнего обновления записи';