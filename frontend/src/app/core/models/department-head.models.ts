export interface DepartmentHeadAnalyticsDto {
  summary: DhSummaryDto;
  applicationsByStatus: DhStatusCountDto[];
  gwByYear: DhYearCountDto[];
  teacherWorkload: DhTeacherWorkloadDto[];
}

export interface DhSummaryDto {
  totalTopics: number;
  totalStudents: number;
  totalApplications: number;
  totalGraduateWorks: number;
}

export interface DhStatusCountDto {
  statusCode: string;
  statusDisplayName: string;
  count: number;
}

export interface DhYearCountDto {
  year: number;
  count: number;
}

export interface DhTeacherWorkloadDto {
  teacherFullName: string;
  activeStudentsCount: number;
  maxStudentsLimit: number | null;
}
