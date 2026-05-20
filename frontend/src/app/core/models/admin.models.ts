export interface UserListItemDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  middleName: string | null;
  roleCodeName: string;
  roleDisplayName: string;
  departmentId: string | null;
  departmentDisplayName: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface DepartmentDto {
  id: string;
  codeName: string;
  displayName: string;
}

export interface UserRoleDto {
  id: string;
  codeName: string;
  displayName: string;
}

export interface CreateUserBody {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  middleName?: string | null;
  roleId: string;
  departmentId?: string | null;
}

export interface AdminAnalyticsDto {
  summary: AdminSummaryDto;
  applicationsByStatus: StatusCountDto[];
  gwByYear: YearCountDto[];
  applicationsByDepartment: DepartmentCountDto[];
  applicationsByMonth: MonthCountDto[];
  topTeachersByApplications: TeacherCountDto[];
}

export interface AdminSummaryDto {
  totalApplications: number;
  totalGraduateWorks: number;
  totalUsers: number;
}

export interface StatusCountDto {
  statusCode: string;
  statusDisplayName: string;
  count: number;
}

export interface YearCountDto {
  year: number;
  count: number;
}

export interface DepartmentCountDto {
  departmentName: string;
  count: number;
}

export interface MonthCountDto {
  month: number;
  count: number;
}

export interface TeacherCountDto {
  teacherFullName: string;
  departmentName: string | null;
  count: number;
}

export interface CreateGwBody {
  applicationId: string;
  title: string;
  year: number;
  grade?: number | null;
  commissionMembers?: string | null;
}

export interface UpdateGwBody {
  title: string;
  year: number;
  grade: number | null;
  commissionMembers: string | null;
}
