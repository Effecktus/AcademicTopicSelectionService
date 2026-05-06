import type { ApplicationStatusCode } from '../models/application.models';

/**
 * Статусы, при которых студент может подать новую заявку (см. NonBlockingStatusesForCreate в StudentApplicationsRepository).
 */
const NON_BLOCKING_STATUSES_FOR_NEW_APPLICATION = new Set<ApplicationStatusCode>([
  'RejectedBySupervisor',
  'RejectedByDepartmentHead',
  'Cancelled',
]);

/** Заявка в этом статусе не даёт создать новую (бэкенд: StudentHasActiveApplicationAsync). */
export function isStatusBlockingNewApplication(status: ApplicationStatusCode): boolean {
  return !NON_BLOCKING_STATUSES_FOR_NEW_APPLICATION.has(status);
}
