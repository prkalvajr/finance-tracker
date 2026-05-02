import { FormControl, FormGroup } from '@angular/forms';
import { describe, expect, it } from 'vitest';

import { applyServerErrors } from './apply-server-errors';

function buildForm() {
  return new FormGroup({
    email: new FormControl(''),
    password: new FormControl('')
  });
}

describe('applyServerErrors', () => {
  it('maps fields by case-insensitive name', () => {
    const form = buildForm();
    applyServerErrors(form, {
      errors: { Email: ['Required'], Password: ['Too short'] }
    });
    expect(form.controls.email.errors).toEqual({ server: 'Required' });
    expect(form.controls.password.errors).toEqual({ server: 'Too short' });
  });

  it('joins multiple messages with spaces', () => {
    const form = buildForm();
    applyServerErrors(form, { errors: { Password: ['Too short', 'Bad'] } });
    expect(form.controls.password.errors?.['server']).toBe('Too short Bad');
  });

  it('ignores fields that have no matching control', () => {
    const form = buildForm();
    const applied = applyServerErrors(form, { errors: { Unknown: ['msg'] } });
    expect(applied).toBe(false);
    expect(form.controls.email.errors).toBeNull();
  });

  it('returns false when problem is null/undefined or has no errors', () => {
    const form = buildForm();
    expect(applyServerErrors(form, null)).toBe(false);
    expect(applyServerErrors(form, undefined)).toBe(false);
    expect(applyServerErrors(form, { title: 'x' })).toBe(false);
  });

  it('marks the matched control as touched so mat-error renders immediately', () => {
    const form = buildForm();
    expect(form.controls.email.touched).toBe(false);
    applyServerErrors(form, { errors: { Email: ['Required'] } });
    expect(form.controls.email.touched).toBe(true);
  });
});
