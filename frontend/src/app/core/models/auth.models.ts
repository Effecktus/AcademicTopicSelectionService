/** Ответ `POST /auth/login` и `POST /auth/refresh` (camelCase от ASP.NET Core). */
export interface AccessTokenDto {
  accessToken: string;
  userId: string;
  email: string;
  role: string;
}

export interface UserInfo {
  userId: string;
  email: string;
  role: string;
}
