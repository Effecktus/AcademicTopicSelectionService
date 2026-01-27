-- Создание таблицы RefreshTokens (Refresh токены для JWT)
-- Зависит от: Users

DROP TABLE IF EXISTS "RefreshTokens" CASCADE;

CREATE TABLE "RefreshTokens" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "Token" TEXT NOT NULL UNIQUE,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT "CK_RefreshTokens_Token_NotEmpty" CHECK (length(btrim("Token")) > 0),
    CONSTRAINT "CK_RefreshTokens_ExpiresAfterCreated" CHECK ("ExpiresAt" > "CreatedAt"),
    
    CONSTRAINT "FK_RefreshTokens_Users"
        FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id")
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- Комментарии к таблице
COMMENT ON TABLE "RefreshTokens" IS 'Таблица refresh токенов для JWT аутентификации. Содержит информацию о токенах обновления доступа пользователей, их сроке действия и статусе отзыва.';

-- Комментарии к столбцам
COMMENT ON COLUMN "RefreshTokens"."Id" IS 'Уникальный идентификатор refresh токена';
COMMENT ON COLUMN "RefreshTokens"."UserId" IS 'Идентификатор пользователя, которому принадлежит токен (внешний ключ к таблице Users)';
COMMENT ON COLUMN "RefreshTokens"."Token" IS 'Значение refresh токена (уникальное)';
COMMENT ON COLUMN "RefreshTokens"."ExpiresAt" IS 'Дата и время истечения срока действия токена';
COMMENT ON COLUMN "RefreshTokens"."CreatedAt" IS 'Дата и время создания токена';
COMMENT ON COLUMN "RefreshTokens"."IsRevoked" IS 'Флаг отзыва токена (true - отозван, false - активен)';
