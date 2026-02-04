-- Создание таблицы Notifications (Уведомления)
-- Зависит от: Users, NotificationTypes

DROP TABLE IF EXISTS "Notifications" CASCADE;

CREATE TABLE "Notifications" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "TypeId" UUID NOT NULL,
    "Title" TEXT NOT NULL,
    "Content" TEXT NOT NULL,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT "CK_Notifications_Title_NotEmpty" CHECK (length(btrim("Title")) > 0),
    CONSTRAINT "CK_Notifications_Content_NotEmpty" CHECK (length(btrim("Content")) > 0),

    CONSTRAINT "FK_Notifications_Users"
        FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
        
    CONSTRAINT "FK_Notifications_NotificationTypes"
        FOREIGN KEY ("TypeId")
        REFERENCES "NotificationTypes"("Id")
        ON DELETE RESTRICT 
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "Notifications" IS 'Таблица уведомлений пользователей системы. Содержит информацию о различных типах уведомлений и их статусе прочтения.';

-- Комментарии к столбцам
COMMENT ON COLUMN "Notifications"."Id" IS 'Уникальный идентификатор уведомления';
COMMENT ON COLUMN "Notifications"."UserId" IS 'Идентификатор пользователя-получателя уведомления (внешний ключ к таблице Users)';
COMMENT ON COLUMN "Notifications"."TypeId" IS 'Идентификатор типа уведомления (внешний ключ к таблице NotificationTypes)';
COMMENT ON COLUMN "Notifications"."Title" IS 'Заголовок уведомления';
COMMENT ON COLUMN "Notifications"."Content" IS 'Содержимое уведомления (полный текст)';
COMMENT ON COLUMN "Notifications"."IsRead" IS 'Флаг прочтения уведомления (true - прочитано, false - не прочитано)';
COMMENT ON COLUMN "Notifications"."CreatedAt" IS 'Дата и время создания уведомления';