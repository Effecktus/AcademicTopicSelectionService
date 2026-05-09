export type GraduateWorkFileType = 'thesis' | 'presentation';

export interface GraduateWorkDto {
  id: string;
  applicationId: string;
  studentId: string;
  teacherId: string;
  title: string;
  year: number;
  grade: number;
  commissionMembers: string;
  hasFile: boolean;
  hasPresentation: boolean;
  createdAt: string;
  updatedAt: string | null;
  fileName: string | null;
  presentationFileName: string | null;
  studentFullName: string;
  teacherFullName: string;
}

export interface FileUrlDto {
  url: string;
  expiresAt: string;
}

export interface GraduateWorksFilter {
  page: number;
  pageSize: number;
  year?: number | null;
  titleQuery?: string | null;
  teacherId?: string | null;
  teacherQuery?: string | null;
}
