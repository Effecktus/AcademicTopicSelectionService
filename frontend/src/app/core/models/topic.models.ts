import type { DictionaryItemRef } from './common.models';

export interface TopicDto {
  id: string;
  title: string;
  description: string | null;
  status: DictionaryItemRef;
  creatorType: DictionaryItemRef;
  createdByUserId: string;
  createdByEmail: string;
  createdByFirstName: string;
  createdByLastName: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface TopicsFilter {
  query?: string;
  statusCodeName?: string;
  createdByUserId?: string;
  creatorTypeCodeName?: string;
  sort?: 'createdAtDesc' | 'createdAtAsc' | 'titleAsc' | 'titleDesc';
  page: number;
  pageSize: number;
}

export interface CreateTopicCommand {
  title: string;
  description: string | null;
  creatorTypeCodeName: 'Teacher' | 'Student';
  statusCodeName?: 'Active' | 'Inactive' | null;
}

export interface UpdateTopicCommand {
  title?: string | null;
  description?: string | null;
  statusCodeName?: string | null;
}
