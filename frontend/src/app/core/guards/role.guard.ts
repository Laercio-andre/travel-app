import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/auth.models';

export const roleGuard: CanActivateFn = (route): boolean | UrlTree => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const roles = (route.data['roles'] ?? []) as UserRole[];

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/auth/login']);
  }

  return roles.length === 0 || auth.hasRole(roles) ? true : router.createUrlTree(['/dashboard']);
};
