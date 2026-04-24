import type { DictionaryItemRef } from './common.models';

export interface SupervisorRequestDto {
  id: string;
  studentId: string;
  studentFirstName: string;
  studentLastName: string;
  teacherUserId: string;
  teacherFirstName: string;
  teacherLastName: string;
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
  studentGroupName: string;
  teacherUserId: string;
  teacherFirstName: string;
  teacherLastName: string;
  teacherEmail: string;
  status: DictionaryItemRef;
  comment: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface SupervisorRequestsFilter {
  page: number;
  pageSize: number;
}
