import { inject, Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  private readonly baseConfig: MatSnackBarConfig = {
    duration: 4000,
    horizontalPosition: 'right',
    verticalPosition: 'bottom'
  };

  success(message: string): void {
    this.snackBar.open(message, 'Close', { ...this.baseConfig, panelClass: 'toast-success' });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Close', { ...this.baseConfig, panelClass: 'toast-error' });
  }

  info(message: string): void {
    this.snackBar.open(message, 'Close', { ...this.baseConfig, panelClass: 'toast-info' });
  }
}
