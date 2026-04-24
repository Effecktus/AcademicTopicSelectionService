import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../auth/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const roleData = route.data['role'] as string | string[] | undefined;
  if (!roleData) {
    return true;
  }

  const auth = inject(AuthService);
  const router = inject(Router);
  const currentRole = auth.role();
  const allowedRoles = Array.isArray(roleData) ? roleData : [roleData];

  return currentRole !== null && allowedRoles.includes(currentRole)
    ? true
    : router.createUrlTree(['/']);
};
