import { isStatusBlockingNewApplication } from './application-create-eligibility';

describe('isStatusBlockingNewApplication', () => {
  it('возвращает false для терминальных статусов, когда новая заявка разрешена', () => {
    expect(isStatusBlockingNewApplication('RejectedBySupervisor')).toBeFalse();
    expect(isStatusBlockingNewApplication('RejectedByDepartmentHead')).toBeFalse();
    expect(isStatusBlockingNewApplication('Cancelled')).toBeFalse();
  });

  it('возвращает true для активных статусов', () => {
    expect(isStatusBlockingNewApplication('Pending')).toBeTrue();
    expect(isStatusBlockingNewApplication('PendingDepartmentHead')).toBeTrue();
    expect(isStatusBlockingNewApplication('ApprovedBySupervisor')).toBeTrue();
    expect(isStatusBlockingNewApplication('ApprovedByDepartmentHead')).toBeTrue();
  });
});
