export interface NotificationDto {
  id: string;
  typeCodeName: string;
  typeDisplayName: string;
  title: string;
  content: string;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationsFilter {
  isRead?: boolean;
  page: number;
  pageSize: number;
}
