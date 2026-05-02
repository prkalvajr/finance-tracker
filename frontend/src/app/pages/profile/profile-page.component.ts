import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserService } from '../../core/services/user.service';
import { ConfirmationService } from '../../shared/services/confirmation.service';

// Group-level validator: currentPassword is required when newPassword is provided.
function requireCurrentPasswordWithNew(): ValidatorFn {
  return (group: AbstractControl) => {
    const newPw = group.get('newPassword')?.value as string;
    const curPw = group.get('currentPassword')?.value as string;
    return newPw && !curPw ? { currentPasswordRequired: true } : null;
  };
}

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfilePageComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly notification = inject(NotificationService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group(
    {
      name: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      currentPassword: [''],
      newPassword: ['', [Validators.minLength(8)]]
    },
    { validators: requireCurrentPasswordWithNew() }
  );

  readonly submitting = signal(false);

  ngOnInit(): void {
    const user = this.auth.currentUser();
    if (user) {
      this.form.patchValue({ name: user.name, email: user.email });
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, email, currentPassword, newPassword } = this.form.getRawValue();

    if (newPassword) {
      const confirmed = await this.confirmation.confirm({
        title: 'Change password',
        message: 'Are you sure you want to change your password?',
        confirmText: 'Change password',
        cancelText: 'Cancel'
      });
      if (!confirmed) {
        return;
      }
    }

    this.submitting.set(true);
    try {
      const updated = await firstValueFrom(
        this.userService.updateUser({
          name: name || null,
          email: email || null,
          currentPassword: currentPassword || null,
          newPassword: newPassword || null
        })
      );
      this.auth.setCurrentUser(updated);
      this.notification.success('Profile updated.');
      this.form.patchValue({ currentPassword: '', newPassword: '' });
      this.form.controls.currentPassword.markAsUntouched();
      this.form.controls.newPassword.markAsUntouched();
    } finally {
      this.submitting.set(false);
    }
  }
}
