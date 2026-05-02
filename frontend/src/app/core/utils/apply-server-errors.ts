import { AbstractControl, FormGroup } from '@angular/forms';

import { ProblemDetails } from '../../models/problem-details.models';

// Maps ProblemDetails.errors onto a FormGroup so server-side validation
// renders inline through <mat-error> using the `server` error key.
//
// Field names in the dictionary are matched case-insensitively against
// control names — the backend uses PascalCase ("Email", "Password") while
// the form uses camelCase. Unknown fields are silently ignored.
//
// Returns true if any field error was applied.
export function applyServerErrors(form: FormGroup, problem: ProblemDetails | null | undefined): boolean {
  if (!problem || !problem.errors) {
    return false;
  }

  const lookup = new Map<string, AbstractControl>();
  for (const [name, control] of Object.entries(form.controls)) {
    lookup.set(name.toLowerCase(), control);
  }

  let applied = false;
  for (const [field, messages] of Object.entries(problem.errors)) {
    const control = lookup.get(field.toLowerCase());
    if (!control) continue;
    const message = Array.isArray(messages) ? messages.join(' ') : String(messages);
    control.setErrors({ ...(control.errors ?? {}), server: message });
    control.markAsTouched();
    applied = true;
  }

  return applied;
}
