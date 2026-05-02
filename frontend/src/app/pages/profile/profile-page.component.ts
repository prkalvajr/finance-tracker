import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  template: `<h2>Profile</h2>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfilePageComponent {}
