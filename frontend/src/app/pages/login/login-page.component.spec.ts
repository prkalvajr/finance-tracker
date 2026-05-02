import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { ActivatedRoute, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LoginPageComponent } from './login-page.component';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

describe('LoginPageComponent', () => {
  let auth: { login: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let notification: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  function build() {
    auth = { login: vi.fn().mockResolvedValue(undefined) };
    router = { navigate: vi.fn() };
    notification = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        provideAnimationsAsync('noop'),
        { provide: ActivatedRoute, useValue: { snapshot: {}, queryParams: { subscribe: () => undefined } } },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        { provide: NotificationService, useValue: notification }
      ]
    });

    return TestBed.createComponent(LoginPageComponent);
  }

  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('marks email and password as required', () => {
    const fixture = build();
    const cmp = fixture.componentInstance;

    cmp.form.controls.email.setValue('');
    cmp.form.controls.password.setValue('');
    expect(cmp.form.controls.email.errors?.['required']).toBeTruthy();
    expect(cmp.form.controls.password.errors?.['required']).toBeTruthy();
  });

  it('rejects malformed emails', () => {
    const fixture = build();
    fixture.componentInstance.form.controls.email.setValue('not-an-email');
    expect(fixture.componentInstance.form.controls.email.errors?.['email']).toBeTruthy();
  });

  it('rejects passwords shorter than 8 characters', () => {
    const fixture = build();
    fixture.componentInstance.form.controls.password.setValue('short');
    expect(fixture.componentInstance.form.controls.password.errors?.['minlength']).toBeTruthy();
  });

  it('does not call AuthService.login when the form is invalid', async () => {
    const fixture = build();
    await fixture.componentInstance.submit();
    expect(auth.login).not.toHaveBeenCalled();
  });

  it('calls AuthService.login and navigates to /home on success', async () => {
    const fixture = build();
    fixture.componentInstance.form.setValue({ email: 'a@b.io', password: 'pw12345678' });
    await fixture.componentInstance.submit();

    expect(auth.login).toHaveBeenCalledWith({ email: 'a@b.io', password: 'pw12345678' });
    expect(router.navigate).toHaveBeenCalledWith(['/home']);
    expect(notification.success).toHaveBeenCalled();
  });

  it('maps a ProblemDetails errors dictionary onto matching form fields', async () => {
    const fixture = build();
    auth.login.mockRejectedValueOnce(
      new HttpErrorResponse({
        status: 400,
        error: { errors: { Email: ['Required'], Password: ['Too short'] } }
      })
    );
    fixture.componentInstance.form.setValue({ email: 'a@b.io', password: 'pw12345678' });
    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.form.controls.email.errors).toEqual({ server: 'Required' });
    expect(fixture.componentInstance.form.controls.password.errors).toEqual({ server: 'Too short' });
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('falls back to a form-level server error when no field matches', async () => {
    const fixture = build();
    auth.login.mockRejectedValueOnce(
      new HttpErrorResponse({ status: 401, error: { title: 'Invalid credentials' } })
    );
    fixture.componentInstance.form.setValue({ email: 'a@b.io', password: 'pw12345678' });
    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.form.errors?.['server']).toBe('Invalid credentials');
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
