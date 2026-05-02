import { HttpClient, HttpContext } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, catchError, finalize, firstValueFrom, of, shareReplay, tap, throwError } from 'rxjs';

import { LoginRequest, RegisterRequest } from '../../models/auth.models';
import { UserDto } from '../../models/user.models';
import { API_BASE_URL } from '../http/api-config';
import { SKIP_AUTH_REFRESH, SKIP_ERROR_TOAST } from '../http/http-context-tokens';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);

  private readonly _currentUser = signal<UserDto | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  // In-flight refresh observable, shared across concurrent 401 callers so the
  // backend (which rotates refresh tokens on each /refresh) only sees one call.
  private refresh$: Observable<void> | null = null;

  private skipRefreshContext(): HttpContext {
    return new HttpContext().set(SKIP_AUTH_REFRESH, true);
  }

  // Used by login/register so credential errors are rendered inline by the
  // page (via applyServerErrors) instead of as a snack bar toast.
  private inlineErrorContext(): HttpContext {
    return new HttpContext().set(SKIP_AUTH_REFRESH, true).set(SKIP_ERROR_TOAST, true);
  }

  async bootstrap(): Promise<void> {
    try {
      const user = await firstValueFrom(
        this.http.get<UserDto>(`${this.apiBase}/user`, {
          withCredentials: true,
          context: this.skipRefreshContext()
        })
      );
      this._currentUser.set(user);
    } catch {
      // 401 (no session yet, expired refresh token, etc.) → stay anonymous.
      this._currentUser.set(null);
    }
  }

  async login(req: LoginRequest): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${this.apiBase}/login`, req, {
        withCredentials: true,
        context: this.inlineErrorContext()
      })
    );
    const user = await firstValueFrom(
      this.http.get<UserDto>(`${this.apiBase}/user`, { withCredentials: true })
    );
    this._currentUser.set(user);
  }

  async register(req: RegisterRequest): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${this.apiBase}/register`, req, {
        withCredentials: true,
        context: this.inlineErrorContext()
      })
    );
    const user = await firstValueFrom(
      this.http.get<UserDto>(`${this.apiBase}/user`, { withCredentials: true })
    );
    this._currentUser.set(user);
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post<void>(`${this.apiBase}/logout`, {}, {
          withCredentials: true,
          context: this.skipRefreshContext()
        })
      );
    } finally {
      this._currentUser.set(null);
    }
  }

  refresh(): Observable<void> {
    if (this.refresh$) {
      return this.refresh$;
    }

    this.refresh$ = this.http
      .post<void>(`${this.apiBase}/refresh`, {}, {
        withCredentials: true,
        context: this.skipRefreshContext()
      })
      .pipe(
        catchError((err) => {
          this._currentUser.set(null);
          return throwError(() => err);
        }),
        finalize(() => {
          this.refresh$ = null;
        }),
        shareReplay(1)
      );

    return this.refresh$;
  }

  setCurrentUser(user: UserDto | null): void {
    this._currentUser.set(user);
  }
}
