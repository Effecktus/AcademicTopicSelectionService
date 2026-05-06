import type { ApplicationStatusCode } from '../models/application.models';

export const APPLICATION_STATUS_BADGE_CLASS: Record<ApplicationStatusCode, string> = {
  OnEditing: 'status-editing',
  Pending: 'status-pending',
  ApprovedBySupervisor: 'status-approved',
  PendingDepartmentHead: 'status-pending-head',
  ApprovedByDepartmentHead: 'status-success',
  RejectedBySupervisor: 'status-rejected',
  RejectedByDepartmentHead: 'status-rejected',
  Cancelled: 'status-cancelled',
};
