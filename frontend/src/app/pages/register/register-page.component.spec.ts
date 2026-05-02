import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { ActivatedRoute, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { RegisterPageComponent } from './register-page.component';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

describe('RegisterPageComponent', () => {
  let auth: { register: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let notification: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  function build() {
    auth = { register: vi.fn().mockResolvedValue(undefined) };
    router = { navigate: vi.fn() };
    notification = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [RegisterPageComponent],
      providers: [
        provideAnimationsAsync('noop'),
        { provide: ActivatedRoute, useValue: { snapshot: {}, queryParams: { subscribe: () => undefined } } },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        { provide: NotificationService, useValue: notification }
      ]
    });

    return TestBed.createComponent(RegisterPageComponent);
  }

  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('marks name, email, password as required', () => {
    const fixture = build();
    const cmp = fixture.componentInstance;
    cmp.form.controls.name.setValue('');
    cmp.form.controls.email.setValue('');
    cmp.form.controls.password.setValue('');
    expect(cmp.form.controls.name.errors?.['required']).toBeTruthy();
    expect(cmp.form.controls.email.errors?.['required']).toBeTruthy();
    expect(cmp.form.controls.password.errors?.['required']).toBeTruthy();
  });

  it('rejects names longer than 200 characters', () => {
    const fixture = build();
    fixture.componentInstance.form.controls.name.setValue('x'.repeat(201));
    expect(fixture.componentInstance.form.controls.name.errors?.['maxlength']).toBeTruthy();
  });

  it('rejects malformed emails and short passwords', () => {
    const fixture = build();
    fixture.componentInstance.form.controls.email.setValue('bad');
    fixture.componentInstance.form.controls.password.setValue('short');
    expect(fixture.componentInstance.form.controls.email.errors?.['email']).toBeTruthy();
    expect(fixture.componentInstance.form.controls.password.errors?.['minlength']).toBeTruthy();
  });

  it('does not call register when invalid', async () => {
    const fixture = build();
    await fixture.componentInstance.submit();
    expect(auth.register).not.toHaveBeenCalled();
  });

  it('calls register and navigates to /home on success', async () => {
    const fixture = build();
    fixture.componentInstance.form.setValue({
      name: 'New Person',
      email: 'n@x.io',
      password: 'pw12345678'
    });
    await fixture.componentInstance.submit();

    expect(auth.register).toHaveBeenCalledWith({
      name: 'New Person',
      email: 'n@x.io',
      password: 'pw12345678'
    });
    expect(router.navigate).toHaveBeenCalledWith(['/home']);
    expect(notification.success).toHaveBeenCalled();
  });

  it('maps server errors dictionary onto fields (e.g., 409 duplicate email)', async () => {
    const fixture = build();
    auth.register.mockRejectedValueOnce(
      new HttpErrorResponse({
        status: 409,
        error: { errors: { Email: ['Email already in use'] } }
      })
    );
    fixture.componentInstance.form.setValue({
      name: 'New',
      email: 'dup@x.io',
      password: 'pw12345678'
    });
    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.form.controls.email.errors).toEqual({
      server: 'Email already in use'
    });
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('falls back to a form-level server error when no field matches', async () => {
    const fixture = build();
    auth.register.mockRejectedValueOnce(
      new HttpErrorResponse({ status: 500, error: { title: 'Server failure' } })
    );
    fixture.componentInstance.form.setValue({
      name: 'New',
      email: 'ok@x.io',
      password: 'pw12345678'
    });
    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.form.errors?.['server']).toBe('Server failure');
  });
});
