import type { PagedResult } from './common.models';

export type ApplicationStatusCode =
  | 'Pending'
  | 'ApprovedBySupervisor'
  | 'PendingDepartmentHead'
  | 'ApprovedByDepartmentHead'
  | 'RejectedBySupervisor'
  | 'RejectedByDepartmentHead'
  | 'Cancelled';

export interface ApplicationStatusRefDto {
  id: string;
  codeName: ApplicationStatusCode;
  displayName: string;
}

export interface StudentApplicationDto {
  id: string;
  studentId: string;
  studentFirstName: string;
  studentLastName: string;
  studentGroupName: string;
  topicId: string;
  topicTitle: string;
  supervisorRequestId: string;
  supervisorUserId: string;
  supervisorFirstName: string;
  supervisorLastName: string;
  topicCreatedByUserId: string;
  topicCreatedByEmail: string;
  topicCreatedByFirstName: string;
  topicCreatedByLastName: string;
  status: ApplicationStatusRefDto;
  createdAt: string;
  updatedAt: string | null;
}

export interface ApplicationActionSnapshotDto {
  id: string;
  responsibleId: string;
  responsibleFirstName: string;
  responsibleLastName: string;
  statusCodeName: string;
  statusDisplayName: string;
  comment: string | null;
  createdAt: string;
}

export interface StudentApplicationDetailDto {
  id: string;
  studentId: string;
  studentFirstName: string;
  studentLastName: string;
  studentGroupName: string;
  topicId: string;
  topicTitle: string;
  topicDescription: string | null;
  supervisorRequestId: string | null;
  supervisorUserId: string;
  supervisorFirstName: string;
  supervisorLastName: string;
  supervisorDepartmentId: string | null;
  topicCreatedByUserId: string;
  topicCreatedByFirstName: string;
  topicCreatedByLastName: string;
  topicSupervisorDepartmentId: string | null;
  status: ApplicationStatusRefDto;
  createdAt: string;
  updatedAt: string | null;
  actions: ApplicationActionSnapshotDto[];
}

export interface CreateApplicationCommand {
  topicId?: string;
  proposedTitle?: string;
  proposedDescription?: string;
  supervisorRequestId: string;
}

export interface ApplicationsFilter {
  page: number;
  pageSize: number;
}

export type ApplicationsPagedResult = PagedResult<StudentApplicationDto>;
