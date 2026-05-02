import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { ProblemDetails } from '../../models/problem-details.models';
import { NotificationService } from '../services/notification.service';
import { SKIP_ERROR_TOAST } from '../http/http-context-tokens';

const FALLBACK_MESSAGE = 'Something went wrong. Please try again.';

function extractMessage(err: HttpErrorResponse): string {
  const body = err.error as ProblemDetails | string | null | undefined;

  if (body && typeof body === 'object') {
    if (body.errors && Object.keys(body.errors).length > 0) {
      const all = Object.values(body.errors).flat();
      if (all.length > 0) {
        return all.join(' ');
      }
    }
    if (typeof body.detail === 'string' && body.detail.length > 0) {
      return body.detail;
    }
    if (typeof body.title === 'string' && body.title.length > 0) {
      return body.title;
    }
  }

  if (typeof body === 'string' && body.length > 0) {
    return body;
  }

  return err.message || FALLBACK_MESSAGE;
}

export const errorToastInterceptor: HttpInterceptorFn = (req, next) => {
  const notification = inject(NotificationService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse)) {
        return throwError(() => err);
      }

      // 401 is handled by the auth interceptor (refresh or redirect).
      // SKIP_ERROR_TOAST is set when the page renders the error inline.
      if (err.status === 401 || req.context.get(SKIP_ERROR_TOAST)) {
        return throwError(() => err);
      }

      notification.error(extractMessage(err));
      return throwError(() => err);
    })
  );
};
