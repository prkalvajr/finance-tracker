import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;
  let snackBar: { open: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    snackBar = { open: vi.fn() };
    TestBed.configureTestingModule({
      providers: [{ provide: MatSnackBar, useValue: snackBar }]
    });
    service = TestBed.inject(NotificationService);
  });

  it('success() opens the snackbar with toast-success panelClass', () => {
    service.success('done');
    expect(snackBar.open).toHaveBeenCalledTimes(1);
    const [msg, action, config] = snackBar.open.mock.calls[0];
    expect(msg).toBe('done');
    expect(action).toBe('Close');
    expect(config.panelClass).toBe('toast-success');
  });

  it('error() opens the snackbar with toast-error panelClass', () => {
    service.error('boom');
    const [, , config] = snackBar.open.mock.calls[0];
    expect(config.panelClass).toBe('toast-error');
  });

  it('info() opens the snackbar with toast-info panelClass', () => {
    service.info('fyi');
    const [, , config] = snackBar.open.mock.calls[0];
    expect(config.panelClass).toBe('toast-info');
  });
});
