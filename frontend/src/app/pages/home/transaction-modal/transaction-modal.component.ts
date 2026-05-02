import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { TransactionsService } from '../../../core/services/transactions.service';
import { NotificationService } from '../../../core/services/notification.service';
import { applyServerErrors } from '../../../core/utils/apply-server-errors';
import {
  TransactionCategory,
  TransactionDto
} from '../../../models/transaction.models';
import { ProblemDetails } from '../../../models/problem-details.models';

function todayInputValue(): string {
  const now = new Date();
  const offsetDate = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return offsetDate.toISOString().slice(0, 10);
}

@Component({
  selector: 'app-transaction-modal',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './transaction-modal.component.html',
  styleUrl: './transaction-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly transactions = inject(TransactionsService);
  private readonly notification = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<TransactionModalComponent, TransactionDto>);
  readonly data = inject<TransactionDto | null>(MAT_DIALOG_DATA, { optional: true });

  readonly isEditMode = this.data !== null;
  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    title: [this.data?.title ?? '', [Validators.required, Validators.maxLength(200)]],
    amount: [this.data?.amount ?? 0, [Validators.required, Validators.min(0.01)]],
    category: [this.data?.category ?? 'expense' as TransactionCategory, [Validators.required]],
    date: [this.data?.date?.slice(0, 10) ?? todayInputValue()]
  });

  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const { title, amount, category, date } = this.form.getRawValue();
      const saved = this.isEditMode
        ? await this.transactions.update({
            transactionId: this.data!.transactionId,
            title,
            amount,
            category,
            date: date || null
          })
        : await this.transactions.create({
            title,
            amount,
            category,
            date: date || null
          });

      this.notification.success(this.isEditMode ? 'Transaction updated.' : 'Transaction added.');
      this.dialogRef.close(saved);
    } catch (err) {
      const problem = err instanceof HttpErrorResponse ? (err.error as ProblemDetails) : null;
      applyServerErrors(this.form, problem);
    } finally {
      this.submitting.set(false);
    }
  }
}
