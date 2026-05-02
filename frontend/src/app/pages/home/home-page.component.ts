import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-home-page',
  standalone: true,
  template: `<h2>Home</h2>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomePageComponent {}
