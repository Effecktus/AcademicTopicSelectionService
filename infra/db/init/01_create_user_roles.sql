-- Создание справочника ролей пользователей

DROP TABLE IF EXISTS "UserRoles" CASCADE;

CREATE TABLE "UserRoles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(50) NOT NULL UNIQUE,      -- Системное имя (для кода)
    "DisplayName" VARCHAR(100) NOT NULL,     -- Отображаемое имя (для UI)
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL
);

-- Вставка начальных значений
INSERT INTO "UserRoles" ("Name", "DisplayName") VALUES
('Student', 'Студент'),
('Teacher', 'Преподаватель'),
('DepartmentHead', 'Заведующий кафедрой'),
('Admin', 'Администратор');

-- Комментарии
COMMENT ON TABLE "UserRoles" IS 'Справочник ролей пользователей';
COMMENT ON COLUMN "UserRoles"."Name" IS 'Системное значение';
COMMENT ON COLUMN "UserRoles"."DisplayName" IS 'Отображаемое значение';
COMMENT ON COLUMN "UserRoles"."CreatedAt" IS 'Дата создания записи';
COMMENT ON COLUMN "UserRoles"."UpdatedAt" IS 'Дата последнего обновления записи';
