import type { DictionaryItemRef } from './common.models';

export interface TeacherDto {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  middleName: string | null;
  departmentDisplayName: string | null;
  maxStudentsLimit: number | null;
  academicDegree: DictionaryItemRef;
  academicTitle: DictionaryItemRef;
  position: DictionaryItemRef;
  createdAt: string;
  updatedAt: string | null;
}
