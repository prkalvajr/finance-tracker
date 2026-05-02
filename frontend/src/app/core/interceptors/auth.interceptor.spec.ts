import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { firstValueFrom, Observable, of } from 'rxjs';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { ALREADY_RETRIED, SKIP_AUTH_REFRESH } from '../http/http-context-tokens';

const TARGET = 'http://api.test/data';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let auth: { refresh: ReturnType<typeof vi.fn>; setCurrentUser: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    auth = {
      refresh: vi.fn(() => of(undefined)),
      setCurrentUser: vi.fn()
    };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('passes 200 responses straight through', async () => {
    const promise = firstValueFrom(http.get<{ ok: boolean }>(TARGET));
    httpMock.expectOne(TARGET).flush({ ok: true });
    expect(await promise).toEqual({ ok: true });
    expect(auth.refresh).not.toHaveBeenCalled();
  });

  it('attaches withCredentials to every request', () => {
    http.get(TARGET).subscribe({ next: () => undefined, error: () => undefined });
    const req = httpMock.expectOne(TARGET);
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('on 401 calls refresh, then retries the original request once', async () => {
    const promise = firstValueFrom(http.get<{ ok: boolean }>(TARGET));

    httpMock
      .expectOne(TARGET)
      .flush({ title: 'expired' }, { status: 401, statusText: 'Unauthorized' });

    expect(auth.refresh).toHaveBeenCalledTimes(1);

    const retry = httpMock.expectOne(TARGET);
    expect(retry.request.context.get(ALREADY_RETRIED)).toBe(true);
    retry.flush({ ok: true });

    expect(await promise).toEqual({ ok: true });
  });

  it('does not retry when SKIP_AUTH_REFRESH is set', async () => {
    const ctx = new HttpContext().set(SKIP_AUTH_REFRESH, true);
    const promise = firstValueFrom(http.get(TARGET, { context: ctx })).catch((e) => e);

    httpMock.expectOne(TARGET).flush({ title: 'no' }, { status: 401, statusText: 'Unauthorized' });

    await promise;
    expect(auth.refresh).not.toHaveBeenCalled();
  });

  it('does not retry when ALREADY_RETRIED is set', async () => {
    const ctx = new HttpContext().set(ALREADY_RETRIED, true);
    const promise = firstValueFrom(http.get(TARGET, { context: ctx })).catch((e) => e);

    httpMock.expectOne(TARGET).flush({ title: 'no' }, { status: 401, statusText: 'Unauthorized' });

    await promise;
    expect(auth.refresh).not.toHaveBeenCalled();
  });

  it('logs out and navigates to /login when refresh itself fails', async () => {
    auth.refresh = vi.fn(() =>
      new Observable<void>((subscriber) => {
        subscriber.error(new Error('refresh-failed'));
      })
    );

    const promise = firstValueFrom(http.get(TARGET)).catch((e) => e);
    httpMock
      .expectOne(TARGET)
      .flush({ title: 'expired' }, { status: 401, statusText: 'Unauthorized' });

    await promise;
    expect(auth.setCurrentUser).toHaveBeenCalledWith(null);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('passes non-401 errors through without calling refresh', async () => {
    const promise = firstValueFrom(http.get(TARGET)).catch((e) => e);
    httpMock.expectOne(TARGET).flush({ title: 'oops' }, { status: 500, statusText: 'Server' });

    await promise;
    expect(auth.refresh).not.toHaveBeenCalled();
  });
});
