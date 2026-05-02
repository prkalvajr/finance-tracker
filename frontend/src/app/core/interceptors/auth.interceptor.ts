import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { ALREADY_RETRIED, SKIP_AUTH_REFRESH } from '../http/http-context-tokens';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const authedReq = req.clone({ withCredentials: true });

  return next(authedReq).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }

      if (req.context.get(SKIP_AUTH_REFRESH) || req.context.get(ALREADY_RETRIED)) {
        return throwError(() => err);
      }

      return authService.refresh().pipe(
        switchMap(() => {
          const retried = authedReq.clone({
            context: authedReq.context.set(ALREADY_RETRIED, true)
          });
          return next(retried);
        }),
        catchError((refreshErr) => {
          authService.setCurrentUser(null);
          router.navigate(['/login']);
          return throwError(() => refreshErr);
        })
      );
    })
  );
};
