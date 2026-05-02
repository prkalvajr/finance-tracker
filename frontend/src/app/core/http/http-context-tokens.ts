import { HttpContextToken } from '@angular/common/http';

// Tokens consumed by the auth and error-toast interceptors.
//
// SKIP_AUTH_REFRESH — set on /login, /register, /refresh, /logout requests so a
// 401 from those endpoints is not retried via /refresh (avoids infinite loops).
//
// ALREADY_RETRIED — set internally by the auth interceptor on the retry attempt
// after a successful refresh. A second 401 on a request that has already been
// retried logs the user out instead of looping.
//
// SKIP_ERROR_TOAST — set on requests whose component renders error inline
// (login, register, applyServerErrors flow). Suppresses the automatic toast.
export const SKIP_AUTH_REFRESH = new HttpContextToken<boolean>(() => false);
export const ALREADY_RETRIED = new HttpContextToken<boolean>(() => false);
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);
