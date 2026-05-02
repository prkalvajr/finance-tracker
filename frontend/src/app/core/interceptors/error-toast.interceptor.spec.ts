import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { errorToastInterceptor } from './error-toast.interceptor';
import { NotificationService } from '../services/notification.service';
import { SKIP_ERROR_TOAST } from '../http/http-context-tokens';

const URL = 'http://api.test/x';

describe('errorToastInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let notification: { error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    notification = { error: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorToastInterceptor])),
        provideHttpClientTesting(),
        { provide: NotificationService, useValue: notification }
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('shows a toast for non-401 errors using ProblemDetails.detail', async () => {
    const promise = firstValueFrom(http.get(URL)).catch((e) => e);
    httpMock
      .expectOne(URL)
      .flush(
        { title: 'Bad', detail: 'Field invalid', status: 400 },
        { status: 400, statusText: 'BadRequest' }
      );
    await promise;
    expect(notification.error).toHaveBeenCalledWith('Field invalid');
  });

  it('joins messages from a ProblemDetails errors dictionary', async () => {
    const promise = firstValueFrom(http.get(URL)).catch((e) => e);
    httpMock
      .expectOne(URL)
      .flush(
        { title: 'Validation', errors: { Email: ['Required'], Password: ['Too short', 'Bad'] } },
        { status: 400, statusText: 'BadRequest' }
      );
    await promise;
    expect(notification.error).toHaveBeenCalledTimes(1);
    const msg = notification.error.mock.calls[0][0] as string;
    expect(msg).toContain('Required');
    expect(msg).toContain('Too short');
    expect(msg).toContain('Bad');
  });

  it('falls back to title when detail is missing', async () => {
    const promise = firstValueFrom(http.get(URL)).catch((e) => e);
    httpMock
      .expectOne(URL)
      .flush({ title: 'Conflict' }, { status: 409, statusText: 'Conflict' });
    await promise;
    expect(notification.error).toHaveBeenCalledWith('Conflict');
  });

  it('uses a generic fallback when nothing useful is available', async () => {
    const promise = firstValueFrom(http.get(URL)).catch((e) => e);
    httpMock.expectOne(URL).flush(null, { status: 500, statusText: 'Server' });
    await promise;
    expect(notification.error).toHaveBeenCalledTimes(1);
    const msg = notification.error.mock.calls[0][0] as string;
    expect(typeof msg).toBe('string');
    expect(msg.length).toBeGreaterThan(0);
  });

  it('does not toast on 401', async () => {
    const promise = firstValueFrom(http.get(URL)).catch((e) => e);
    httpMock
      .expectOne(URL)
      .flush({ title: 'no' }, { status: 401, statusText: 'Unauthorized' });
    await promise;
    expect(notification.error).not.toHaveBeenCalled();
  });

  it('does not toast when SKIP_ERROR_TOAST is set on the request', async () => {
    const ctx = new HttpContext().set(SKIP_ERROR_TOAST, true);
    const promise = firstValueFrom(http.get(URL, { context: ctx })).catch((e) => e);
    httpMock
      .expectOne(URL)
      .flush({ title: 'bad' }, { status: 400, statusText: 'BadRequest' });
    await promise;
    expect(notification.error).not.toHaveBeenCalled();
  });
});
