/** Ответ `POST /auth/login` и `POST /auth/refresh` (camelCase от ASP.NET Core). */
export interface AccessTokenDto {
  accessToken: string;
  fullName: string;
  userId: string;
  email: string;
  role: Role;
}

export interface UserInfo {
  fullName: string;
  userId: string;
  email: string;
  role: Role;
}

export type Role = 'Student' | 'Teacher' | 'DepartmentHead' | 'Admin';