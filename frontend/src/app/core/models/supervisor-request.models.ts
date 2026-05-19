import type { DictionaryItemRef } from './common.models';

export interface SupervisorRequestDto {
  id: string;
  studentId: string;
  studentFirstName: string;
  studentLastName: string;
  studentMiddleName: string | null;
  teacherUserId: string;
  teacherFirstName: string;
  teacherLastName: string;
  teacherMiddleName: string | null;
  status: DictionaryItemRef;
  comment: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface SupervisorRequestDetailDto {
  id: string;
  studentId: string;
  studentFirstName: string;
  studentLastName: string;
  studentMiddleName: string | null;
  studentGroupName: string;
  teacherUserId: string;
  teacherFirstName: string;
  teacherLastName: string;
  teacherMiddleName: string | null;
  teacherEmail: string;
  status: DictionaryItemRef;
  comment: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface SupervisorRequestsFilter {
  page: number;
  pageSize: number;
  sort?: string;
  createdFromUtc?: string;
  createdToUtc?: string;
}
