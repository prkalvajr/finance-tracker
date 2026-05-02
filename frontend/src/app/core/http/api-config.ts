import { InjectionToken } from '@angular/core';

// Backend base URL. Default points at the Kestrel http profile in launchSettings.json.
// Override in tests or alternate environments by re-providing API_BASE_URL.
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => 'http://localhost:5283'
});
