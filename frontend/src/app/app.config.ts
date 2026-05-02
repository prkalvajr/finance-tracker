import {
  ApplicationConfig,
  ErrorHandler,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorToastInterceptor } from './core/interceptors/error-toast.interceptor';
import { AuthService } from './core/services/auth.service';
import { AppErrorHandler } from './core/error-handler';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor, errorToastInterceptor])),
    provideAppInitializer(() => inject(AuthService).bootstrap()),
    { provide: ErrorHandler, useClass: AppErrorHandler }
  ]
};
