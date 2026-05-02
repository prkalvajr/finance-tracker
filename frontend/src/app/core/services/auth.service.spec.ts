import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { AuthService } from './auth.service';

const API = 'http://localhost:5283';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([])),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  describe('bootstrap', () => {
    it('populates currentUser on 200', async () => {
      const promise = service.bootstrap();
      const req = httpMock.expectOne(`${API}/user`);
      expect(req.request.method).toBe('GET');
      expect(req.request.withCredentials).toBe(true);
      req.flush({ userId: 7, name: 'Demo', email: 'd@x.io' });
      await promise;

      expect(service.currentUser()).toEqual({ userId: 7, name: 'Demo', email: 'd@x.io' });
      expect(service.isAuthenticated()).toBe(true);
    });

    it('keeps currentUser null on 401', async () => {
      const promise = service.bootstrap();
      httpMock.expectOne(`${API}/user`).flush({ status: 401 }, { status: 401, statusText: 'Unauthorized' });
      await promise;

      expect(service.currentUser()).toBeNull();
      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('login', () => {
    it('posts credentials, then fetches the user, and updates the signal', async () => {
      const promise = service.login({ email: 'a@b.io', password: 'pw12345678' });

      const loginReq = httpMock.expectOne(`${API}/login`);
      expect(loginReq.request.method).toBe('POST');
      expect(loginReq.request.body).toEqual({ email: 'a@b.io', password: 'pw12345678' });
      expect(loginReq.request.withCredentials).toBe(true);
      loginReq.flush(null);

      // Yield so the awaited firstValueFrom resolves and the next HTTP call queues.
      await Promise.resolve();
      await Promise.resolve();

      const userReq = httpMock.expectOne(`${API}/user`);
      userReq.flush({ userId: 7, name: 'Demo', email: 'a@b.io' });

      await promise;
      expect(service.currentUser()).toEqual({ userId: 7, name: 'Demo', email: 'a@b.io' });
    });

    it('rejects when login returns an error and leaves currentUser null', async () => {
      const promise = service.login({ email: 'a@b.io', password: 'wrong' }).then(
        () => 'resolved',
        (err) => err
      );

      httpMock
        .expectOne(`${API}/login`)
        .flush({ title: 'invalid' }, { status: 401, statusText: 'Unauthorized' });

      const result = await promise;
      expect(result).not.toBe('resolved');
      expect(service.currentUser()).toBeNull();
    });
  });

  describe('register', () => {
    it('posts payload, then fetches user, and updates the signal', async () => {
      const promise = service.register({ name: 'New', email: 'n@x.io', password: 'pw12345678' });

      const regReq = httpMock.expectOne(`${API}/register`);
      expect(regReq.request.method).toBe('POST');
      regReq.flush(null);

      await Promise.resolve();
      await Promise.resolve();

      httpMock.expectOne(`${API}/user`).flush({ userId: 9, name: 'New', email: 'n@x.io' });

      await promise;
      expect(service.currentUser()).toEqual({ userId: 9, name: 'New', email: 'n@x.io' });
    });
  });

  describe('logout', () => {
    it('clears currentUser on success', async () => {
      service.setCurrentUser({ userId: 1, name: 'A', email: 'a@b.io' });

      const promise = service.logout();
      httpMock.expectOne(`${API}/logout`).flush(null);
      await promise;

      expect(service.currentUser()).toBeNull();
    });

    it('clears currentUser even when /logout fails', async () => {
      service.setCurrentUser({ userId: 1, name: 'A', email: 'a@b.io' });

      const promise = service.logout().catch(() => undefined);
      httpMock
        .expectOne(`${API}/logout`)
        .flush({ title: 'oops' }, { status: 500, statusText: 'ServerError' });
      await promise;

      expect(service.currentUser()).toBeNull();
    });
  });

  describe('refresh', () => {
    it('shares a single in-flight HTTP request among concurrent callers', async () => {
      const a = new Promise<void>((resolve, reject) => service.refresh().subscribe({ next: resolve, error: reject }));
      const b = new Promise<void>((resolve, reject) => service.refresh().subscribe({ next: resolve, error: reject }));

      const reqs = httpMock.match(`${API}/refresh`);
      expect(reqs.length).toBe(1);
      reqs[0].flush(null);

      await Promise.all([a, b]);
    });

    it('starts a new request after the previous one completes', async () => {
      const first = new Promise<void>((resolve) => service.refresh().subscribe({ next: () => resolve() }));
      httpMock.expectOne(`${API}/refresh`).flush(null);
      await first;

      const second = new Promise<void>((resolve) => service.refresh().subscribe({ next: () => resolve() }));
      httpMock.expectOne(`${API}/refresh`).flush(null);
      await second;
    });

    it('clears currentUser when refresh fails', async () => {
      service.setCurrentUser({ userId: 1, name: 'A', email: 'a@b.io' });

      const failed = new Promise<unknown>((resolve) =>
        service.refresh().subscribe({ next: () => resolve('ok'), error: (e) => resolve(e) })
      );
      httpMock
        .expectOne(`${API}/refresh`)
        .flush({ title: 'no' }, { status: 401, statusText: 'Unauthorized' });

      const result = await failed;
      expect(result).not.toBe('ok');
      expect(service.currentUser()).toBeNull();
    });
  });
});
