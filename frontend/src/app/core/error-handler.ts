import { ErrorHandler, inject, Injectable, NgZone } from '@angular/core';

import { NotificationService } from './services/notification.service';

// Safety net for uncaught errors (HTTP errors are already handled by the
// error-toast interceptor). Logs to console and surfaces a generic toast so
// users get some feedback even when something unexpected breaks.
@Injectable()
export class AppErrorHandler implements ErrorHandler {
  private readonly notification = inject(NotificationService);
  private readonly zone = inject(NgZone);

  handleError(error: unknown): void {
    // eslint-disable-next-line no-console
    console.error(error);
    this.zone.run(() =>
      this.notification.error('An unexpected error occurred. Please try again.')
    );
  }
}
