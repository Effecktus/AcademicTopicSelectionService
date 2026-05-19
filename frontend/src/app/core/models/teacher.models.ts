import type { DictionaryItemRef } from './common.models';

export interface TeacherGraduateWorkDto {
  id: string;
  title: string;
  year: number;
  grade: number;
  studentLastName: string;
  studentFirstName: string;
  studentMiddleName: string | null;
  hasThesis: boolean;
  hasPresentation: boolean;
}

export interface TeacherDto {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  middleName: string | null;
  departmentDisplayName: string | null;
  maxStudentsLimit: number | null;
  graduateWorksCount: number;
  occupiedSlotsCount: number | null;
  academicDegree: DictionaryItemRef;
  academicTitle: DictionaryItemRef;
  position: DictionaryItemRef;
  createdAt: string;
  updatedAt: string | null;
}
