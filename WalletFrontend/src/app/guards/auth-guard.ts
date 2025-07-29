import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth-service';

export const authGuard: CanActivateFn = (_route, state): boolean | UrlTree => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // logged in?
  if (auth.token) {
    return true;
  }

  // not logged in → redirect - preserve intended URL
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};
