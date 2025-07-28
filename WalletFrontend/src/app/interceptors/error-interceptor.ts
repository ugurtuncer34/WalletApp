import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast-service';
import { AuthService } from '../services/auth-service';
import { Router } from '@angular/router';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      let msg = 'Unexpected error';

      if (req.url.endsWith('/auth/login') && err.status === 401) {
        msg = 'Login failed: wrong email or password.';
      } else if (typeof err.error === 'string') {
        msg = err.error;                         // plain string from API (BadRequest etc.)
      } else if (err.error?.title) {
        msg = err.error.title;
      } else if (err.status === 401) {
        msg = 'Unauthorized. Please log in again.';
        auth.logout();
        router.navigate(['/login']);
      } else if (err.status >= 500) {
        msg = 'Server error. Please try later.';
      } else {
        msg = err.message;
      }

      toast.show(msg);
      return throwError(() => err);
    })
  );
};