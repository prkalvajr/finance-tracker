import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  ConfirmationDialogComponent,
  ConfirmOptions
} from '../components/confirmation-dialog/confirmation-dialog.component';
import { ConfirmationService } from './confirmation.service';

function makeDialogMock(subject: Subject<boolean | undefined>) {
  return {
    open: vi.fn().mockReturnValue({ afterClosed: () => subject.asObservable() })
  };
}

describe('ConfirmationService', () => {
  let service: ConfirmationService;
  let subject: Subject<boolean | undefined>;
  let mockDialog: ReturnType<typeof makeDialogMock>;

  beforeEach(() => {
    subject = new Subject<boolean | undefined>();
    mockDialog = makeDialogMock(subject);

    TestBed.configureTestingModule({
      providers: [{ provide: MatDialog, useValue: mockDialog }]
    });

    service = TestBed.inject(ConfirmationService);
  });

  it('resolves true when confirm is clicked', async () => {
    const promise = service.confirm({ title: 'T', message: 'M' });
    subject.next(true);
    subject.complete();
    expect(await promise).toBe(true);
  });

  it('resolves false when cancel is clicked', async () => {
    const promise = service.confirm({ title: 'T', message: 'M' });
    subject.next(false);
    subject.complete();
    expect(await promise).toBe(false);
  });

  it('resolves false on backdrop close (undefined result)', async () => {
    const promise = service.confirm({ title: 'T', message: 'M' });
    subject.next(undefined);
    subject.complete();
    expect(await promise).toBe(false);
  });

  it('passes options through to dialog data', async () => {
    const opts: ConfirmOptions = {
      title: 'Delete item',
      message: 'This cannot be undone.',
      confirmText: 'Delete',
      cancelText: 'Keep'
    };
    const promise = service.confirm(opts);
    subject.next(false);
    subject.complete();
    await promise;

    expect(mockDialog.open).toHaveBeenCalledWith(
      ConfirmationDialogComponent,
      expect.objectContaining({ data: opts })
    );
  });

  it('propagates variant: danger to dialog data', async () => {
    const opts: ConfirmOptions = { title: 'Delete', message: 'Sure?', variant: 'danger' };
    const promise = service.confirm(opts);
    subject.next(true);
    subject.complete();
    await promise;

    expect(mockDialog.open).toHaveBeenCalledWith(
      ConfirmationDialogComponent,
      expect.objectContaining({ data: expect.objectContaining({ variant: 'danger' }) })
    );
  });
});
