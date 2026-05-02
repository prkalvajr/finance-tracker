import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { TransactionModalComponent } from './transaction-modal.component';
import { TransactionsService } from '../../../core/services/transactions.service';
import { NotificationService } from '../../../core/services/notification.service';
import { TransactionDto } from '../../../models/transaction.models';

function makeTransaction(overrides: Partial<TransactionDto> = {}): TransactionDto {
  return {
    transactionId: 7,
    userId: 1,
    title: 'Groceries',
    amount: 54.25,
    category: 'expense',
    date: '2026-04-20',
    createdAt: '2026-04-20T10:00:00Z',
    updatedAt: '2026-04-20T10:00:00Z',
    ...overrides
  };
}

function build(data: TransactionDto | null = null) {
  const transactions = {
    create: vi.fn().mockResolvedValue(makeTransaction({ transactionId: 11 })),
    update: vi.fn().mockResolvedValue(makeTransaction({ title: 'Updated' }))
  };
  const dialogRef = { close: vi.fn() };
  const notification = { success: vi.fn(), error: vi.fn(), info: vi.fn() };

  TestBed.configureTestingModule({
    imports: [TransactionModalComponent, NoopAnimationsModule],
    providers: [
      { provide: MAT_DIALOG_DATA, useValue: data },
      { provide: MatDialogRef, useValue: dialogRef },
      { provide: TransactionsService, useValue: transactions },
      { provide: NotificationService, useValue: notification }
    ]
  });

  const fixture = TestBed.createComponent(TransactionModalComponent);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, transactions, dialogRef, notification };
}

describe('TransactionModalComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('1: Create mode starts empty and defaults date to today', () => {
    const { component } = build();
    const today = new Date(Date.now() - new Date().getTimezoneOffset() * 60_000)
      .toISOString()
      .slice(0, 10);

    expect(component.isEditMode).toBe(false);
    expect(component.form.controls.title.value).toBe('');
    expect(component.form.controls.amount.value).toBe(0);
    expect(component.form.controls.category.value).toBe('expense');
    expect(component.form.controls.date.value).toBe(today);
  });

  it('2: Edit mode pre-fills from MAT_DIALOG_DATA', () => {
    const row = makeTransaction({ title: 'Salary', amount: 3200, category: 'income', date: '2026-03-02T00:00:00' });
    const { component } = build(row);

    expect(component.isEditMode).toBe(true);
    expect(component.form.getRawValue()).toEqual({
      title: 'Salary',
      amount: 3200,
      category: 'income',
      date: '2026-03-02'
    });
  });

  it('3: Validators require title, positive amount, and category', () => {
    const { component } = build();
    component.form.controls.title.setValue('');
    component.form.controls.amount.setValue(-1);
    component.form.controls.category.setValue('' as never);

    expect(component.form.controls.title.errors?.['required']).toBeTruthy();
    expect(component.form.controls.amount.errors?.['min']).toBeTruthy();
    expect(component.form.controls.category.errors?.['required']).toBeTruthy();
  });

  it('4: Submit in create mode calls create() and closes with saved entity', async () => {
    const { component, transactions, dialogRef, notification } = build();
    component.form.setValue({ title: 'Book', amount: 18, category: 'expense', date: '2026-05-01' });

    await component.submit();

    expect(transactions.create).toHaveBeenCalledWith({
      title: 'Book',
      amount: 18,
      category: 'expense',
      date: '2026-05-01'
    });
    expect(dialogRef.close).toHaveBeenCalledWith(makeTransaction({ transactionId: 11 }));
    expect(notification.success).toHaveBeenCalledWith('Transaction added.');
  });

  it('5: Submit in edit mode calls update() and closes with saved entity', async () => {
    const row = makeTransaction();
    const { component, transactions, dialogRef, notification } = build(row);
    component.form.setValue({ title: 'Groceries updated', amount: 60, category: 'expense', date: '2026-04-21' });

    await component.submit();

    expect(transactions.update).toHaveBeenCalledWith({
      transactionId: 7,
      title: 'Groceries updated',
      amount: 60,
      category: 'expense',
      date: '2026-04-21'
    });
    expect(dialogRef.close).toHaveBeenCalledWith(makeTransaction({ title: 'Updated' }));
    expect(notification.success).toHaveBeenCalledWith('Transaction updated.');
  });

  it('6: Negative amount is rejected client-side', async () => {
    const { component, transactions } = build();
    component.form.setValue({ title: 'Bad amount', amount: -5, category: 'expense', date: '2026-05-01' });

    await component.submit();

    expect(transactions.create).not.toHaveBeenCalled();
    expect(component.form.controls.amount.errors?.['min']).toBeTruthy();
  });

  it('7: Server errors map onto fields via applyServerErrors', async () => {
    const { component, transactions, dialogRef } = build();
    transactions.create.mockRejectedValueOnce(
      new HttpErrorResponse({
        status: 400,
        error: { errors: { Title: ['Title is required.'], Amount: ['Amount must be positive.'] } }
      })
    );
    component.form.setValue({ title: 'Invalid', amount: 1, category: 'expense', date: '2026-05-01' });

    await component.submit();

    expect(component.form.controls.title.errors).toEqual({ server: 'Title is required.' });
    expect(component.form.controls.amount.errors).toEqual({ server: 'Amount must be positive.' });
    expect(dialogRef.close).not.toHaveBeenCalled();
  });
});
