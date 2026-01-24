-- Создание всех ENUM типов для базы данных
-- Этот скрипт должен выполняться ПЕРВЫМ

-- Роли пользователей
DROP TYPE IF EXISTS user_role CASCADE;
CREATE TYPE user_role AS ENUM (
    'Student',
    'Teacher',
    'DepartmentHead',
    'Admin'
);
COMMENT ON TYPE user_role IS 'Роли пользователей в системе';

-- Статусы заявок (для Applications)
DROP TYPE IF EXISTS application_status CASCADE;
CREATE TYPE application_status AS ENUM (
    'Pending',
    'ApprovedBySupervisor',
    'RejectedBySupervisor',
    'PendingDepartmentHead',
    'ApprovedByDepartmentHead',
    'RejectedByDepartmentHead',
    'Cancelled'
);
COMMENT ON TYPE application_status IS 'Статусы заявок студентов на выбор темы ВКР';

-- Статусы тем (для Topics)
DROP TYPE IF EXISTS topic_status CASCADE;
CREATE TYPE topic_status AS ENUM (
    'Active',
    'Inactive'
);
COMMENT ON TYPE topic_status IS 'Статус темы ВКР: Active - активна, Inactive - неактивна';

-- Типы уведомлений (для Notifications)
DROP TYPE IF EXISTS notification_type CASCADE;
CREATE TYPE notification_type AS ENUM (
    'ApplicationStatusChanged',
    'NewMessage',
    'TopicApproved',
    'TopicRejected'
);
COMMENT ON TYPE notification_type IS 'Типы уведомлений в системе';
