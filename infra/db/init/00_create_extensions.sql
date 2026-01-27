-- Создание расширений PostgreSQL, необходимых проекту
-- Этот скрипт должен выполняться ПЕРЕД созданием таблиц, использующих CITEXT

-- gen_random_uuid(), gen_random_bytes()
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE EXTENSION IF NOT EXISTS citext;

COMMENT ON EXTENSION pgcrypto IS 'Криптографические функции (gen_random_uuid, gen_random_bytes и др.)';
COMMENT ON EXTENSION citext IS 'Добавляет тип CITEXT для регистронезависимого сравнения строк (Email, Name в справочниках и т.п.)';
