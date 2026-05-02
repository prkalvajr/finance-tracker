import { inject, Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

import {
  ConfirmationDialogComponent,
  ConfirmOptions
} from '../components/confirmation-dialog/confirmation-dialog.component';

@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  private readonly dialog = inject(MatDialog);

  async confirm(opts: ConfirmOptions): Promise<boolean> {
    const ref = this.dialog.open<ConfirmationDialogComponent, ConfirmOptions, boolean>(
      ConfirmationDialogComponent,
      { data: opts }
    );
    const result = await firstValueFrom(ref.afterClosed());
    return result === true;
  }
}
