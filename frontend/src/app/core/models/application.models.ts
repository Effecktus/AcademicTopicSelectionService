import type { PagedResult } from './common.models';

export type ApplicationStatusCode =
  | 'OnEditing'
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
  studentMiddleName: string | null;
  studentGroupName: string;
  topicId: string;
  topicTitle: string;
  supervisorRequestId: string;
  supervisorUserId: string;
  supervisorFirstName: string;
  supervisorLastName: string;
  supervisorMiddleName: string | null;
  topicCreatedByUserId: string;
  topicCreatedByEmail: string;
  topicCreatedByFirstName: string;
  topicCreatedByLastName: string;
  topicCreatedByMiddleName: string | null;
  status: ApplicationStatusRefDto;
  createdAt: string;
  updatedAt: string | null;
}

export interface ApplicationActionSnapshotDto {
  id: string;
  responsibleId: string;
  responsibleFirstName: string;
  responsibleLastName: string;
  responsibleMiddleName: string | null;
  statusCodeName: string;
  statusDisplayName: string;
  comment: string | null;
  createdAt: string;
}

export interface ApplicationTopicChangeHistoryEntryDto {
  id: string;
  changedByUserId: string;
  changedByFirstName: string;
  changedByLastName: string;
  changedByMiddleName: string | null;
  changeKind: string;
  changeKindDisplayName: string;
  newValue: string | null;
  createdAt: string;
}

export interface ChatMessageDto {
  id: string;
  applicationId?: string;
  senderId: string;
  senderFullName: string;
  content: string;
  sentAt: string;
  readAt?: string | null;
}

export interface StudentApplicationDetailDto {
  id: string;
  studentId: string;
  studentFirstName: string;
  studentLastName: string;
  studentMiddleName: string | null;
  studentGroupName: string;
  topicId: string;
  topicTitle: string;
  topicDescription: string | null;
  supervisorRequestId: string | null;
  supervisorUserId: string;
  supervisorFirstName: string;
  supervisorLastName: string;
  supervisorMiddleName: string | null;
  supervisorDepartmentId: string | null;
  topicCreatedByUserId: string;
  topicCreatedByFirstName: string;
  topicCreatedByLastName: string;
  topicCreatedByMiddleName: string | null;
  topicSupervisorDepartmentId: string | null;
  status: ApplicationStatusRefDto;
  createdAt: string;
  updatedAt: string | null;
  actions: ApplicationActionSnapshotDto[];
  topicChangeHistory: ApplicationTopicChangeHistoryEntryDto[];
}

export interface CreateApplicationCommand {
  topicId?: string;
  proposedTitle?: string;
  proposedDescription?: string;
  supervisorRequestId: string;
}

export interface UpdateApplicationTopicBody {
  title: string;
  description?: string | null;
}

export interface ApplicationsFilter {
  page: number;
  pageSize: number;
  query?: string | null;
}

export type ApplicationsPagedResult = PagedResult<StudentApplicationDto>;
