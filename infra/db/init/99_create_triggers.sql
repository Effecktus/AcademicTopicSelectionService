-- Функция и триггеры для автоматического обновления UpdatedAt
-- Используется для всех таблиц с полем UpdatedAt
-- Выполняется после создания всех таблиц

-- Функция для автоматического обновления UpdatedAt
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION update_updated_at_column() IS 'Функция для автоматического обновления поля UpdatedAt при изменении записи';

-- Триггер для таблицы Users
DROP TRIGGER IF EXISTS update_users_updated_at ON "Users";
CREATE TRIGGER update_users_updated_at
    BEFORE UPDATE ON "Users"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы Departments
DROP TRIGGER IF EXISTS update_departments_updated_at ON "Departments";
CREATE TRIGGER update_departments_updated_at
    BEFORE UPDATE ON "Departments"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы Teachers
DROP TRIGGER IF EXISTS update_teachers_updated_at ON "Teachers";
CREATE TRIGGER update_teachers_updated_at
    BEFORE UPDATE ON "Teachers"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы Students
DROP TRIGGER IF EXISTS update_students_updated_at ON "Students";
CREATE TRIGGER update_students_updated_at
    BEFORE UPDATE ON "Students"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы Topics
DROP TRIGGER IF EXISTS update_topics_updated_at ON "Topics";
CREATE TRIGGER update_topics_updated_at
    BEFORE UPDATE ON "Topics"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы Applications
DROP TRIGGER IF EXISTS update_applications_updated_at ON "Applications";
CREATE TRIGGER update_applications_updated_at
    BEFORE UPDATE ON "Applications"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы GraduateWorks
DROP TRIGGER IF EXISTS update_graduate_works_updated_at ON "GraduateWorks";
CREATE TRIGGER update_graduate_works_updated_at
    BEFORE UPDATE ON "GraduateWorks"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггеры для справочников

-- Триггер для таблицы UserRoles
DROP TRIGGER IF EXISTS update_user_roles_updated_at ON "UserRoles";
CREATE TRIGGER update_user_roles_updated_at
    BEFORE UPDATE ON "UserRoles"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы ApplicationStatuses
DROP TRIGGER IF EXISTS update_application_statuses_updated_at ON "ApplicationStatuses";
CREATE TRIGGER update_application_statuses_updated_at
    BEFORE UPDATE ON "ApplicationStatuses"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы TopicStatuses
DROP TRIGGER IF EXISTS update_topic_statuses_updated_at ON "TopicStatuses";
CREATE TRIGGER update_topic_statuses_updated_at
    BEFORE UPDATE ON "TopicStatuses"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы NotificationTypes
DROP TRIGGER IF EXISTS update_notification_types_updated_at ON "NotificationTypes";
CREATE TRIGGER update_notification_types_updated_at
    BEFORE UPDATE ON "NotificationTypes"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы AcademicDegrees
DROP TRIGGER IF EXISTS update_academic_degrees_updated_at ON "AcademicDegrees";
CREATE TRIGGER update_academic_degrees_updated_at
    BEFORE UPDATE ON "AcademicDegrees"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы AcademicTitles
DROP TRIGGER IF EXISTS update_academic_titles_updated_at ON "AcademicTitles";
CREATE TRIGGER update_academic_titles_updated_at
    BEFORE UPDATE ON "AcademicTitles"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Триггер для таблицы Positions
DROP TRIGGER IF EXISTS update_positions_updated_at ON "Positions";
CREATE TRIGGER update_positions_updated_at
    BEFORE UPDATE ON "Positions"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
