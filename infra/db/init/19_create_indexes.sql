-- Индексы для ускорения типовых запросов
-- Выполняется после создания таблиц

-- Users
CREATE INDEX IF NOT EXISTS "IX_Users_RoleId" ON "Users" ("RoleId");
CREATE INDEX IF NOT EXISTS "IX_Users_DepartmentId" ON "Users" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_Users_IsActive" ON "Users" ("IsActive");

-- Teachers / Students
CREATE INDEX IF NOT EXISTS "IX_Teachers_AcademicDegreeId" ON "Teachers" ("AcademicDegreeId");
CREATE INDEX IF NOT EXISTS "IX_Teachers_AcademicTitleId" ON "Teachers" ("AcademicTitleId");
CREATE INDEX IF NOT EXISTS "IX_Teachers_PositionId" ON "Teachers" ("PositionId");
CREATE INDEX IF NOT EXISTS "IX_Students_Group" ON "Students" ("Group");

-- Topics
CREATE INDEX IF NOT EXISTS "IX_Topics_TeacherId" ON "Topics" ("TeacherId");
CREATE INDEX IF NOT EXISTS "IX_Topics_StatusId" ON "Topics" ("StatusId");
CREATE INDEX IF NOT EXISTS "IX_Topics_Year" ON "Topics" ("Year");

-- Applications
CREATE INDEX IF NOT EXISTS "IX_Applications_StudentId" ON "Applications" ("StudentId");
CREATE INDEX IF NOT EXISTS "IX_Applications_TopicId" ON "Applications" ("TopicId");
CREATE INDEX IF NOT EXISTS "IX_Applications_StatusId" ON "Applications" ("StatusId");
CREATE INDEX IF NOT EXISTS "IX_Applications_CreatedAt" ON "Applications" ("CreatedAt");

-- ChatMessages
CREATE INDEX IF NOT EXISTS "IX_ChatMessages_ApplicationId" ON "ChatMessages" ("ApplicationId");
CREATE INDEX IF NOT EXISTS "IX_ChatMessages_SentAt" ON "ChatMessages" ("SentAt");

-- Notifications
CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId" ON "Notifications" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_IsRead" ON "Notifications" ("UserId", "IsRead");
CREATE INDEX IF NOT EXISTS "IX_Notifications_CreatedAt" ON "Notifications" ("CreatedAt");

-- GraduateWorks
CREATE INDEX IF NOT EXISTS "IX_GraduateWorks_StudentId" ON "GraduateWorks" ("StudentId");
CREATE INDEX IF NOT EXISTS "IX_GraduateWorks_TeacherId" ON "GraduateWorks" ("TeacherId");
CREATE INDEX IF NOT EXISTS "IX_GraduateWorks_Year" ON "GraduateWorks" ("Year");

-- RefreshTokens
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_ExpiresAt" ON "RefreshTokens" ("ExpiresAt");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId_Active" ON "RefreshTokens" ("UserId") WHERE "IsRevoked" = FALSE;

