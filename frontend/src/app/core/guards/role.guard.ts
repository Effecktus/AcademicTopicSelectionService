import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../auth/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const requiredRole = route.data['role'] as string | undefined;
  if (!requiredRole) {
    return true;
  }

  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.role() === requiredRole ? true : router.createUrlTree(['/']);
};
